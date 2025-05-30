using System.Text.RegularExpressions;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.GuildSetting.Core;

/// <summary>
///     Command for setting synergy bot accounts
/// </summary>
public class SynergyBotAccountCommand : GuildMessageCommand
{
    private readonly DbContextProvider _dbProvider;
    private readonly KookSocketClient _kook;

    public SynergyBotAccountCommand(DbContextProvider dbProvider, KookSocketClient kook) : base(true)
    {
        _dbProvider = dbProvider;
        _kook = kook;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            配置协同机器人账号
            被添加的机器人账号将自动被添加到水母机器人创建的私有/临时频道中（除了频道组指令）
            ⚠️警告：请不要将非机器人（即 Kook APP 内名字旁边没有「机器人」标识的用户）配置为协同机器人，这将导致诸如组队功能等部分功能出错。
            """,
            """
            基础功能：
            1. 列表：列出已添加的机器人账号
            2. 添加 [#机器人用户引用]（支持多个）：添加机器人账号作为协同机器人
            3. 删除 [#机器人用户引用]（支持多个）：删除协同机器人账号
            ---
            冲突消息：
            > 水母的指令前缀可能与其他机器人冲突，导致其他机器人发送如：找不到指令，指令错误 这类提示
            > 若无法与其他机器人协商关闭提示信息，您可以在这里添加冲突消息（作为模板）
            > 如果其他机器人发送了包含这些内容的消息，水母将自动删除

            1. 冲突消息列表：列出全部冲突消息模板
            2. 添加冲突消息 [消息内容]：添加冲突消息模板
            3. 删除冲突消息 [消息内容]：删除冲突消息模板
            ---
            #机器人用户引用：指的是在聊天框中输入 @ 并选择的机器人，在 Kook 中显示为蓝色文字。直接输入用户名是无效的。
            """);
    }

    public override string Name() => "协同机器人账号配置指令";

    public override IEnumerable<string> Keywords() => ["!协同机器人", "！协同机器人"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("列表"))
            await ListAccounts(channel);
        else if (args.StartsWith("添加"))
            isSuccess = await AddOrRemoveAccounts(args[2..].Trim(), channel, true);
        else if (args.StartsWith("删除"))
            isSuccess = await AddOrRemoveAccounts(args[2..].Trim(), channel, false);
        else if (args.StartsWith("冲突消息列表"))
            await ListConflictMessageTemplates(channel);
        else if (args.StartsWith("添加冲突消息"))
            isSuccess = await AddOrRemoveConflictMessageTemplate(args[6..].Trim(), channel, true);
        else if (args.StartsWith("删除冲突消息"))
            isSuccess = await AddOrRemoveConflictMessageTemplate(args[6..].Trim(), channel, false);
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
            .Select((userId, index) => $"{index + 1}：{MentionUtils.KMarkdownMentionUser(userId)}")
            .ToArray();

        return accounts.IsEmpty()
            ? channel.SendInfoCardAsync($"{channel.Guild.Name} 未添加任何协同机器人账号", false)
            : channel.SendSuccessCardAsync($"已添加以下协同机器人：\n{accounts.StringJoin("\n")}", false);
    }

    /// <summary>
    ///     Add or remove synergy bot accounts
    /// </summary>
    /// <param name="rawArgs">Raw args</param>
    /// <param name="channel">Current channel</param>
    /// <param name="isAdd">Is action adding or deleting?</param>
    /// <returns>Is command success or not?</returns>
    private async Task<bool> AddOrRemoveAccounts(string rawArgs, SocketTextChannel channel, bool isAdd)
    {
        var bots = new HashSet<ulong>();

        foreach (Match match in Regexs.MatchUserMention().Matches(rawArgs))
        {
            if (!ulong.TryParse(match.Groups["userId"].Value, out var botId))
            {
                await channel.SendErrorCardAsync("您应该使用 @ 来指定机器人账号，被指定的账号在 Kook APP 中显示为蓝色文字", true);
                return false;
            }

            if (!(channel.Guild.GetUser(botId)?.IsBot ?? false))
            {
                await channel.SendErrorCardAsync("您指定的账号并非机器人账号", true);
                return false;
            }

            if (_kook.CurrentUser!.Id == botId)
            {
                await channel.SendErrorCardAsync("水母自己不能成为自己的协同机器人", true);
                return false;
            }

            bots.Add(botId);
        }

        if (bots.IsEmpty())
        {
            await channel.SendErrorCardAsync("请在消息中指定（@）机器人账号，可以同时指定多个", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();

        var setting = dbCtx.GuildSettings.First(s => s.GuildId == channel.Guild.Id);

        if (isAdd)
        {
            setting.Setting.SynergyBotAccounts.UnionWith(bots);
            await channel.SendSuccessCardAsync("指定账号已添加为协同机器人", false);
        }
        else
        {
            setting.Setting.SynergyBotAccounts.ExceptWith(bots);
            await channel.SendSuccessCardAsync("指定账号已不再作为协同机器人", false);
        }

        dbCtx.SaveChanges();
        AppCaches.GuildSettings[channel.Guild.Id] = setting.Setting;

        return true;
    }

    /// <summary>
    ///     List all added conflict message templates
    /// </summary>
    /// <param name="channel">Current channel</param>
    private static Task ListConflictMessageTemplates(SocketTextChannel channel)
    {
        var templates = AppCaches.GuildSettings[channel.Guild.Id].SynergyBotConflictMessage
            .Select((message, index) => $"{index + 1}：{message}")
            .ToArray();

        return templates.IsEmpty()
            ? channel.SendInfoCardAsync($"{channel.Guild.Name} 未添加任何冲突消息（模板）", false)
            : channel.SendSuccessCardAsync($"已添加以下冲突消息（模板）：\n{templates.StringJoin("\n")}", false);
    }


    /// <summary>
    ///     Add or remove conflict message template
    /// </summary>
    /// <param name="template">Conflict message</param>
    /// <param name="channel">Current channel</param>
    /// <param name="isAdd">Is action adding or deleting?</param>
    /// <returns>Is command success or not?</returns>
    private async Task<bool> AddOrRemoveConflictMessageTemplate(string template, SocketTextChannel channel, bool isAdd)
    {
        if (template.IsEmpty())
        {
            await channel.SendErrorCardAsync("请在消息中指定冲突消息内容", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();

        var setting = dbCtx.GuildSettings.First(s => s.GuildId == channel.Guild.Id);

        if (isAdd)
        {
            setting.Setting.SynergyBotConflictMessage.Add(template);
            await channel.SendSuccessCardAsync("冲突消息模板已添加", false);
        }
        else
        {
            setting.Setting.SynergyBotConflictMessage.Remove(template);
            await channel.SendSuccessCardAsync("冲突消息模板已删除", false);
        }

        dbCtx.SaveChanges();
        AppCaches.GuildSettings[channel.Guild.Id] = setting.Setting;

        return true;
    }
}
