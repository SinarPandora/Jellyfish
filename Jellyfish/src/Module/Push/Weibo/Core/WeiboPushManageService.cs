using Jellyfish.Core.Data;
using Jellyfish.Module.Push.Weibo.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Service for managing Weibo push binding
/// </summary>
public class WeiboPushManageService(DbContextProvider dbProvider, ILogger<WeiboPushManageService> log)
{
    /// <summary>
    ///     Create a push config and
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawArgs">User input</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> CreateOrAppendPushConfig(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);

        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync(
                """
                参数不足！举例：`!微博推送 添加 1234567890 #引用现有文字频道`
                 引用的频道必须是一个 Kook 引用，请在消息框中输入#（井号）并在弹出的菜单中选择指定频道
                """,
                true);
            return false;
        }

        var uidOrAlias = args[0];
        var rawMention = args[1];

        var isUid = false;
        if (long.TryParse(uidOrAlias, out var uid))
        {
            uidOrAlias = uid.ToString();
            isUid = true;
        }

        if (!rawMention.StartsWith(KookConstants.ChannelMention))
        {
            await channel.SendErrorCardAsync("请在指令中引用现有文字频道，具体内容请参考：`!微博推送 帮助`", true);
            return false;
        }

        if (!MentionUtils.TryParseChannel(rawMention, out var channelId, TagMode.KMarkdown))
        {
            await channel.SendErrorCardAsync("现有文字频道引用应是一个频道引用（蓝色文本），请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return false;
        }

        await using var dbCtx = dbProvider.Provide();
        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        #region Transaction

        var config = dbCtx.WeiboPushConfigs
            .Include(c => c.Instances)
            .FirstOrDefault(c => c.Uid == uidOrAlias || c.Alias == uidOrAlias);

        if (config is null)
        {
            if (!isUid)
            {
                await channel.SendErrorCardAsync("初次绑定请使用 UID", true);
                return false;
            }

            config = new WeiboPushConfig(uidOrAlias, uidOrAlias, channel.Guild.Id);
            dbCtx.WeiboPushConfigs.Add(config);
            dbCtx.SaveChanges();
        }
        else if (config.Instances.Any(i => i.ChannelId == channelId))
        {
            await channel.SendErrorCardAsync("此微博用户推送已绑定过该频道", true);
            return false;
        }

        dbCtx.WeiboPushInstances.Add(new WeiboPushInstance(config.Id, channelId));
        dbCtx.SaveChanges();

        #endregion

        await transaction.CommitAsync();

        log.LogInformation("创建微博用户{UID}到频道{ChannelId}的推送绑定", uidOrAlias, channelId);
        await channel.SendSuccessCardAsync(
            $"绑定成功！{config.Alias}的新微博将推送到{MentionUtils.KMarkdownMentionChannel(channelId)}",
            false);
        return true;
    }

    /// <summary>
    ///     Rename config
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawArgs">User input</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> RenameConfig(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);

        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！举例：`!微博推送 别名 1234567890 水母`", true);
            return false;
        }

        var rawUid = args[0];
        var alias = args[1];

        if (!long.TryParse(rawUid, out var uid))
        {
            await channel.SendWarningCardAsync("第一个参数应为 UID（长数字），具体内容请参考：`!微博推送 帮助`", true);
            return false;
        }

        await using var dbCtx = dbProvider.Provide();
        var config = (
            from c in dbCtx.WeiboPushConfigs
            where c.Uid == uid.ToString()
            select c
        ).FirstOrDefault();

        if (config is null)
        {
            await channel.SendWarningCardAsync("指定推送配置不存在！", true);
            return false;
        }

        var duplicateConfig = dbCtx.WeiboPushConfigs.FirstOrDefault(c => c.Alias == alias);
        if (duplicateConfig is not null)
        {
            await channel.SendWarningCardAsync($"指定别名已分配给用户：{duplicateConfig.Uid}！", true);
            return false;
        }

        config.Alias = alias;
        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync($"UID：{uid} 别名已更新为 {alias}", false);
        return true;
    }

    /// <summary>
    ///     Remove push instance in the config
    ///     which means the bot will never push to the channel it bindings again.
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawArgs">User input</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> RemovePushInstance(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);

        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync(
                """
                参数不足！举例：`!微博推送 删除 1234567890 [#引用现有文字频道]`
                 引用的频道必须是一个 Kook 引用，请在消息框中输入#（井号）并在弹出的菜单中选择指定频道
                """,
                true);
            return false;
        }

        var uidOrAlias = args[0];
        var rawMention = args[1];

        if (!MentionUtils.TryParseChannel(rawMention, out var channelId, TagMode.KMarkdown))
        {
            await channel.SendErrorCardAsync("现有文字频道引用应是一个频道引用（蓝色文本），请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return false;
        }

        await using var dbCtx = dbProvider.Provide();
        var config = dbCtx.WeiboPushConfigs
            .Include(c => c.Instances)
            .FirstOrDefault(c => c.Alias == uidOrAlias || c.Uid == uidOrAlias);

        if (config is null)
        {
            await channel.SendWarningCardAsync("指定推送配置不存在！", false);
            return false;
        }

        var instance = config.Instances.FirstOrDefault(i => i.ChannelId == channelId);
        if (instance is null)
        {
            await channel.SendWarningCardAsync("该用户动态推送并没有绑定到该频道！", false);
            return false;
        }

        dbCtx.WeiboPushInstances.Remove(instance);
        dbCtx.SaveChanges();

        if (dbCtx.WeiboPushInstances.All(c => c.ConfigId != instance.ConfigId))
        {
            dbCtx.WeiboPushConfigs.Remove(config);
            await channel.SendSuccessCardAsync($"删除成功！当前用户{uidOrAlias}不再关联任何频道，配置已清除", false);
        }
        else
        {
            await channel.SendSuccessCardAsync("删除成功！", false);
        }

        return true;
    }

    /// <summary>
    ///     List all push configs with instances
    /// </summary>
    /// <param name="channel">Current channel</param>
    public async Task ListPushConfig(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();

        var configs = (
            from config in dbCtx.WeiboPushConfigs.Include(c => c.Instances)
                .AsNoTracking()
            select $"{config.Alias}（{config.Uid}）推送到频道：" + (
                from instance in config.Instances
                select $"{MentionUtils.KMarkdownMentionChannel(instance.ChannelId)}"
            ).ToArray().StringJoin(" | ")
        ).ToArray();

        if (configs.IsEmpty())
            await channel.SendInfoCardAsync("本服务器还没有设置任何微博推送", false);
        else
            await channel.SendInfoCardAsync("已开启如下微博推送：\n" + configs.StringJoin("\n"), false);
    }
}
