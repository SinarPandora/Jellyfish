using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     TeamPlay management actions service
/// </summary>
public class TeamPlayManageService
{
    public const string UserInjectKeyword = "{USER}";
    private readonly ILogger<TeamPlayManageService> _log;
    private readonly DbContextProvider _dbProvider;

    public TeamPlayManageService(ILogger<TeamPlayManageService> log, DbContextProvider dbProvider)
    {
        _log = log;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Send binding wizard card message to the current channel
    /// </summary>
    /// <param name="user">Action user</param>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name</param>
    /// <returns>Is task success</returns>
    public async Task<bool> SendBindingWizard(SocketUser user, SocketTextChannel channel, string name)
    {
        await using var dbCtx = _dbProvider.Provide();
        var names = dbCtx.TpConfigs.EnabledInGuild(channel.Guild).AsNoTracking()
            .Select(c => c.Name)
            .ToHashSet();

        if (string.IsNullOrEmpty(name))
        {
            await channel.SendErrorCardAsync("请设置绑定名称，举例：`！组队 绑定 真格上分`", true);
            return false;
        }

        var cardBuilder = new CardBuilder();
        // Header element
        cardBuilder
            .AddModule<HeaderModuleBuilder>(m => m.Text = "欢迎使用组队绑定功能")
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText($"您正在{(names.Contains(name) ? "重新" : "")}绑定 {name}", true);
            })
            .AddModule<DividerModuleBuilder>()
            // Voice channel binding prompt
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText("""
                           🗣️您可以为当前配置绑定语音入口频道，该频道将成为后续自动创建语音频道的入口
                           绑定方法为：先加入目标频道，加入后，请点击下方按钮
                           """, true);
            })
            .AddModule<DividerModuleBuilder>()
            // Voice channel binding button
            .AddModule<ActionGroupModuleBuilder>(a =>
            {
                a.AddElement(b =>
                {
                    // Click this button will run DoBindingParentChannel with the name which user input
                    b.WithText("我已加入语音频道")
                        .WithClick(ButtonClickEventType.ReturnValue)
                        .WithValue($"tp_bind_{user.Id}_{name}")
                        .WithTheme(ButtonTheme.Primary);
                });
            })
            .AddModule<DividerModuleBuilder>()
            // Text channel binding prompt
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText($"""
                            💬您也可以同时绑定任意文字频道为入口频道，在目标频道发送由 /组队 开头的消息将自动创建对应房间
                            绑定方法为：`!组队 绑定文字频道 {name} [#引用文字频道]`
                            ---
                            引用的频道必须是一个 Kook 引用（在消息中显示为蓝色）
                            """, true);
            })
            .AddModule<DividerModuleBuilder>()
            // Other prompt
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText("当绑定了一个语音入口频道或文字入口频道后，配置就可以使用啦",
                    true);
            })
            .WithSize(CardSize.Large);

        await channel.SendCardAsync(cardBuilder.Build());
        _log.LogInformation("已发送绑定向导，目标类型：{Name}", name);

        return true;
    }

    /// <summary>
    ///     List all team play config
    /// </summary>
    /// <param name="channel">Message send to this channel</param>
    public async Task ListConfigs(SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
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
                    var voiceChannel = e.VoiceChannelId.HasValue
                        ? MentionUtils.KMarkdownMentionChannel(e.VoiceChannelId.Value)
                        : "未绑定";
                    var textChannel = e.TextChannelId.HasValue
                        ? MentionUtils.KMarkdownMentionChannel(e.TextChannelId.Value)
                        : "未绑定";
                    return $"ID：{e.Id}，名称：{e.Name}，语音入口：{voiceChannel}，" +
                           $"文字入口：{textChannel}，当前语音房间数：{e.RoomInstances.Count}";
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
    public async Task BindingVoiceChannel(string name, Cacheable<SocketGuildUser, ulong> user,
        SocketTextChannel channel)
    {
        _log.LogInformation("已收到名为 {Name} 的语音频道绑定请求，执行进一步操作", name);
        var voiceChannel = user.Value.VoiceChannel;
        if (voiceChannel == null)
        {
            await channel.SendErrorCardAsync("未检测到您加入的语音频道", true);
        }
        else
        {
            _log.LogInformation("已检测到语音频道：{VoiceChannelName}：{VoiceChannelId}", voiceChannel.Name, voiceChannel.Id);
            await channel.SendInfoCardAsync($"检测到您加入了频道：{voiceChannel.Name}，正在绑定...", false);

            await using var dbCtx = _dbProvider.Provide();
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
            dbCtx.SaveChanges();
            AppCaches.TeamPlayConfigs.AddOrUpdate($"{channel.Guild.Id}_{name}", config);
            await channel.SendSuccessCardAsync(
                $"绑定成功！加入 {MentionUtils.KMarkdownMentionChannel(voiceChannel.Id)} 将自动创建 {name} 类型的房间", false);
            await SendFurtherConfigIntroMessage(channel, config);

            _log.LogInformation("成功绑定 {Name} 到 {VoiceChannelName}：{VoiceChannelId}，ID：{ConfigId}",
                name, voiceChannel.Name, voiceChannel.Id, config.Id);
        }
    }

    /// <summary>
    ///     Binding text channel to config
    /// </summary>
    /// <param name="channel">Channel to binding</param>
    /// <param name="rawArgs">Command args</param>
    /// <returns>Is task success</returns>
    public async Task<bool> BindingTextChannel(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);

        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync(
                """
                参数不足！举例：`!组队 绑定文字频道 配置名称 #引用现有文字频道`
                 引用的频道必须是一个 Kook 引用（在消息中显示为蓝色）
                """,
                true);
            return false;
        }

        var configName = args[0];
        var rawMention = args[1];

        if (!rawMention.StartsWith("(chn)"))
        {
            await channel.SendErrorCardAsync("请在指令中引用现有文字频道，具体内容可以参考：`!组队 帮助`", true);
            return false;
        }

        var chnMatcher = Regexs.MatchTextChannelMention().Match(rawMention);
        if (!ulong.TryParse(chnMatcher.Groups["channelId"].Value, out var bindingChannelId))
        {
            await channel.SendErrorCardAsync("现有文字频道引用应是一个频道引用（蓝色文本），具体内容可以参考：`!组队 帮助`", true);
            return false;
        }

        var bindingChannelName = channel.Guild.GetTextChannel(bindingChannelId).Name;

        await using var dbCtx = _dbProvider.Provide();
        _log.LogInformation("已收到名为 {Name} 的文字频道绑定请求，执行进一步操作", configName);
        var config = dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            .FirstOrDefault(e => e.Name == configName);
        if (config == null)
        {
            config = new TpConfig(configName, channel.Guild.Id)
            {
                TextChannelId = bindingChannelId
            };
            dbCtx.TpConfigs.Add(config);
        }
        else
        {
            config.TextChannelId = bindingChannelId;
        }

        // Refresh voice quality when updating
        dbCtx.SaveChanges();
        AppCaches.TeamPlayConfigs.AddOrUpdate($"{channel.Guild.Id}_{configName}", config);
        await channel.SendSuccessCardAsync(
            $"绑定成功！{MentionUtils.KMarkdownMentionChannel(bindingChannelId)}已与组队配置 {configName} 绑定", false
        );

        _log.LogInformation("成功绑定 {Name} 到 {ChannelName}：{ChannelId}，ID：{ConfigId}",
            configName, bindingChannelName, bindingChannelId, config.Id);

        await SendFurtherConfigIntroMessage(channel, config);
        return true;
    }

    /// <summary>
    ///     Remove specified configuration
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name to remove</param>
    /// <returns>Is task success</returns>
    public async Task<bool> RemoveConfig(SocketTextChannel channel, string name)
    {
        await using var dbCtx = _dbProvider.Provide();
        var record = (
            from config in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where config.Name == name
            select config
        ).FirstOrDefault();
        if (record == null)
        {
            await channel.SendErrorCardAsync($"规则 {name} 未找到或已被删除", true);
            return false;
        }

        record.Enabled = false;
        dbCtx.SaveChanges();
        if (!AppCaches.TeamPlayConfigs.Remove($"{channel.Guild.Id}_{name}", out _))
        {
            _log.LogWarning("组队配置缓存缺失，可能是一个 bug。配置键：{GuildId}_{Name}", channel.Guild.Id, name);
        }

        await channel.SendSuccessCardAsync($"规则 {name} 删除成功！已创建的房间将保留直到无人使用", false);
        return true;
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
                     语音质量：当前频道默认最高质量
                     ---
                     **设置房间名格式：**
                     > `!组队 房间名格式 {config.Name} 房间名格式`

                     使用 {UserInjectKeyword} 代表用户输入内容
                     举例如下：
                     1. 指令：`!组队 房间名格式 {config.Name} 【上分】{UserInjectKeyword}`
                     2. 用户发送指令创建房间：`/组队 XP1700开放`
                     3. 最终房间名为：`【上分】XP1700开放`
                     若房间重名，将在房间后面添加序号来进行区分
                     ---
                     **设置默认人数：**
                     > `!组队 默认人数 {config.Name} [数字]`

                     设定创建语音房间的默认人数，输入 0 代表人数无限
                     **默认人数无限**
                     ---
                     **设置通知文字频道**
                     > `!组队 通知文字频道 {config.Name} [#引用现有文字频道]`

                     设置后，通过语音频道自动创建的房间将会向该频道发送通知（若未设置则使用指令文字频道）
                     ---
                     **设置房间所在分组**
                     > `!组队 语音频道分组 {config.Name} [#引用现有文字频道]`
                     > `!组队 文字频道分组 {config.Name} [#引用现有文字频道]`

                     [#引用现有文字频道]：指的是一个文字频道的 Kook 引用，用于获取其所属的分类频道（因为 Kook 无法直接引用分类频道）
                     默认语音房间将创建在上一步中绑定的语音所在分组，文字房间将创建在上一步中绑定的文字房间所在分组。
                     """, true);
            })
            .WithSize(CardSize.Large)
            .Build());
    }

    /// <summary>
    ///     Set new room instance's name pattern
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    /// <returns>Is task success</returns>
    public async Task<bool> SetRoomPattern(SocketTextChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync($"参数不足！举例：`!组队 房间名格式 上分 【上分】{UserInjectKeyword}`", true);
            return false;
        }

        var configName = args[0];
        var pattern = args[1];
        if (!pattern.Contains(UserInjectKeyword))
        {
            await channel.SendWarningCardAsync(
                $"名称格式中不包含用户输入的任何内容，这将导致创建的房间重名，" +
                $"从而影响分辨，您可以在格式中添加 {UserInjectKeyword} 来代表用户输入的内容", false);
        }

        await using var dbCtx = _dbProvider.Provide();
        var config = (
            from record in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where record.Name == configName
            select record
        ).FirstOrDefault();

        if (config == null)
        {
            await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置", true);
            return false;
        }

        config.RoomNamePattern = pattern;
        dbCtx.SaveChanges();
        AppCaches.TeamPlayConfigs[$"{channel.Guild.Id}_{configName}"].RoomNamePattern = pattern;
        await channel.SendSuccessCardAsync($"更改房间名格式成功，新房间名称格式为：{pattern}", false);
        return true;
    }

    /// <summary>
    ///     Set new room default member count
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    /// <returns>Is task success</returns>
    public async Task<bool> SetDefaultMemberCount(SocketTextChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！举例：`!组队 默认人数 上分 4`", true);
            return false;
        }

        var configName = args[0];

        if (!int.TryParse(args[1], out var memberLimit) || memberLimit < 0)
        {
            await channel.SendErrorCardAsync("指令格式错误！人数请使用阿拉伯数字正数，若要设置无限人数房间，请使用 0，举例：`!组队 默认人数 上分 4`", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();
        var config = (
            from record in dbCtx.TpConfigs.EnabledInGuild(channel.Guild)
            where record.Name == configName
            select record
        ).FirstOrDefault();

        if (config == null)
        {
            await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置", true);
            return false;
        }

        config.DefaultMemberLimit = memberLimit;
        dbCtx.SaveChanges();
        AppCaches.TeamPlayConfigs[$"{channel.Guild.Id}_{args[0]}"].DefaultMemberLimit = memberLimit;
        await channel.SendSuccessCardAsync(
            $"更改默认人数成功，当前默认人数为：{(memberLimit == 0 ? "无限制" : memberLimit.ToString())}",
            false
        );

        return true;
    }

    /// <summary>
    ///     Configure the category channel for team play room (voice or text channel)
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawArgs">Command args</param>
    /// <param name="channelType">Target channel type</param>
    /// <returns>Is task success</returns>
    public async Task<bool> SetCategoryChannel(SocketTextChannel channel, string rawArgs,
        AdditionChannelType channelType)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);

        var channelTypeName = channelType switch
        {
            AdditionChannelType.TmpTextCategoryInto => "文字频道分组",
            AdditionChannelType.TmpVoiceCategoryInto => "语音频道分组",
            AdditionChannelType.CreationNotify => "通知文字频道",
            _ => throw new ArgumentOutOfRangeException(nameof(channelType), channelType, "内部错误，使用了无法处理的枚举值")
        };

        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync(
                $"""
                 参数不足！举例：`!组队 {channelTypeName} 配置名称 #引用现有文字频道`
                  引用的频道必须是一个 Kook 引用（在消息中显示为蓝色）
                 """,
                true);
            return false;
        }

        var configName = args[0];
        await using var dbCtx = _dbProvider.Provide();

        var tpConfig =
            (from c in dbCtx.TpConfigs
                where c.GuildId == channel.Guild.Id && c.Name == configName && c.Enabled
                select c).FirstOrDefault();

        if (tpConfig == null)
        {
            await channel.SendErrorCardAsync("配置不存在，您可以发送：`!组队 列表` 查看现有配置", true);
            return false;
        }

        var rawMention = args[1];

        if (!rawMention.StartsWith("(chn)"))
        {
            await channel.SendErrorCardAsync("请在指令中引用现有文字频道，具体内容可以参考：`!组队 帮助`", true);
            return false;
        }

        var chnMatcher = Regexs.MatchTextChannelMention().Match(rawMention);
        if (!ulong.TryParse(chnMatcher.Groups["channelId"].Value, out var textChannelId))
        {
            await channel.SendErrorCardAsync("现有文字频道引用应是一个频道引用（蓝色文本），具体内容可以参考：`!组队 帮助`", true);
            return false;
        }

        if (channelType == AdditionChannelType.CreationNotify)
        {
            tpConfig.CreationNotifyChannelId = textChannelId;
            AppCaches.TeamPlayConfigs[$"{channel.Guild.Id}_{configName}"].CreationNotifyChannelId = textChannelId;
        }
        else
        {
            var categoryId = channel.Guild.GetTextChannel(textChannelId).CategoryId;

            if (!categoryId.HasValue)
            {
                await channel.SendErrorCardAsync("指定的文字频道不属于任何分组，为确保频道列表简洁，请重新选择一个带有分组的文字频道", true);
                return false;
            }

            if (channelType == AdditionChannelType.TmpTextCategoryInto)
            {
                tpConfig.TextCategoryId = categoryId;
                AppCaches.TeamPlayConfigs[$"{channel.Guild.Id}_{configName}"].TextCategoryId = categoryId;
            }
            else
            {
                tpConfig.VoiceCategoryId = categoryId;
                AppCaches.TeamPlayConfigs[$"{channel.Guild.Id}_{configName}"].VoiceCategoryId = categoryId;
            }
        }

        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync($"{channelTypeName}配置成功！", false);
        return true;
    }
}
