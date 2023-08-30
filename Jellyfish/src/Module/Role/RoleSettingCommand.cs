using System.Collections.Immutable;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.Role.Helper;
using Jellyfish.Util;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.Role;

/// <summary>
///     Role setting Command
/// </summary>
public class RoleSettingCommand : MessageCommand
{
    private readonly ImmutableHashSet<string> _commandNames;

    public RoleSettingCommand(IEnumerable<MessageCommand> commands)
    {
        _commandNames = commands.Select(c => c.Name()).ToImmutableHashSet();
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            可以限制「只有某些角色才能使用某指令」
            当指令**没有与任何角色绑定时，所有人都可以使用它**
            授权和解绑可以重复用在一个指令上，来为指令绑定/解绑多个角色权限
            """,
            """
            列表：列出全部配置的权限关系
            服务器角色：列出当前服务器的全部角色
            授权 [指令名称] [服务器角色名称]：设置该角色可以使用该指令
            解绑 [指令名称] [服务器角色名称]：将该指令中的该角色权限移除
            """);
    }

    public override string Name() => "权限配置指令";

    public override string[] Keywords() => new[] { "!权限", "！权限" };

    public override async Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await channel.SendTextAsync(HelpMessage);
        else if (args.StartsWith("列表"))
            await ListPermissions(channel);
        else if (args.StartsWith("服务器角色"))
            await ListGuildRoles(channel);
        else if (args.StartsWith("授权"))
            await BindingPermission(args[2..].Trim(), channel);
        else if (args.StartsWith("解绑"))
            await UnBindingPermission(args[2..].Trim(), channel);
        else await channel.SendTextAsync(HelpMessage);
    }

    /// <summary>
    ///     List configured permission
    /// </summary>
    /// <param name="channel">Current channel</param>
    private static async Task ListPermissions(SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();
        var roles = from role in dbCtx.UserRoles.Include(e => e.CommandPermissions).AsNoTracking()
            orderby role.KookId
            where role.GuildId == channel.Guild.Id
            from permission in role.CommandPermissions
            select role;
        var permissions = string.Join(
            "\n",
            roles.Select(r =>
                $"{r.GetName(channel.Guild)}：{string.Join("，", r.CommandPermissions.Select(p => p.CommandName))}"
            ));
        await channel.SendInfoCardAsync($"已配置的权限列表：\n{permissions}");
    }

    /// <summary>
    ///     List all role in the current guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    private static async Task ListGuildRoles(SocketTextChannel channel)
    {
        var rolenames = string.Join("\n",
            from role in channel.Guild.Roles
            orderby role.Name
            select role.Name
        );
        await channel.SendInfoCardAsync($"当前服务器角色：\n{rolenames}");
    }

    /// <summary>
    ///     Extract permission binding parameters
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    /// <param name="dbCtx">Opened database context</param>
    /// <returns>Parameters</returns>
    private async Task<(string commandName, string guildRoleName, uint guildRoleId, UserCommandPermission? record)?>
        ExtractPermissionBindingParameters(string rawArgs, SocketTextChannel channel, DatabaseContext dbCtx)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs);
        if (args.Length < 2)
        {
            await channel.SendWarningCardAsync("参数不足！举例：授权 权限配置指令 管理员");
            return null;
        }

        var commandName = args[0];
        if (_commandNames.All(it => it != commandName))
        {
            await channel.SendWarningCardAsync("指令不存在，您可以发送 “/帮助” 查看全部指令名称");
            return null;
        }

        var guildRoleName = args[1];
        var guildRoleId = channel.Guild.GetRoleIdByName(guildRoleName);
        if (guildRoleId == null)
        {
            await channel.SendWarningCardAsync("角色不存在，您可以发送 “!权限 服务器角色” 列出全部角色名称（或查看当前服务器配置）");
            return null;
        }

        var record = (
            from role in dbCtx.UserRoles.Include(e => e.CommandPermissions)
            from permission in role.CommandPermissions
            where role.KookId == guildRoleId && permission.CommandName == commandName
            select permission
        ).AsNoTracking().FirstOrDefault();

        return (commandName, guildRoleName, (uint)guildRoleId, record);
    }

    /// <summary>
    ///     Binding command permission to guild role
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    private async Task BindingPermission(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();

        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, dbCtx);

        if (parameters == null) return;

        var (commandName, guildRoleName, guildRoleId, record) = parameters.Value;

        if (record != null)
        {
            await channel.SendInfoCardAsync("您已设置过该权限");
        }
        else
        {
            await using var transaction = await dbCtx.Database.BeginTransactionAsync();

            #region Transaction

            var role = CreateOrGetRole(dbCtx, guildRoleId, channel.Guild.Id);
            dbCtx.UserCommandPermissions.Add(new UserCommandPermission(role.Id, commandName));
            dbCtx.SaveChanges();

            #endregion

            await transaction.CommitAsync();

            await channel.SendSuccessCardAsync($"权限绑定成功！角色 {guildRoleName} 可以执行 {commandName}");
        }
    }

    /// <summary>
    ///     Unbinding permission on command
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    private async Task UnBindingPermission(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();

        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, dbCtx);

        if (parameters == null) return;

        var (commandName, guildRoleName, _, record) = parameters.Value;

        if (record == null)
        {
            await channel.SendInfoCardAsync("权限已被解绑");
        }
        else
        {
            dbCtx.UserCommandPermissions.Remove(record);
            dbCtx.SaveChanges();
            await channel.SendSuccessCardAsync($"权限解绑成功！角色 {guildRoleName} 已无法使用 {commandName}");
        }
    }

    /// <summary>
    ///     Create or get user role in database, use to make sure user role config exists
    /// </summary>
    /// <param name="dbCtx">Database context</param>
    /// <param name="guildRoleId">Guild role id</param>
    /// <param name="guildId">Guild id to create role if not exist</param>
    /// <returns>User role object</returns>
    private static UserRole CreateOrGetRole(DatabaseContext dbCtx, uint guildRoleId, ulong guildId)
    {
        var record = (from role in dbCtx.UserRoles
            where role.KookId == guildRoleId
            select role).FirstOrDefault();

        if (record == null)
        {
            record = new UserRole(guildRoleId, guildId);
            dbCtx.UserRoles.Add(record);
        }

        dbCtx.SaveChanges();

        return record;
    }
}
