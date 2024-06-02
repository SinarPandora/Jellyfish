using Jellyfish.Core.Data;
using Jellyfish.Module.CountDownName.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.CountDownName.Core;

/// <summary>
///     Logic for CountDown-Name channel
/// </summary>
public class CountDownChannelService(DbContextProvider dbProvider)
{
    private const string CountPlaceHolder = "{COUNT}";

    private static readonly Dictionary<char, string> EmojiNumberMapping = new()
    {
        { '0', "0️⃣" },
        { '1', "1️⃣" },
        { '2', "2️⃣" },
        { '3', "3️⃣" },
        { '4', "4️⃣" },
        { '5', "5️⃣" },
        { '6', "6️⃣" },
        { '7', "7️⃣" },
        { '8', "8️⃣" },
        { '9', "9️⃣" }
    };

    /// <summary>
    ///     List all CountDown-Name channels in this Guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> List(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();

        var today = DateTime.Today;
        var cdChannels = (from cdChannel in dbCtx.CountDownChannels
            where cdChannel.GuildId == channel.Guild.Id
            select $"{cdChannel.Id}：{
                MentionUtils.KMarkdownMentionChannel(cdChannel.ChannelId)
            }，目标日期：{
                cdChannel.DueDate:yyyy-MM-dd
            }{
                (cdChannel.Positive ? "（正计时" : "（倒计时")
            }{
                Math.Abs((cdChannel.DueDate.ToDateTime(TimeOnly.MinValue) - today).Days)
            }天），到期名称：{cdChannel.DueText ?? "未设置"}").ToArray();

        if (cdChannels.IsEmpty())
        {
            await channel.SendInfoCardAsync("当前服务器没有设置倒计时频道", false);
            return true;
        }

        await channel.SendInfoCardAsync(cdChannels.StringJoin("\n"), false);
        return true;
    }

    /// <summary>
    ///     Parse user input and create the CountDown-Name channel
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current text channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> ParseAndCreate(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendErrorCardAsync($"参数不足！举例：`！倒计时频道 #频道 2024-05-01 距离劳动节还有{CountPlaceHolder}天`", true);
            return false;
        }

        var rawMention = args[0];

        if (!rawMention.StartsWith(KookConstants.ChannelMention))
        {
            await channel.SendErrorCardAsync("请在指令中引用现有频道，具体内容请参考：`!倒计时频道 帮助`", true);
            return false;
        }

        if (!MentionUtils.TryParseChannel(rawMention, out var targetChannelId, TagMode.KMarkdown))
        {
            await channel.SendErrorCardAsync("频道引用应是一个蓝色文本，请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return false;
        }

        var pattern = args[2];
        if (!pattern.Contains(CountPlaceHolder))
        {
            await channel.SendErrorCardAsync($"频道名模板应包含{CountPlaceHolder}，以显示倒计时，具体内容请参考：`!倒计时频道 帮助`", true);
            return false;
        }

        var rawDueDate = args[1];
        if (!DateOnly.TryParse(rawDueDate, out var dueDate))
        {
            await channel.SendErrorCardAsync("日期格式错误，举例：`1997-7-29`", true);
            return false;
        }

        var delta = (dueDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

        await using var dbCtx = dbProvider.Provide();

        var exists = dbCtx.CountDownChannels.FirstOrDefault(item =>
            item.ChannelId == targetChannelId && item.GuildId == channel.Guild.Id);
        if (exists is not null)
        {
            await channel.SendErrorCardAsync("该频道已设置过倒计时，请选择其他频道（或删除倒计时重新创建）", true);
            return false;
        }

        var cdChannel = new CountDownChannel(channel.Guild.Id, targetChannelId, pattern, dueDate, delta <= 0);
        dbCtx.CountDownChannels.Add(cdChannel);

        dbCtx.SaveChanges();

        await UpdateChannelText(channel.Guild.GetChannel(targetChannelId)!, cdChannel);
        await channel.SendSuccessCardAsync(
            $"""
             创建成功！{MentionUtils.KMarkdownMentionChannel(channel.Id)} 的频道名称已设置为{(delta <= 0 ? "正计时" : "倒计时")}，距离天数：{Math.Abs(delta)}
             ---
             您可以进一步设置到达指定日期时显示的频道标题，如：
             `!倒计时频道 到期名称 {cdChannel.Id} XXX就是今天！`
             """,
            false
        );
        return true;
    }

    /// <summary>
    ///     Persist the due text to the CountDown-Name channel
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> PersistDueText(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync("参数不足！请参考帮助信息：`！倒计时频道 帮助`", true);
            return false;
        }

        var rawMention = args[0];
        await using var dbCtx = dbProvider.Provide();
        var cdChannel = await ExtractMentionOrId(rawMention, channel, dbCtx);
        if (cdChannel is null) return false;

        cdChannel.DueText = args[1];
        dbCtx.SaveChanges();

        await UpdateChannelText(channel.Guild.GetChannel(cdChannel.ChannelId)!, cdChannel);
        await channel.SendSuccessCardAsync(
            $"创建成功！在到达指定日期时，频道 {MentionUtils.KMarkdownMentionChannel(channel.Id)} 名称将显示为：{args[1]}",
            false
        );
        return true;
    }

    /// <summary>
    ///     Extract the channel mention or id from the user input as CountDown-Name channel
    /// </summary>
    /// <param name="rawMention">User input</param>
    /// <param name="channel">Current channel</param>
    /// <param name="dbCtx">Database context</param>
    /// <returns>CountDown-Name channel if extracted</returns>
    private static async Task<CountDownChannel?> ExtractMentionOrId(string rawMention, SocketTextChannel channel,
        DatabaseContext dbCtx)
    {
        if (rawMention.StartsWith(KookConstants.ChannelMention))
        {
            if (MentionUtils.TryParseChannel(rawMention, out var targetChannelId, TagMode.KMarkdown))
            {
                return dbCtx.CountDownChannels.FirstOrDefault(item =>
                    item.ChannelId == targetChannelId && item.GuildId == channel.Guild.Id);
            }

            await channel.SendErrorCardAsync("频道引用应是一个蓝色文本，请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return null;
        }

        if (long.TryParse(rawMention, out var cdChannelId))
        {
            return dbCtx.CountDownChannels.FirstOrDefault(item => item.Id == cdChannelId);
        }

        await channel.SendErrorCardAsync("请艾特对应频道或输入倒计时编号，具体内容请参考：`!倒计时频道 帮助`", true);
        return null;
    }

    /// <summary>
    ///     Remove the due text from the CountDown-Name channel
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> RemoveDueText(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var cdChannel = await ExtractMentionOrId(rawArgs, channel, dbCtx);
        if (cdChannel is null) return false;

        cdChannel.DueText = null;
        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync("到期频道名称已移除", false);
        return true;
    }

    /// <summary>
    ///     Remove the CountDown-Name channel setting
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> Delete(string rawArgs, SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var cdChannel = await ExtractMentionOrId(rawArgs, channel, dbCtx);
        if (cdChannel is null) return false;

        dbCtx.CountDownChannels.Remove(cdChannel);
        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync("指定频道倒计时已被删除", false);
        return true;
    }

    /// <summary>
    ///     Update the channel name based on the pattern
    /// </summary>
    /// <param name="target">Target channel</param>
    /// <param name="cdChannel">CountDown-Name channel object as a pattern</param>
    public static async Task UpdateChannelText(IGuildChannel target, CountDownChannel cdChannel)
    {
        var delta = Math.Abs((cdChannel.DueDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days);
        string name;
        if (delta == 0)
        {
            if (cdChannel.DueText is not null)
            {
                name = cdChannel.DueText;
            }
            else return;
        }
        else
        {
            name = cdChannel.Pattern.Replace(CountPlaceHolder,
                delta
                    .ToString()
                    .ToCharArray()
                    .Select(c => EmojiNumberMapping[c])
                    .StringJoin(string.Empty)
            );
        }

        await target.ModifyAsync(c => c.Name = name);
    }
}
