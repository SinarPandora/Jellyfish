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
    ///     Parse user input and create the CountDown-Name channel
    /// </summary>
    /// <param name="rawArgs">Raw user input</param>
    /// <param name="channel">Current text channel</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> ParseAndCreate(string rawArgs, SocketTextChannel channel)
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
            await channel.SendErrorCardAsync("请在指令中引用现有频道，具体内容请参考：`!倒计时频道 帮助`", true);
            return false;
        }

        var chnMatcher = Regexs.MatchTextChannelMention().Match(rawMention);
        if (!ulong.TryParse(chnMatcher.Groups["channelId"].Value, out var targetChannelId))
        {
            await channel.SendErrorCardAsync("现有频道引用应是一个蓝色文本，具体内容请参考：`!倒计时频道 帮助`", true);
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
        var cdChannel = new CountDownChannel(channel.Guild.Id, targetChannelId, pattern, dueDate, delta <= 0);
        dbCtx.CountDownChannels.Add(cdChannel);

        dbCtx.SaveChanges();

        await UpdateChannelText(channel.Guild.GetChannel(targetChannelId)!, cdChannel);
        await channel.SendSuccessCardAsync(
            $"创建成功！{MentionUtils.KMarkdownMentionChannel(channel.Id)} 的频道名称已设置为{(delta <= 0 ? "正计时" : "倒计时")}，距离天数：{Math.Abs(delta)}",
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
    public Task<bool> PersistDueText(string rawArgs, SocketTextChannel channel)
    {
        throw new NotImplementedException();
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
            if (cdChannel.DueText != null)
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
