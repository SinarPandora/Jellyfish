using Jellyfish.Command.TeamPlay.Data;
using Jellyfish.Loader;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;
using AppCtx = Jellyfish.Loader.AppContext;

namespace Jellyfish.Command.TeamPlay;

public class TeamPlayManagerAction
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
            列表：列出全部的组队频道
            绑定父频道 [父频道类型]：设定组队频道所在的父频道
            默认语音质量 [低|中|高]：设定临时语音频道的质量
            """);
    }

    public static async Task StartBindingParentChannel(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string name)
    {
        await using var dbCtx = new DatabaseContext();

        var names = dbCtx.TpConfigs.AsNoTracking()
            .Select(c => c.Name)
            .OrderBy(c => c)
            .ToArray();

        if (string.IsNullOrEmpty(name))
        {
            var message = "请设置父频道类型";
            if (!names.Any())
                message += $"""
                            ------------------
                            当前已设置的父频道类型：
                            {string.Join("\n", names)}
                            """;

            await channel.SendTextAsync(message);
        }
        else if (names.Contains(name))
        {
            Log.Info($"检测到名称重复，将启动重新绑定流程，目标类型：{name}");
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
                            > 您正在绑定 {name}
                            > 请先加入您要绑定的语音频道，该频道将成为自动创建语音频道的入口
                            > 加入频道后，请点击下方按钮
                            """, true);
            });
            // Button element
            cardBuilder.AddModule<ActionGroupModuleBuilder>(a =>
            {
                a.AddElement(b =>
                {
                    // Click this button will run DoBindingParentChannel with the name which user input
                    b.WithText("已加入语音频道")
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
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        Log.Info($"已收到名为 {name} 的绑定请求，执行进一步操作");
        var voiceChannel = user.Value.VoiceChannel;
        if (voiceChannel == null)
        {
            await channel.SendCardAsync(new CardBuilder()
                .AddModule<SectionModuleBuilder>(s => { s.WithText("❓未检测到您加入的语音频道"); })
                .Build());
        }
        else
        {
            Log.Info($"已检测到语音频道：{voiceChannel.Name}：{voiceChannel.Id}");
            await channel.SendCardAsync(new CardBuilder()
                .AddModule<SectionModuleBuilder>(s => { s.WithText($"✅检测到您加入了频道：{voiceChannel.Name}，正在绑定..."); })
                .Build());


            await using var dbCtx = new DatabaseContext();
            var record = dbCtx.TpConfigs
                .FirstOrDefault(e => e.Name == name);

            // Update or Insert
            if (record != null)
            {
                record.ChannelId = voiceChannel.Id;
            }
            else
            {
                record = new TpConfig(name, voiceChannel.Id);
                dbCtx.TpConfigs.Add(record);
            }

            await dbCtx.SaveChangesAsync();

            await channel.SendCardAsync(new CardBuilder()
                .AddModule<SectionModuleBuilder>(s => { s.WithText("✅绑定成功！"); })
                .Build());

            Log.Info($"成功绑定 {name} 到 {voiceChannel.Name}：{voiceChannel.Id}，ID：{record.Id}");
        }
    }

    public async Task SetDefaultQuality(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string msg)
    {
        throw new NotImplementedException();
    }
}
