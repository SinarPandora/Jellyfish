using System.Collections.Immutable;
using Jellyfish.Core.Cache;
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
public class RoleSettingCommand : GuildMessageCommand
{
    private readonly Lazy<ImmutableHashSet<string>> _commandNames;
    private readonly DatabaseContext _dbCtx;

    public RoleSettingCommand(IServiceScopeFactory provider, DatabaseContext dbCtx)
    {
        _dbCtx = dbCtx;
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            可以限制「只有某些角色才能使用某指令」
            当指令**没有与任何角色绑定时，所有人都可以使用它**
            授权和解绑可以重复用在一个指令上，来为指令绑定/解绑多个角色权限
            ---
            使用 `/帮助` 指令查看所有可用指令
            """,
            """
            1. 列表：列出全部配置的权限关系
            2. 服务器角色：列出当前服务器的全部角色
            3. 授权 [指令名称] [服务器角色名称]：设置该角色可以使用该指令
            4. 解绑 [指令名称] [服务器角色名称]：将该指令中的该角色权限移除
            """);

        _commandNames = new Lazy<ImmutableHashSet<string>>(() =>
            {
                using var scope = provider.CreateScope();
                return scope.ServiceProvider
                    .GetServices<GuildMessageCommand>()
                    .Select(c => c.Name())
                    .ToImmutableHashSet();
            }
        );
    }

    public override string Name() => "权限配置指令";

    public override string[] Keywords() => new[] { "!权限", "！权限" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        var isSuccess = true;
        if (args.StartsWith("帮助"))
            await channel.SendCardAsync(HelpMessage);
        else if (args.StartsWith("列表"))
            await ListPermissions(channel);
        else if (args.StartsWith("服务器角色"))
            await ListGuildRoles(channel);
        else if (args.StartsWith("授权"))
            isSuccess = await BindingPermission(args[2..].TrimStart(), channel);
        else if (args.StartsWith("解绑"))
            isSuccess = await UnBindingPermission(args[2..].TrimStart(), channel);
        else
            await channel.SendCardAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    /// <summary>
    ///     List configured permission
    /// </summary>
    /// <param name="channel">Current channel</param>
    private async Task ListPermissions(SocketTextChannel channel)
    {
        var roles = from role in _dbCtx.UserRoles.Include(e => e.CommandPermissions).AsNoTracking()
            orderby role.KookId
            where role.GuildId == channel.Guild.Id
            from permission in role.CommandPermissions
            select role;
        var permissions = string.Join(
            "\n",
            roles.Select(r =>
                $"{r.GetName(channel.Guild)}：{string.Join("，", r.CommandPermissions.Select(p => p.CommandName))}"
            ));
        await channel.SendInfoCardAsync($"已配置的权限列表：\n{permissions}", false);
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
        await channel.SendInfoCardAsync($"当前服务器角色：\n{rolenames}", false);
    }

    /// <summary>
    ///     Extract permission binding parameters
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    /// <param name="_dbCtx">Opened database context</param>
    /// <returns>Parameters</returns>
    private async Task<(string commandName, string guildRoleName, uint guildRoleId, UserCommandPermission? record)?>
        ExtractPermissionBindingParameters(string rawArgs, SocketTextChannel channel, DatabaseContext _dbCtx)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！举例：`!权限 授权 权限配置指令 管理员`", true);
            return null;
        }

        var commandName = args[0];
        if (_commandNames.Value.All(it => it != commandName))
        {
            await channel.SendErrorCardAsync("指令不存在，您可以发送 `/帮助` 查看全部指令名称", true);
            return null;
        }

        var guildRoleName = args[1];
        var guildRoleId = channel.Guild.GetRoleIdByName(guildRoleName);
        if (guildRoleId == null)
        {
            await channel.SendErrorCardAsync("角色不存在，您可以发送 `!权限 服务器角色` 列出全部角色名称（或查看当前服务器配置）", true);
            return null;
        }

        var record = (
            from role in _dbCtx.UserRoles.Include(e => e.CommandPermissions)
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
    /// <returns>Is task success</returns>
    private async Task<bool> BindingPermission(string rawArgs, SocketTextChannel channel)
    {
        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, _dbCtx);

        if (parameters == null) return false;

        var (commandName, guildRoleName, guildRoleId, record) = parameters.Value;

        if (record != null)
        {
            await channel.SendInfoCardAsync("您已设置过该权限", true);
            return true;
        }

        await using var transaction = await _dbCtx.Database.BeginTransactionAsync();

        #region Transaction

        var role = CreateOrGetRole(_dbCtx, guildRoleId, channel.Guild.Id);
        _dbCtx.UserCommandPermissions.Add(new UserCommandPermission(role.Id, commandName));
        _dbCtx.SaveChanges();

        #endregion

        await transaction.CommitAsync();

        // Update cache
        AppCaches.Permissions.AddOrUpdate($"{channel.Guild.Id}_{commandName}",
            new HashSet<uint> { guildRoleId },
            (_, v) =>
            {
                v.Add(role.KookId);
                return v;
            });

        await channel.SendSuccessCardAsync($"权限绑定成功！角色 {guildRoleName} 可以执行 {commandName}", false);
        return true;
    }

    /// <summary>
    ///     Unbinding permission on command
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success</returns>
    private async Task<bool> UnBindingPermission(string rawArgs, SocketTextChannel channel)
    {
        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, _dbCtx);

        if (parameters == null) return false;

        var (commandName, guildRoleName, guildRoleId, record) = parameters.Value;

        if (record == null)
        {
            await channel.SendInfoCardAsync("权限已被解绑", false);
        }
        else
        {
            _dbCtx.UserCommandPermissions.Remove(record);
            _dbCtx.SaveChanges();

            // Update cache
            var cacheKey = $"{channel.Guild.Id}_{commandName}";
            if (AppCaches.Permissions.ContainsKey(cacheKey))
            {
                AppCaches.Permissions.GetValueOrDefault(cacheKey)?.Remove(guildRoleId);
            }

            await channel.SendSuccessCardAsync($"权限解绑成功！角色 {guildRoleName} 已无法使用 {commandName}", false);
        }

        return true;
    }

    /// <summary>
    ///     Create or get user role in database, use to make sure user role config exists
    /// </summary>
    /// <param name="_dbCtx">Database context</param>
    /// <param name="guildRoleId">Guild role id</param>
    /// <param name="guildId">Guild id to create role if not exist</param>
    /// <returns>User role object</returns>
    private static UserRole CreateOrGetRole(DatabaseContext _dbCtx, uint guildRoleId, ulong guildId)
    {
        var record = (from role in _dbCtx.UserRoles
            where role.KookId == guildRoleId
            select role).FirstOrDefault();

        if (record == null)
        {
            record = new UserRole(guildRoleId, guildId);
            _dbCtx.UserRoles.Add(record);
        }

        _dbCtx.SaveChanges();

        return record;
    }
}
