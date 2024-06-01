using System.Collections.Immutable;
using System.Text.RegularExpressions;
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
    private readonly DbContextProvider _dbProvider;

    public RoleSettingCommand(IServiceScopeFactory provider, DbContextProvider dbProvider) : base(true)
    {
        _dbProvider = dbProvider;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            可以限制「只有某些角色才能使用某指令」
            当指令**没有与任何角色绑定时，所有人都可以使用它**
            授权和解绑可以重复用在一个指令上，来为指令绑定/解绑多个角色权限
            ---
            ⚠️在不额外设置权限时，所有管理指令（一般以叹号开头）只允许带有管理员权限的用户使用
            ---
            使用 `/帮助` 指令查看所有可用指令
            """,
            """
            1. 列表：列出全部配置的权限关系
            2. 服务器角色：列出当前服务器的全部角色
            3. 设置默认管理员 [#用户/角色引用]（支持多个）：设置默认管理员用户/角色（将替换之前的设置）
            4. 授权 [指令名称] [服务器角色名称]：设置该角色可以使用该指令，与上一功能不同，
            指定的角色可能包含很多人，为避免打扰，此处需要提供名称，而不是使用@，下同
            5. 解绑 [指令名称] [服务器角色名称]：将该指令中的该角色权限移除
            ---
            #用户/角色引用：指的是在聊天框中输入 @ 并选择的用户/角色，在 Kook 中显示为蓝色文字。直接输入用户/角色名是无效的。
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

    public override string[] Keywords() => ["!权限", "！权限"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        var isSuccess = true;
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
            await channel.SendCardSafeAsync(HelpMessage);
        else if (args.StartsWith("列表"))
            await ListPermissions(channel);
        else if (args.StartsWith("服务器角色"))
            await ListGuildRoles(channel);
        else if (args.StartsWith("设置默认管理员"))
            isSuccess = await SetDefaultManagerAccountsAndRoles(args[7..].Trim(), channel);
        else if (args.StartsWith("授权"))
            isSuccess = await BindingPermission(args[2..].TrimStart(), channel);
        else if (args.StartsWith("解绑"))
            isSuccess = await UnBindingPermission(args[2..].TrimStart(), channel);
        else
            await channel.SendCardSafeAsync(HelpMessage);

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
        await using var dbCtx = _dbProvider.Provide();
        var roles = (from role in dbCtx.UserRoles.Include(e => e.CommandPermissions).AsNoTracking()
            orderby role.KookId
            where role.GuildId == channel.Guild.Id
            from permission in role.CommandPermissions
            select role).ToArray();

        if (roles.IsEmpty())
        {
            await channel.SendInfoCardAsync(
                """
                您还没有对任何指令进行权限限制
                ---
                在不额外设置权限时，所有管理指令（一般以叹号开头）只允许带有管理员权限的用户使用
                除此之外的指令任何人都可以执行
                """, false
            );
            return;
        }

        var permissions = string.Join(
            "\n",
            roles.Select(r =>
                $"{r.GetName(channel.Guild)}：{string.Join("，", r.CommandPermissions.Select(p => p.CommandName))}"
            ));
        await channel.SendInfoCardAsync($"已配置的权限列表：\n{permissions}", false);
    }

    /// <summary>
    ///     List all roles in the current guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    private static Task ListGuildRoles(SocketTextChannel channel)
    {
        var rolenames = string.Join("\n",
            from role in channel.Guild.Roles
            orderby role.Name
            select role.Name
        );
        return channel.SendInfoCardAsync($"当前服务器角色：\n{rolenames}", false);
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
        if (guildRoleId is null)
        {
            await channel.SendErrorCardAsync("角色不存在，您可以发送 `!权限 服务器角色` 列出全部角色名称（或查看当前服务器配置）", true);
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
    ///     Binding command permission to a guild role
    /// </summary>
    /// <param name="rawArgs">Raw text args, split with space</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> BindingPermission(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, dbCtx);

        if (parameters is null) return false;

        var (commandName, guildRoleName, guildRoleId, record) = parameters.Value;

        if (record is not null)
        {
            await channel.SendInfoCardAsync("您已设置过该权限", true);
            return true;
        }

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        #region Transaction

        var role = CreateOrGetRole(dbCtx, guildRoleId, channel.Guild.Id);
        dbCtx.UserCommandPermissions.Add(new UserCommandPermission(role.Id, commandName));
        dbCtx.SaveChanges();

        #endregion

        await transaction.CommitAsync();

        // Update cache
        AppCaches.Permissions.AddOrUpdate($"{channel.Guild.Id}_{commandName}",
            [guildRoleId],
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
    /// <returns>Is command success or not</returns>
    private async Task<bool> UnBindingPermission(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
        var parameters = await ExtractPermissionBindingParameters(rawArgs, channel, dbCtx);

        if (parameters is null) return false;

        var (commandName, guildRoleName, guildRoleId, record) = parameters.Value;

        if (record is null)
        {
            await channel.SendInfoCardAsync("权限已被解绑", false);
        }
        else
        {
            dbCtx.UserCommandPermissions.Remove(record);
            dbCtx.SaveChanges();

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
    /// <param name="dbCtx">Database context</param>
    /// <param name="guildRoleId">Guild role id</param>
    /// <param name="guildId">Guild id to create role if not exist</param>
    /// <returns>User role object</returns>
    private static UserRole CreateOrGetRole(DatabaseContext dbCtx, uint guildRoleId, ulong guildId)
    {
        var record = (from role in dbCtx.UserRoles
            where role.KookId == guildRoleId
            select role).FirstOrDefault();

        if (record is null)
        {
            record = new UserRole(guildRoleId, guildId);
            dbCtx.UserRoles.Add(record);
        }

        dbCtx.SaveChanges();

        return record;
    }

    /// <summary>
    ///     Set default manger account/roles
    /// </summary>
    /// <param name="rawArgs">Raw command args</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> SetDefaultManagerAccountsAndRoles(string rawArgs, SocketTextChannel channel)
    {
        var users = new HashSet<ulong>();
        var roles = new HashSet<ulong>();

        foreach (Match match in Regexs.MatchUserMention().Matches(rawArgs))
        {
            if (!ulong.TryParse(match.Groups["userId"].Value, out var userId))
            {
                await channel.SendErrorCardAsync("您应该使用 @ 来指定管理员用户，被指定的用户在 Kook APP 中显示为蓝色文字", true);
                return false;
            }

            users.Add(userId);
        }

        foreach (Match match in Regexs.MatchRoleMention().Matches(rawArgs))
        {
            if (!ulong.TryParse(match.Groups["roleId"].Value, out var roleId))
            {
                await channel.SendErrorCardAsync("您应该使用 @ 来指定管理员角色，被指定的用户在 Kook APP 中显示为蓝色文字", true);
                return false;
            }

            roles.Add(roleId);
        }

        if (users.IsEmpty() && roles.IsEmpty())
        {
            await channel.SendErrorCardAsync("请在消息中指定（@）用户/角色，可以同时指定多个", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();

        var setting = dbCtx.GuildSettings.First(s => s.GuildId == channel.Guild.Id);
        setting.Setting.DefaultManagerAccounts = users;
        setting.Setting.DefaultManagerRoles = roles;

        dbCtx.SaveChanges();
        AppCaches.GuildSettings[channel.Guild.Id] = setting.Setting;

        await channel.SendSuccessCardAsync("已成功将上述用户/角色设置为默认管理员", false);
        return true;
    }
}
