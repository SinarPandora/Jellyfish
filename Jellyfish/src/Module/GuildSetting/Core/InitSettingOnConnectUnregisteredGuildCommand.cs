using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Kook.WebSocket;

namespace Jellyfish.Module.GuildSetting.Core;

/// <summary>
///     Init empty setting when bot connected an unregistered guild
/// </summary>
public class InitSettingOnConnectUnregisteredGuildCommand(
    DbContextProvider dbProvider,
    ILogger<InitSettingOnConnectUnregisteredGuildCommand> log
) : GuildAvailableCommand
{
    public override string Name() => "初始化服务器配置指令（连接时）";

    public override async Task<CommandResult> Execute(SocketGuild guild)
    {
        if (AppCaches.GuildSettings.ContainsKey(guild.Id))
            return CommandResult.Continue;
        log.LogInformation(
            "发现频道 {Name} 尚未被注册到系统中，开始注册并添加初始配置",
            guild.Name
        );

        await using var dbCtx = dbProvider.Provide();
        var setting = new Data.GuildSetting(guild.Id);
        dbCtx.GuildSettings.Add(setting);
        AppCaches.GuildSettings.AddOrUpdate(guild.Id, setting.Setting);
        dbCtx.SaveChanges();

        log.LogInformation("频道 {Name} 已注册", guild.Name);
        return CommandResult.Continue;
    }
}
