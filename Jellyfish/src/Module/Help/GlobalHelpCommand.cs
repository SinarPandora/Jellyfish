using System.Collections.Immutable;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.Help;

/// <summary>
///     Global help command
/// </summary>
public class GlobalHelpCommand : MessageCommand
{
    private readonly ImmutableArray<MessageCommand> _commands;

    public GlobalHelpCommand(IEnumerable<MessageCommand> commands)
    {
        _commands = commands.Where(e => e.Name() != "全局帮助指令").ToImmutableArray();
    }

    public override string Name() => "全局帮助指令";

    public override string[] Keywords() => new[] { "/帮助" };

    public override async Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var cache = AppCaches.Permissions;
        var userGuildRoles = user.Roles.Select(it => it.Id).ToArray();
        var availableCommands =
            string.Join("\n",
                from command in _commands
                let cacheKey = $"{user.Guild.Id}_{command.Name()}"
                where !cache.ContainsKey(cacheKey) || cache.GetValueOrDefault(cacheKey).ContainsAny(userGuildRoles)
                orderby command.Name()
                select $"{command.Name()}：{command.Keywords().First()}"
            );

        await channel.SendCardAsync(new CardBuilder()
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText(
                    $"""
                     您好，我是「随处可见的」员工水母🪼
                     很高兴为您服务
                     ---
                     以下是我可以做的事情：
                     指令名称    触发关键字
                     {availableCommands}
                     ---
                     每个指令都附带帮助功能，
                     例如发送：/组队 帮助
                     可以查看「组队房间指令」的帮助信息
                     """, true);
            })
            .Build());
    }
}
