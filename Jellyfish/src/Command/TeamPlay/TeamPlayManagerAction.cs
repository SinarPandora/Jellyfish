using Jellyfish.Command.TeamPlay.Data;
using Jellyfish.Core.Protocol;
using Jellyfish.Loader;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;
using AppCtx = Jellyfish.Loader.AppContext;
using VoiceQuality = Jellyfish.Core.Protocol.VoiceQuality;

namespace Jellyfish.Command.TeamPlay;

public static class TeamPlayManagerAction
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task Help(SocketTextChannel channel)
    {
        await channel.SendTextAsync(
            """
            组队系统：管理员指令
            指令名称：！组队

            参数：
            帮助：显示此消息
            列表：列出全部的组队频道配置
            父频道 [父频道类型]：设定组队频道所在的父频道
            语音质量 [配置 ID] [低|中|高]：设定临时语音频道的质量，配置 ID 可以通过“列表”指令获取
            """);
    }

    public static async Task StartBindingParentChannel(SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();

        var names = dbCtx.TpConfigs.AsNoTracking()
            .Select(c => c.Name)
            .OrderBy(c => c)
            .ToArray();

        if (string.IsNullOrEmpty(name))
        {
            await channel.SendWarningCardAsync("请设置父频道类型，举例：！组队 父频道 真格上分");
        }
        else
        {
            Log.Info($"检测到新类型绑定，启动绑定流程，目标类型：{name}");
            var cardBuilder = new CardBuilder();
            // Header element
            cardBuilder.AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText($"""
                            **欢迎使用组队绑定功能**
                            > 您正在{(names.Contains(name) ? "重新" : "")}绑定 {name}
                            > 请先加入任意语音频道，该频道将成为后续自动创建语音频道的入口
                            > 加入后，请点击下方按钮
                            """, true);
            });
            // Button element
            cardBuilder.AddModule<ActionGroupModuleBuilder>(a =>
            {
                a.AddElement(b =>
                {
                    // Click this button will run DoBindingParentChannel with the name which user input
                    b.WithText("我已加入语音频道")
                        .WithClick(ButtonClickEventType.ReturnValue)
                        .WithValue($"tp_binding_{name}")
                        .WithTheme(ButtonTheme.Primary);
                });
            });

            await channel.SendCardAsync(cardBuilder.Build());
            Log.Info($"已发送绑定向导，目标类型：{name}");
        }
    }

    public static async Task DoBindingParentChannel(string name, Cacheable<SocketGuildUser, ulong> user,
        SocketTextChannel channel)
    {
        Log.Info($"已收到名为 {name} 的绑定请求，执行进一步操作");
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
            var record = dbCtx.TpConfigs
                .FirstOrDefault(e => e.Name == name);

            // Update or Insert
            if (record != null)
            {
                record.VoiceQuality = channel.Guild.BoostLevel > BoostLevel.None
                    ? VoiceQuality.High
                    : VoiceQuality.Medium;
                record.ChannelId = voiceChannel.Id;
            }
            else
            {
                record = new TpConfig(name, voiceChannel.Id, channel.Guild.Id);
                dbCtx.TpConfigs.Add(record);
            }

            dbCtx.SaveChanges();

            await channel.SendSuccessCardAsync("绑定成功！");

            Log.Info($"成功绑定 {name} 到 {voiceChannel.Name}：{voiceChannel.Id}，ID：{record.Id}");
        }
    }

    public static async Task SetDefaultQuality(SocketTextChannel channel, string msg)
    {
        var args = msg.Split(" ");
        if (args.Length < 2)
        {
            await channel.SendWarningCardAsync("参数不足！举例：语音质量 1 高");
            return;
        }

        var quality = VoiceQualityNames.FromName(args[1]);
        if (quality == null)
        {
            await channel.SendWarningCardAsync("请从以下选项中选择语音质量：低，中，高");
        }
        else
        {
            if (!int.TryParse(args[1], out var configId))
            {
                await channel.SendWarningCardAsync("配置 ID 应为数字，举例：语音质量 1 高");
            }
            else
            {
                await using var dbCtx = new DatabaseContext();
                var config = dbCtx.TpConfigs.FirstOrDefault(e => e.Id == configId);
                if (config == null)
                {
                    await channel.SendWarningCardAsync("配置 ID 不存在，请使用：“！组队 列表”指令查看现有配置");
                }
                else
                {
                    config.VoiceQuality = (VoiceQuality)quality;
                    dbCtx.SaveChanges();
                    await channel.SendSuccessCardAsync("设置成功！");
                }
            }
        }
    }

    public static async Task ListConfigs(SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();
        var configRecords = dbCtx.TpConfigs.OrderByDescending(e => e.Name).ToArray();
        if (!configRecords.Any())
        {
            await channel.SendTextAsync("您还没有配置任何组队语音频道");
        }
        else
        {
            var configs = configRecords
                .Select(e =>
                    $"ID：{e.Id}，名称：{e.Name}，频道：{MentionUtils.KMarkdownMentionChannel(e.ChannelId)}，" +
                    $"语音质量：{VoiceQualityNames.Get(e.VoiceQuality)}，当前语音房间数：{e.RoomInstances.Count}"
                )
                .ToArray();
            await channel.SendTextAsync(string.Join("\n", configs));
        }
    }
}
