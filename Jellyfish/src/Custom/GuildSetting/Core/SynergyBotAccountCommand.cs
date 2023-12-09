using System.Text.RegularExpressions;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Custom.GuildSetting.Core;

/// <summary>
///     Command for setting synergy bot account
/// </summary>
public class SynergyBotAccountCommand : GuildMessageCommand
{
    private readonly DbContextProvider _dbProvider;

    public SynergyBotAccountCommand(DbContextProvider dbProvider) : base(true)
    {
        _dbProvider = dbProvider;
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            配置协同Bot账号
            被添加的协同Bot账号将自动被添加到水母Bot创建的私有/临时频道中（除了频道组指令）
            ⚠️警告：请不要将非Bot（即 Kook APP 内名字旁边没有「机器人」标识的用户）配置为协同Bot，这将导致诸如组队功能等部分功能出错。
            """,
            """
            1. 列表：列出已添加的Bot账号
            2. 添加 [#Bot用户引用]（支持多个）：添加Bot账号作为协同Bot
            3. 删除 [#Bot用户引用]（支持多个）：删除协同Bot账号
            ---
            #Bot用户引用：指的是在聊天框中输入 @ 并选择的Bot，在 Kook 中显示为蓝色文字。直接输入用户名是无效的。
            """);
    }

    public override string Name() => "服务器协同机器人账号配置指令";

    public override IEnumerable<string> Keywords() => ["！协同机器人", "!协同机器人"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
        {
            await channel.SendCardAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("列表"))
            await ListAccounts(channel);
        else if (args.StartsWith("添加"))
            isSuccess = await AddOrRemoveAccounts(args[2..].Trim(), channel, true);
        else if (args.StartsWith("删除"))
            isSuccess = await AddOrRemoveAccounts(args[2..].Trim(), channel, false);
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    /// <summary>
    ///     List all added synergy bot accounts
    /// </summary>
    /// <param name="channel">Current channel</param>
    private static Task ListAccounts(SocketTextChannel channel)
    {
        var accounts = AppCaches.GuildSettings[channel.Guild.Id].SynergyBotAccounts
            .Select(userId => $"- {MentionUtils.KMarkdownMentionUser(userId)}")
            .ToArray();

        return accounts.IsEmpty()
            ? channel.SendInfoCardAsync($"{channel.Guild.Name} 未添加任何协同Bot账号", false)
            : channel.SendSuccessCardAsync($"已添加以下协同Bot：\n{accounts.StringJoin("\n")}", false);
    }

    /// <summary>
    ///     Add or remove synergy bot accounts
    /// </summary>
    /// <param name="rawArgs">Raw args</param>
    /// <param name="channel">Current channel</param>
    /// <param name="isAdd">Is action add or delete</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> AddOrRemoveAccounts(string rawArgs, SocketTextChannel channel, bool isAdd)
    {
        var bots = new HashSet<ulong>();

        foreach (Match match in Regexs.MatchUserMention().Matches(rawArgs))
        {
            if (!ulong.TryParse(match.Groups["userId"].Value, out var botId))
            {
                await channel.SendErrorCardAsync("您应该使用 @ 来指定Bot账号，被指定的账号在 Kook APP 中显示为蓝色文字", true);
                return false;
            }

            bots.Add(botId);
        }

        if (bots.IsEmpty())
        {
            await channel.SendErrorCardAsync("请在消息中指定（@）Bot账号，可以同时指定多个", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();

        var setting = dbCtx.GuildSettings.First(s => s.GuildId == channel.Guild.Id);

        if (isAdd)
        {
            setting.Setting.SynergyBotAccounts.UnionWith(bots);
            await channel.SendSuccessCardAsync("指定账号已添加为协同Bot", false);
        }
        else
        {
            setting.Setting.SynergyBotAccounts.ExceptWith(bots);
            await channel.SendSuccessCardAsync("指定账号已不再作为协同Bot", false);
        }

        dbCtx.SaveChanges();
        AppCaches.GuildSettings[channel.Guild.Id] = setting.Setting;

        return true;
    }
}
