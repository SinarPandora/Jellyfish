using System.Collections.Immutable;
using Jellyfish.Core.Command;
using Jellyfish.Module.Role.Core;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.Help;

/// <summary>
///     Global help command
/// </summary>
public class GlobalHelpCommand : GuildMessageCommand
{
    private readonly Lazy<ImmutableArray<GuildMessageCommand>> _commands;

    public GlobalHelpCommand(IServiceScopeFactory provider)
        : base(false)
    {
        _commands = new Lazy<ImmutableArray<GuildMessageCommand>>(() =>
        {
            using var scope = provider.CreateScope();
            return
            [
                .. scope
                    .ServiceProvider.GetServices<GuildMessageCommand>()
                    .Where(e => e.Name() != "全局帮助指令"),
            ];
        });
    }

    public override string Name() => "全局帮助指令";

    public override string[] Keywords() => ["/帮助"];

    protected override async Task Execute(
        string args,
        string keyword,
        SocketMessage msg,
        SocketGuildUser user,
        SocketTextChannel channel
    )
    {
        var availableCommands = _commands
            .Value.Where(user.CanExecute)
            .Select(command =>
                $"{command.Name()}：{command.Keywords().FirstOrDefault() ?? "被动触发"}"
            )
            .StringJoin("\n");

        await channel.SendCardSafeAsync(
            new CardBuilder()
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
                        """,
                        true
                    );
                })
                .Build()
        );
    }
}
