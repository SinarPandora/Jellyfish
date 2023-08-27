using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using NLog;
using VoiceQuality = Jellyfish.Core.Kook.Protocol.VoiceQuality;

namespace Jellyfish.Module.TeamPlay;

/// <summary>
///     Team play card action
/// </summary>
public class TeamPlayButtonActionEntry : ButtonActionCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override string Name() => "组队游戏卡片操作";

    public override async Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (value.StartsWith("tp_binding_"))
        {
            await BindingParentChannel(value[11..], user, channel);
            return CommandResult.Done;
        }

        return CommandResult.Continue;
    }

    private static async Task BindingParentChannel(string name, Cacheable<SocketGuildUser, ulong> user,
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
                record.VoiceChannelId = voiceChannel.Id;
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
}
