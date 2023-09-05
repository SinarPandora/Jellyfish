using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Core.Kook.Protocol;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;
using VoiceQuality = Jellyfish.Core.Kook.Protocol.VoiceQuality;

namespace Jellyfish.Module.TeamPlay.Manage;

/// <summary>
///     Team play config manage command
/// </summary>
public class TeamPlayManageCommand : MessageCommand
{
    private const string UserInjectKeyword = "{USER}";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TeamPlayManageCommand()
    {
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            管理组队配置

            您可以绑定语音入口频道，该频道将成为后续自动创建语音频道的入口
            您也可以绑定任意文字频道为入口频道，在目标频道发送由 /组队 开头的消息将自动创建对应房间
            当绑定了一个语音入口频道或文字入口频道后，配置就可以使用啦
            """,
            $"""
             - 帮助：显示此消息
             - 列表：列出全部的组队配置
             - 配置 [配置名称]：调整指定组队配置
             - 绑定文字频道 [配置名称]：在目标频道中使用，设置后，该频道发送的组队质量会使用该配置创建语音频道
             - 房间名格式 [配置名称] [名称格式]：修改语音房间名称格式，使用 {UserInjectKeyword} 代表用户输入的内容
             - 默认人数 [配置名称] [数字]：设定创建语音房间的默认人数，输入 0 代表人数无限
             - 语音质量 [配置名称] [低|中|高]：设定临时语音频道的质量
             - 删除 [配置名称]：删除指定配置
             """);
    }

    public override string Name() => "管理组队配置指令";

    public override IEnumerable<string> Keywords() => new[] { "!组队", "！组队" };

    public override async Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await channel.SendTextAsync(HelpMessage);
        else if (args.StartsWith("配置"))
            await SendBindingWizard(user, channel, args[2..].TrimStart());
        else if (args.StartsWith("绑定文字频道"))
            await BindingTextChannel(channel, args[6..].TrimStart());
        else if (args.StartsWith("语音质量"))
            await SetDefaultQuality(channel, args[4..].TrimStart());
        else if (args.StartsWith("房间名格式"))
            await SetRoomPattern(channel, args[5..].TrimStart());
        else if (args.StartsWith("默认人数"))
            await SetDefaultMemberCount(channel, args[4..].TrimStart());
        else if (args.StartsWith("删除"))
            await RemoveConfig(channel, args[2..].TrimStart());
        else if (args.StartsWith("列表"))
            await ListConfigs(channel);
        else
            await channel.SendTextAsync(HelpMessage);
    }

    /// <summary>
    ///     Send binding wizard card message to the current channel
    /// </summary>
    /// <param name="user">Action user</param>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name</param>
    private static async Task SendBindingWizard(SocketUser user, SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();

        var names = dbCtx.TpConfigs.EnabledInGuild(channel.Guild).AsNoTracking()
            .Select(c => c.Name)
            .ToHashSet();

        if (string.IsNullOrEmpty(name))
        {
            await channel.SendErrorCardAsync("请设置绑定名称，举例：`！组队 绑定 真格上分`");
        }
        else
        {
            var cardBuilder = new CardBuilder();
            // Header element
            cardBuilder.AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText($"""
                            **欢迎使用组队绑定功能**
                            您正在{(names.Contains(name) ? "重新" : "")}绑定 {name}
                            """, true);
            });
            // Voice channel binding prompt
            cardBuilder.AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText("""
                           > 🗣️您可以为当前配置绑定语音入口频道，该频道将成为后续自动创建语音频道的入口
                           > 绑定方法为：先加入目标频道，加入后，请点击下方按钮
                           """, true);
            });
            // Voice channel binding button
            cardBuilder.AddModule<ActionGroupModuleBuilder>(a =>
            {
                a.AddElement(b =>
                {
                    // Click this button will run DoBindingParentChannel with the name which user input
                    b.WithText("我已加入语音频道")
                        .WithClick(ButtonClickEventType.ReturnValue)
                        .WithValue($"tp_bind_{user.Id}_{name}")
                        .WithTheme(ButtonTheme.Primary);
                });
            });
            // Text channel binding prompt
            cardBuilder.AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText("""
                           > 💬您也可以同时绑定任意文字频道为入口频道，在目标频道发送由 /组队 开头的消息将自动创建对应房间
                           > 绑定方法为：在目标文字频道发送 !组队 绑定文字频道
                           """, true);
            });
            // Other prompt
            cardBuilder
                .AddModule<SectionModuleBuilder>(s =>
                {
                    s.WithText("> 当绑定了一个语音入口频道或文字入口频道后，配置就可以使用啦",
                        true);
                });

            await channel.SendCardAsync(cardBuilder.Build());
            Log.Info($"已发送绑定向导，目标类型：{name}");
        }
    }

    /// <summary>
    ///     List all team play config
    /// </summary>
    /// <param name="channel">Message send to this channel</param>
    private static async Task ListConfigs(SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();
        var configRecords = dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            .OrderByDescending(e => e.Name)
            .ToArray();
        if (!configRecords.Any())
        {
            await channel.SendTextAsync("您还没有任何组队配置");
        }
        else
        {
            var configs = configRecords
                .Select(e =>
                {
                    var voiceChannel = e.VoiceChannelId != null
                        ? MentionUtils.KMarkdownMentionChannel((ulong)e.VoiceChannelId)
                        : "未绑定";
                    var textChannel = e.TextChannelId != null
                        ? MentionUtils.KMarkdownMentionChannel((ulong)e.TextChannelId)
                        : "未绑定";
                    return $"ID：{e.Id}，名称：{e.Name}，语音入口：{voiceChannel}，文字入口：{textChannel}" +
                           $"语音质量：{e.VoiceQuality?.GetName() ?? "默认"}，当前语音房间数：{e.RoomInstances.Count}";
                })
                .ToArray();
            await channel.SendTextAsync(string.Join("\n", configs));
        }
    }

    /// <summary>
    ///     Binding voice channel to config
    /// </summary>
    /// <param name="name">Config name</param>
    /// <param name="user">Action user</param>
    /// <param name="channel">Current channel</param>
    public static async Task BindingVoiceChannel(string name, Cacheable<SocketGuildUser, ulong> user,
        SocketTextChannel channel)
    {
        Log.Info($"已收到名为 {name} 的语音频道绑定请求，执行进一步操作");
        var voiceChannel = user.Value.VoiceChannel;
        if (voiceChannel == null)
        {
            await channel.SendErrorCardAsync("未检测到您加入的语音频道");
        }
        else
        {
            Log.Info($"已检测到语音频道：{voiceChannel.Name}：{voiceChannel.Id}");
            await channel.SendInfoCardAsync($"检测到您加入了频道：{voiceChannel.Name}，正在绑定...");


            await using var dbCtx = new DatabaseContext();
            var config = dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
                .FirstOrDefault(e => e.Name == name);

            // Update or Insert
            if (config != null)
            {
                config.VoiceChannelId = voiceChannel.Id;
            }
            else
            {
                config = new TpConfig(name, channel.Guild.Id)
                {
                    VoiceChannelId = voiceChannel.Id
                };
                dbCtx.TpConfigs.Add(config);
            }

            // Refresh voice quality when updating
            config.VoiceQuality = VoiceQualityHelper.GetHighestVoiceQuality(channel.Guild);
            dbCtx.SaveChanges();
            AppCaches.TeamPlayConfigs.Put($"{channel.Guild.Id}_{name}", config);
            await channel.SendSuccessCardAsync(
                $"绑定成功！加入 {MentionUtils.KMarkdownMentionChannel(voiceChannel.Id)} 将自动创建 {name} 类型的房间");
            await SendFurtherConfigIntroMessage(channel, config);

            Log.Info($"成功绑定 {name} 到 {voiceChannel.Name}：{voiceChannel.Id}，ID：{config.Id}");
        }
    }

    /// <summary>
    ///     Binding text channel to config
    /// </summary>
    /// <param name="channel">Channel to binding</param>
    /// <param name="name">Config name</param>
    private static async Task BindingTextChannel(SocketTextChannel channel, string name)
    {
        Log.Info($"已收到名为 {name} 的文字频道绑定请求，执行进一步操作");
        await using var dbCtx = new DatabaseContext();
        var config = dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            .FirstOrDefault(e => e.Name == name);
        if (config == null)
        {
            config = new TpConfig(name, channel.Guild.Id)
            {
                TextChannelId = channel.Id
            };
            dbCtx.TpConfigs.Add(config);
        }
        else
        {
            config.TextChannelId = channel.Id;
        }

        // Refresh voice quality when updating
        config.VoiceQuality = VoiceQualityHelper.GetHighestVoiceQuality(channel.Guild);
        dbCtx.SaveChanges();
        AppCaches.TeamPlayConfigs.Put($"{channel.Guild.Id}_{name}", config);
        await channel.SendSuccessCardAsync(
            $"""
             绑定成功！当前频道已与组队配置 {name} 绑定
             您可以使用 !组队
             """
        );
        await SendFurtherConfigIntroMessage(channel, config);

        Log.Info($"成功绑定 {name} 到 {channel.Name}：{channel.Id}，ID：{config.Id}");
    }

    /// <summary>
    ///     Set default voice quality, only for temporary updates when the boost level drops
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    private static async Task SetDefaultQuality(SocketTextChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！举例：`!组队 语音质量 休闲 高`");
            return;
        }

        var quality = VoiceQualityHelper.FromName(args[1]);
        if (quality == null)
        {
            await channel.SendErrorCardAsync("请从以下选项中选择语音质量：低，中，高");
        }
        else
        {
            await using var dbCtx = new DatabaseContext();
            var config = dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
                .FirstOrDefault(e => e.Name == args[0]);
            if (config == null)
            {
                await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置");
            }
            else
            {
                config.VoiceQuality = (VoiceQuality)quality;
                dbCtx.SaveChanges();
                AppCaches.TeamPlayConfigs.Update($"{channel.Guild.Id}_{args[0]}", c =>
                {
                    c.VoiceQuality = config.VoiceQuality;
                    return c;
                });
                await channel.SendSuccessCardAsync("设置成功！");
            }
        }
    }

    /// <summary>
    ///     Remove specified configuration
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name to remove</param>
    private static async Task RemoveConfig(SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();
        var record = (
            from config in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where config.Name == name
            select config
        ).FirstOrDefault();
        if (record == null)
        {
            await channel.SendErrorCardAsync($"规则 {name} 未找到或已被删除");
        }
        else
        {
            record.Enabled = false;
            dbCtx.SaveChanges();
            if (!AppCaches.TeamPlayConfigs.Remove($"{channel.Guild.Id}_{name}"))
            {
                Log.Warn($"组队配置缓存缺失，可能是一个 bug。配置键：{channel.Guild.Id}_{name}");
            }

            await channel.SendErrorCardAsync($"规则 {name} 删除成功！已创建的房间将保留直到无人使用");
        }
    }

    /// <summary>
    ///     Send further configure intro message, after creating or binding room
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="config">Team play config</param>
    private static async Task SendFurtherConfigIntroMessage(IMessageChannel channel, TpConfig config)
    {
        await channel.SendCardAsync(new CardBuilder()
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText(
                    $"""
                     您可以进一步配置 {config.Name}
                     ---
                     房间格式：{config.RoomNamePattern ?? $"{UserInjectKeyword}的房间"}
                     默认人数：{(config.DefaultMemberLimit == 0 ? "无限制" : config.DefaultMemberLimit.ToString())}
                     语音质量：{config.VoiceQuality?.GetName() ?? "频道当前最高"}
                     ---
                     设置房间名格式：`!组队 {config.Name} 房间名格式`
                     使用 {UserInjectKeyword} 代表用户输入内容
                     举例如下：
                     1. 使用 `!组队 房间名格式 {config.Name} 【上分】{UserInjectKeyword}` 设置房间格式
                     2. 用户发送指令创建房间：`/组队 XP1700开放`
                     3. 最终房间名为：`【上分】XP1700开放`

                     若房间重名，将在房间后面添加序号来进行区分
                     ---
                     设置默认人数：`!组队 默认人数 [数字]`
                     设定创建语音房间的默认人数，输入 -1 代表人数无限
                     **默认人数无限**
                     ---
                     设置语音质量：`!组队 语音质量 {config.Name} [低|中|高]`
                     低、中、高 为可选项，选择一个即可
                     未配置前根据助力等级自动使用**最高的语音质量**，配置后使用指定的语音质量
                     """, true);
            })
            .Build());
    }

    /// <summary>
    ///     Set new room instance's name pattern
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    private static async Task SetRoomPattern(SocketTextChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync($"参数不足！举例：`!组队 房间名格式 上分 【上分】{UserInjectKeyword}`");
            return;
        }


        var configName = args[0];
        var pattern = args[1];
        if (!pattern.Contains(UserInjectKeyword))
        {
            await channel.SendWarningCardAsync(
                $"名称格式中不包含用户输入的任何内容，这将导致创建的房间重名，" +
                $"从而影响分辨，您可以在格式中添加 {UserInjectKeyword} 来代表用户输入的内容");
        }

        await using var dbCtx = new DatabaseContext();
        var config = (
            from record in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where record.Name == configName
            select record
        ).FirstOrDefault();

        if (config == null)
        {
            await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置");
        }
        else
        {
            config.RoomNamePattern = pattern;
            dbCtx.SaveChanges();
            AppCaches.TeamPlayConfigs.Update($"{channel.Guild.Id}_{configName}", c =>
            {
                c.RoomNamePattern = pattern;
                return c;
            });
            await channel.SendSuccessCardAsync($"更改房间名格式成功，新房间名称格式为：{pattern}{UserInjectKeyword}");
        }
    }

    /// <summary>
    ///     Set new room default member count
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    private static async Task SetDefaultMemberCount(SocketTextChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！举例：`!组队 默认人数 上分 4`");
            return;
        }

        var configName = args[0];

        if (!int.TryParse(args[1], out var memberLimit) || memberLimit < 0)
        {
            await channel.SendErrorCardAsync("指令格式错误！人数请使用阿拉伯数字正数，若要设置无限人数房间，请使用 0，举例：`!组队 默认人数 上分 4`");
            return;
        }

        await using var dbCtx = new DatabaseContext();
        var config = (
            from record in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where record.Name == configName
            select record
        ).FirstOrDefault();

        if (config == null)
        {
            await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置");
        }
        else
        {
            config.DefaultMemberLimit = memberLimit;
            dbCtx.SaveChanges();
            AppCaches.TeamPlayConfigs.Update($"{channel.Guild.Id}_{configName}", c =>
            {
                c.DefaultMemberLimit = memberLimit;
                return c;
            });
            await channel.SendSuccessCardAsync(
                $"更改默认人数成功，当前默认人数为：{(memberLimit == 0 ? "无限制" : memberLimit.ToString())}"
            );
        }
    }
}
