using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Core.Kook.Protocol;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;
using AppCtx = Jellyfish.Core.Container.AppContext;
using VoiceQuality = Jellyfish.Core.Kook.Protocol.VoiceQuality;

namespace Jellyfish.Module.TeamPlay;

/// <summary>
///     Team play config manage command
/// </summary>
public class TeamPlayManageCommand : MessageCommand
{
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
            """
            帮助：显示此消息
            列表：列出全部的组队配置
            配置 [配置名称]：调整指定组队配置
            绑定文字频道 [配置名称]：在目标频道中使用，设置后，该频道发送的组队质量会使用该配置创建语音频道
            设置语音质量 [配置名称] [低|中|高]：设定临时语音频道的质量
            删除 [配置名称]：删除指定配置
            """);
    }

    public override string Name() => "管理组队配置指令";

    public override IEnumerable<string> Keywords() => new[] { "!组队", "！组队" };

    public override async Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await channel.SendTextAsync(HelpMessage);
        else if (args.StartsWith("配置"))
            await SendBindingWizard(channel, args[2..].TrimStart());
        else if (args.StartsWith("绑定文字频道"))
            await BindingTextChannel(channel, args[6..].TrimStart());
        else if (args.StartsWith("设置语音质量"))
            await SetDefaultQuality(channel, args[6..].TrimStart());
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
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name</param>
    private static async Task SendBindingWizard(SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();

        var names = dbCtx.TpConfigs.AsNoTracking()
            .Select(c => c.Name)
            .OrderBy(c => c)
            .ToArray();

        if (string.IsNullOrEmpty(name))
        {
            await channel.SendWarningCardAsync("请设置绑定名称，举例：！组队 绑定 真格上分");
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
                        .WithValue($"tp_v_bind_{name}")
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
        var configRecords = dbCtx.TpConfigs
            .Where(e => e.Enabled)
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
                           $"语音质量：{VoiceQualityHelper.GetName(e.VoiceQuality)}，当前语音房间数：{e.RoomInstances.Count}";
                })
                .ToArray();
            await channel.SendTextAsync(string.Join("\n", configs));
        }
    }

    /// <summary>
    ///     Binding text channel to config
    /// </summary>
    /// <param name="channel">Channel to binding</param>
    /// <param name="name">Config name</param>
    private static async Task BindingTextChannel(SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();
        var config = dbCtx.TpConfigs.FirstOrDefault(e => e.Name == name);
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
        config.VoiceQuality = VoiceQualityHelper.GetHighestInGuild(channel.Guild);
        dbCtx.SaveChanges();
        await channel.SendSuccessCardAsync($"绑定成功！当前频道已与组队配置 {name} 绑定");
    }

    /// <summary>
    ///     Set default voice quality, only for temporary updates when the boost level drops
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="msg">Message that contains args</param>
    private static async Task SetDefaultQuality(IMessageChannel channel, string msg)
    {
        var args = Regexs.MatchWhiteChars().Split(msg);
        if (args.Length < 2)
        {
            await channel.SendWarningCardAsync("参数不足！举例：!组队 语音质量 休闲 高");
            return;
        }

        var quality = VoiceQualityHelper.FromName(args[1]);
        if (quality == null)
        {
            await channel.SendWarningCardAsync("请从以下选项中选择语音质量：低，中，高");
        }
        else
        {
            await using var dbCtx = new DatabaseContext();
            var config = dbCtx.TpConfigs.FirstOrDefault(e => e.Name == args[0]);
            if (config == null)
            {
                await channel.SendWarningCardAsync("配置不存在，您可以发送：“!组队 列表” 查看现有配置");
            }
            else
            {
                config.VoiceQuality = (VoiceQuality)quality;
                dbCtx.SaveChanges();
                await channel.SendSuccessCardAsync("设置成功！");
            }
        }
    }

    /// <summary>
    ///     Remove specified configuration
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Config name to remove</param>
    private static async Task RemoveConfig(IMessageChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();
        var record = (
            from config in dbCtx.TpConfigs
            where config.Name == name && config.Enabled
            select config
        ).FirstOrDefault();
        if (record == null)
        {
            await channel.SendWarningCardAsync($"规则 {name} 未找到或已被删除");
        }
        else
        {
            record.Enabled = false;
            dbCtx.SaveChanges();
            await channel.SendErrorCardAsync($"规则 {name} 删除成功！已创建的房间将保留直到无人使用");
        }
    }
}
