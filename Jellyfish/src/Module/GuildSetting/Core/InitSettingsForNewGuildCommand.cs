using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Kook.WebSocket;

namespace Jellyfish.Module.GuildSetting.Core;

/// <summary>
///     Init empty setting when bot join to a new guild
/// </summary>
public class InitSettingsForNewGuildCommand(DbContextProvider dbProvider) : BotJoinGuildCommand
{
    public override string Name() => "初始化服务器配置指令";

    public override async Task<CommandResult> Execute(SocketGuild guild)
    {
        await using var dbCtx = dbProvider.Provide();
        if (dbCtx.GuildSettings.Any(s => s.GuildId == guild.Id))
            return CommandResult.Continue;

        var setting = new Data.GuildSetting(guild.Id);
        dbCtx.GuildSettings.Add(setting);
        AppCaches.GuildSettings.AddOrUpdate(guild.Id, setting.Setting);

        return CommandResult.Continue;
    }
}
