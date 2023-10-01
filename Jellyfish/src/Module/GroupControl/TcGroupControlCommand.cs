using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;
using YamlDotNet.Serialization;

namespace Jellyfish.Module.GroupControl;

/// <summary>
///     Room group control command
///     Create, modify, or destroy rooms on a group-by-group basis
/// </summary>
public class TcGroupControlCommand : GuildMessageCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<IDeserializer> YamlDeserializer = new(() => new DeserializerBuilder().Build());

    public TcGroupControlCommand()
    {
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            文字频道组指令

            您可以批量创建一组频道，这些频道可以一并显示或隐藏，也可以单独更新信息
            """,
            """
            1. 列表：列出全部的频道组
            2. 配置 [配置名称] #引导文字频道 [配置文本]：配置频道组（具体内容看下文）
            3. 改名 [配置名称] [子频道原名] [子频道新名]：更改对应子频道名称
            4. 显示 [配置名称]：显示全部子频道
            5. 隐藏 [配置名称]：隐藏全部子频道
            6. 同步 [配置名称]：同步所属分类频道权限和子频道频道权限
            7. 解绑 [配置名称]：解绑频道组配置下所有子频道，子频道将不再受到以上指令控制说（无法恢复，请谨慎操作）
            8. 解绑 [配置名称] [子频道名称]：解绑单独子频道
            9. 删除 [配置名称]：删除频道组配置和下面全部的子频道（无法恢复，请谨慎操作）
            10. 删除 [配置名称] [子频道名称]：删除指定子频道
            ---
            **配置指令参数解释**
            1. # 引导文字频道：一个普通的文字频道，生成的全部子频道将参考该频道所在的分组信息。
            引导文字频道应是一个 Kook 引用（输入 # 和频道名称进行引用），在消息中为蓝色文本。

            2. 配置文本：格式如下（[Yaml 格式](https://yaml.cn/)）：
            ```yaml
            频道名称1: ｜
                备注信息
                支持换行
            频道名称2: ｜
                备注信息
                支持换行
            ```
            其中，`:` 和 `|` 均为英文标点
            """);
    }

    public override string Name() => "文字频道组指令";

    public override IEnumerable<string> Keywords() => new[] { "!频道组", "！频道组" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        var isSuccess = true;
        if (args.StartsWith("列表"))
            await ListGroups(channel);
        else if (args.StartsWith("配置"))
            isSuccess = await ConfigGroup(args[2..].TrimStart(), channel);
        else if (args.StartsWith("改名"))
            isSuccess = await UpdateChannelName(args[2..].TrimStart(), channel);
        else if (args.StartsWith("显示"))
            isSuccess = await UpdateGroupVisible(args[2..].TrimStart(), false, channel);
        else if (args.StartsWith("隐藏"))
            isSuccess = await UpdateGroupVisible(args[2..].TrimStart(), true, channel);
        else if (args.StartsWith("解绑"))
            isSuccess = await DeleteGroupOrInstance(args[2..].TrimStart(), false, channel);
        else if (args.StartsWith("删除"))
            isSuccess = await DeleteGroupOrInstance(args[2..].TrimStart(), true, channel);
        else
            await channel.SendCardAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    /// <summary>
    ///     List all channel groups
    /// </summary>
    /// <param name="channel">Sender channel</param>
    private static async Task ListGroups(SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();
        var groups =
            from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
            where g.GuildId == channel.Guild.Id
            orderby g.Id
            let prefix = g.Hidden ? "隐藏" : "显示"
            select $"【{prefix}】名称：{g.Name}，频道数量：{g.GroupInstances.Count}";
        if (groups.IsEmpty())
        {
            await channel.SendInfoCardAsync("当前服务器尚未创建过频道组", false);
            return;
        }

        await channel.SendInfoCardAsync(string.Join("\n", groups), false);
    }

    /// <summary>
    ///     Config channel group
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private static async Task<bool> ConfigGroup(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendErrorCardAsync("参数不足，具体内容可以查看：`!频道组 帮助`", true);
            return false;
        }

        // Load origin text channel from message
        var rawMention = args[1];

        if (!rawMention.StartsWith("(chn)"))
        {
            await channel.SendErrorCardAsync("请在指令中指定引导文字频道，具体内容可以参考：`!频道组 帮助`", true);
            return false;
        }

        var chnMatcher = Regexs.MatchTextChannelMention().Match(rawMention);
        if (!ulong.TryParse(chnMatcher.Groups["channelId"].Value, out var textChannelId))
        {
            await channel.SendErrorCardAsync("引导文字频道应是一个频道引用（蓝色文本），具体内容可以参考：`!频道组 帮助`", true);
            return false;
        }

        var originalChannel = channel.Guild.GetTextChannel(textChannelId);

        // Load config object from message
        await using var dbCtx = new DatabaseContext();
        Dictionary<string, string> configMapping = null!;
        try
        {
            configMapping = YamlDeserializer.Value.Deserialize<Dictionary<string, string>>(args[2]);
        }
        catch (Exception e)
        {
            Log.Error(e, "解析批量频道定义失败");
            await channel.SendErrorCardAsync(
                "批量频道定义数据格式不正确，请使用正确的 Yaml 格式，具体内容可以参考：`!频道组 帮助`",
                true);
        }

        await CreateOrUpdateTc(args[0], configMapping, originalChannel, channel, dbCtx);

        dbCtx.SaveChanges();
        await channel.SendSuccessCardAsync("批量频道创建完成！", false);
        return true;
    }

    /// <summary>
    ///     Create or update channel
    /// </summary>
    /// <param name="groupName">Channel group name</param>
    /// <param name="configMapping">Bulk channel config mapping</param>
    /// <param name="originalChannel">Reference channel to locate</param>
    /// <param name="channel">Sender channel</param>
    /// <param name="dbCtx">Database context</param>
    private static async Task CreateOrUpdateTc(string groupName,
        Dictionary<string, string> configMapping,
        SocketTextChannel originalChannel, SocketTextChannel channel,
        DatabaseContext dbCtx)
    {
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == groupName
                select g)
            .FirstOrDefault();

        if (tcGroup == null)
        {
            tcGroup = new TcGroup(groupName, channel.Guild.Id);
            dbCtx.TcGroups.Add(tcGroup);
            dbCtx.SaveChanges();
        }

        var instanceMap = tcGroup.GroupInstances.ToDictionary(instance => instance.Name);

        // Load config mapping from message
        Log.Info("开始批量创建频道");
        foreach (var (name, rawDescription) in configMapping)
        {
            Log.Info($"频道 {name} 创建开始");
            var description = string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();
            var instance = instanceMap.GetValueOrDefault(name);
            try
            {
                var childChannel = await CloneRoom(instanceMap, originalChannel, name);
                await childChannel.ModifyPermissionOverwriteAsync(channel.Guild.EveryoneRole, p =>
                    p.Modify(
                        viewChannel: tcGroup.Hidden ? PermValue.Deny : PermValue.Allow)
                );
                // Update description
                if (instance == null)
                {
                    await RecordAndDescribeNewChannel(childChannel, name, description, tcGroup, instanceMap, dbCtx);
                }
                else
                {
                    instance.TextChannelId = childChannel.Id;

                    // Diff room description
                    await DescribeExistChannel(instance, name, description, childChannel);
                }

                dbCtx.SaveChanges();
                Log.Info($"频道 {name} 创建完毕");
            }
            catch (Exception e)
            {
                Log.Error(e, $"频道 {name} 创建失败");
                await channel.SendErrorCardAsync($"频道 {name} 创建失败，请稍后重试", false);
            }
        }
    }

    /// <summary>
    ///     Describe existing channel
    /// </summary>
    /// <param name="instance">Channel instance</param>
    /// <param name="name">New channel name</param>
    /// <param name="description">Channel description</param>
    /// <param name="channel">The text channel</param>
    private static async Task DescribeExistChannel(TcGroupInstance instance, string name, string? description,
        IMessageChannel channel)
    {
        if (instance.Description != description)
        {
            var messageId = instance.DescriptionMessageId;
            if (instance.DescriptionMessageId != null)
            {
                await channel.DeleteMessageAsync((Guid)instance.DescriptionMessageId);
            }

            if (description != null)
            {
                var resp = await channel.SendTextAsync(description);
                messageId = resp.Id;
            }

            instance.Description = description;
            instance.DescriptionMessageId = messageId;
            Log.Info($"频道 {name} 备注信息更新完毕");
        }
    }

    /// <summary>
    ///     Record and describe new channel
    /// </summary>
    /// <param name="childChannel">Child channel instance</param>
    /// <param name="name">Channel name</param>
    /// <param name="description">Channel description</param>
    /// <param name="tcGroup">Text channel group</param>
    /// <param name="instanceMap">All existing instance</param>
    /// <param name="dbCtx">Database context</param>
    private static async Task RecordAndDescribeNewChannel(
        IMessageChannel childChannel,
        string name,
        string? description,
        TcGroup tcGroup,
        IDictionary<string, TcGroupInstance> instanceMap,
        DatabaseContext dbCtx)
    {
        Log.Info($"检测到频道 {name} 尚未被记录，正在记录");
        Guid? messageId = null;
        if (description != null)
        {
            var result = await childChannel.SendTextAsync(description);
            messageId = result.Id;
        }

        var instance = messageId == null
            ? new TcGroupInstance(tcGroup.Id, name, childChannel.Id)
            : new TcGroupInstance(tcGroup.Id, name, childChannel.Id, description!, (Guid)messageId);

        dbCtx.TcGroupInstances.Add(instance);
        instanceMap.Add(name, instance);
        Log.Info($"频道 {name} 记录完毕");
    }

    /// <summary>
    ///     Clone room
    /// </summary>
    /// <param name="instances">All existing room instance</param>
    /// <param name="originalChannel">Original channel for clone</param>
    /// <param name="name">New channel name</param>
    private static async Task<ITextChannel> CloneRoom(IReadOnlyDictionary<string, TcGroupInstance> instances,
        SocketTextChannel originalChannel, string name)
    {
        var guild = originalChannel.Guild;
        var instance = instances.GetValueOrDefault(name);
        if (instance == null || guild.GetTextChannel(instance.TextChannelId) == null)
        {
            // Create room
            var channel = await guild.CreateTextChannelAsync(name, p => p.CategoryId = originalChannel.CategoryId);
            return channel;
        }
        else
        {
            // Update room
            var channel = guild.GetTextChannel(instance.TextChannelId);
            await channel.ModifyAsync(c => c.CategoryId = originalChannel.CategoryId);
            return channel;
        }
    }

    /// <summary>
    ///     Update channel name
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private static async Task<bool> UpdateChannelName(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendErrorCardAsync("请提供频道配置名称，频道原名和修改后名称", true);
            return false;
        }

        var configName = args[0];
        var oldName = args[1];
        var newName = args[1];

        await using var dbCtx = new DatabaseContext();
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(g => g.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == configName
                select g).FirstOrDefault();

        if (tcGroup == null)
        {
            await channel.SendErrorCardAsync("指定配置不存在！", true);
            return false;
        }

        var instance = (from ins in tcGroup.GroupInstances
            where ins.Name == oldName
            select ins).FirstOrDefault();

        if (instance == null)
        {
            await channel.SendErrorCardAsync("指定频道不存在！", true);
            return false;
        }

        if (oldName != newName)
        {
            var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
            if (textChannel == null)
            {
                await channel.SendErrorCardAsync(
                    $"指定文字频道已被删除，请使用 `!频道组 删除 {configName} {oldName}` 指令手动清理已删频道",
                    true
                );
                return false;
            }

            await textChannel.ModifyAsync(c => c.Name = newName);
            await channel.SendSuccessCardAsync("频道名称修改完成！", false);

            return true;
        }

        await channel.SendSuccessCardAsync("频道名称已是最新，无需修改！", false);
        return true;
    }

    /// <summary>
    ///     Update group visible status
    /// </summary>
    /// <param name="groupName">Group name to be set</param>
    /// <param name="hidden">Is hidden or not</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private static async Task<bool> UpdateGroupVisible(string groupName, bool hidden, SocketTextChannel channel)
    {
        await using var dbCtx = new DatabaseContext();
        var tcGroup = (from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
            where g.Name == groupName
            select g).FirstOrDefault();

        if (tcGroup == null)
        {
            await channel.SendErrorCardAsync("指定配置不存在！", true);
            return false;
        }

        var status = hidden ? "隐藏" : "显示";
        foreach (var instance in tcGroup.GroupInstances)
        {
            try
            {
                var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
                if (textChannel == null)
                {
                    await channel.SendErrorCardAsync(
                        $"指定文字频道已被删除，请使用 `!频道组 删除 {groupName} {instance.Name}` 指令手动清理已删频道",
                        false
                    );
                    continue;
                }

                await textChannel.ModifyPermissionOverwriteAsync(channel.Guild.EveryoneRole, p =>
                    p.Modify(viewChannel: hidden ? PermValue.Deny : PermValue.Allow)
                );
            }
            catch (Exception e)
            {
                Log.Error(e, $"调整频道 {instance.Name} 权限时出错！");
                await channel.SendErrorCardAsync($"调整频道 {instance.Name} 状态为 {status} 时出错，请稍后重试", false);
            }
        }

        tcGroup.Hidden = hidden;
        dbCtx.SaveChanges();
        await channel.SendSuccessCardAsync($"{groupName} 下的全部频道已 {status}", false);
        return true;
    }

    /// <summary>
    ///     Delete group or instance
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="hardDel">Is delete or deregister</param>
    /// <param name="channel">Sender text channel</param>
    /// <returns>Is command success of not</returns>
    private static async Task<bool> DeleteGroupOrInstance(string rawArgs, bool hardDel, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);
        if (args.Length < 1)
        {
            await channel.SendErrorCardAsync("请提供频道配置名称，频道原名和修改后名称", true);
            return false;
        }

        var configName = args[0];
        await using var dbCtx = new DatabaseContext();
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(g => g.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == configName
                select g).FirstOrDefault();

        if (tcGroup == null)
        {
            await channel.SendErrorCardAsync("指定配置不存在！", true);
            return false;
        }

        if (args.Length == 2)
        {
            // Delete specify channel
            var channelName = args[1];
            var instance = (from ins in tcGroup.GroupInstances
                where ins.Name == channelName
                select ins).FirstOrDefault();

            if (instance == null)
            {
                await channel.SendErrorCardAsync("指定频道不存在！", true);
                return false;
            }

            await DeleteChildChannel(hardDel, instance, channel, dbCtx);
        }
        else
        {
            // Delete whole config and channels
            await DeleteWholeGroup(hardDel, tcGroup, channel, dbCtx);
        }

        dbCtx.SaveChanges();
        await channel.SendSuccessCardAsync("操作已成功完成", false);
        return true;
    }

    /// <summary>
    ///     Delete(or deregister) whole channel group
    /// </summary>
    /// <param name="hardDel">Is delete or deregister</param>
    /// <param name="tcGroup">Group object</param>
    /// <param name="channel">Sender channel</param>
    /// <param name="dbCtx">Database context</param>
    private static async Task DeleteWholeGroup(
        bool hardDel,
        TcGroup tcGroup,
        SocketTextChannel channel,
        DatabaseContext dbCtx)
    {
        foreach (var instance in tcGroup.GroupInstances)
        {
            if (hardDel)
            {
                var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
                if (textChannel == null)
                {
                    await channel.SendWarningCardAsync($"频道 {instance.Name} 早已被删除", true);
                }
                else
                {
                    try
                    {
                        await textChannel.DeleteAsync();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"房间 {instance.Name} 删除失败");
                        await channel.SendWarningCardAsync(
                            $"房间 {instance.Name} 删除失败，请稍后尝试手动删除",
                            false
                        );
                    }
                }
            }

            dbCtx.TcGroupInstances.Remove(instance);
        }

        if (hardDel)
        {
            dbCtx.TcGroups.Remove(tcGroup);
        }
    }

    /// <summary>
    ///     Delete(or deregister) child text channel
    /// </summary>
    /// <param name="hardDel">Is delete or deregister</param>
    /// <param name="instance">Target channel instance</param>
    /// <param name="channel">Sender channel to send message</param>
    /// <param name="dbCtx">Database context</param>
    private static async Task DeleteChildChannel(bool hardDel, TcGroupInstance instance,
        SocketTextChannel channel, DatabaseContext dbCtx)
    {
        if (hardDel)
        {
            var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
            if (textChannel == null)
            {
                await channel.SendWarningCardAsync("指定文字频道早已被删除", true);
            }
            else
            {
                await textChannel.DeleteAsync();
            }
        }
        else
        {
            dbCtx.TcGroupInstances.Remove(instance);
        }
    }
}
