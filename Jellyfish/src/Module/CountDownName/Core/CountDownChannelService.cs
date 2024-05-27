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

    /// <summary>
    ///     Parse user input and create the CountDown-Name channel
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current text channel</param>
    /// <param name="user">Current user</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> ParseAndCreate(string rawArgs, SocketTextChannel channel, SocketUser user)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 2)
        {
            await channel.SendErrorCardAsync($"参数不足！举例：`！倒计时频道 #频道 2024-05-01 距离劳动节还有{{COUNT}}天`", true);
            return false;
        }

        var rawMention = args[0];

        if (!rawMention.StartsWith("(chn)"))
        {
            await channel.SendErrorCardAsync("请在指令中引用现有文字频道，具体内容请参考：`!倒计时频道 帮助`", true);
            return false;
        }

        var chnMatcher = Regexs.MatchTextChannelMention().Match(rawMention);
        if (!ulong.TryParse(chnMatcher.Groups["channelId"].Value, out var textChannelId))
        {
            await channel.SendErrorCardAsync("现有文字频道引用应是一个频道引用（蓝色文本），具体内容请参考：`!倒计时频道 帮助`", true);
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
        var cdChannel = new CountDownChannel(channel.Guild.Id, channel.Id, pattern, dueDate, delta <= 0);
        dbCtx.CountDownChannels.Add(cdChannel);

        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync(
            $"创建成功！{MentionUtils.KMarkdownMentionChannel(channel.Id)} 的频道名称已设置为{(delta <= 0 ? "正计时" : "倒计时")}，距离天数：{Math.Abs(delta)}",
            false
        );
        return true;
    }
}
