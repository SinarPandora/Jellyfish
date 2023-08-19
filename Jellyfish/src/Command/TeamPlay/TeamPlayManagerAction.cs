using Jellyfish.Core;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

public class TeamPlayManagerAction
{
    public static async Task<CommandResult> Help(SocketTextChannel channel)
    {
        await channel.SendTextAsync(
            """
            组队系统：管理员指令
            指令名称：！组队

            参数：
            帮助：显示此消息
            绑定父频道：设定组队频道所在的父频道
            默认语音质量 [低|中|高]：设定临时语音频道的质量
            """);
        return CommandResult.Done;
    }

    public async Task<CommandResult> BindingParentChannel(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel)
    {
        // user.VoiceChannel
        throw new NotImplementedException();
    }

    public async Task<CommandResult> SetDefaultQuality(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string msg)
    {
        throw new NotImplementedException();
    }
}
