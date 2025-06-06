using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Module.GroupControl.Model;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;

namespace Jellyfish.Module.GroupControl;

/// <summary>
///     Room group control command
///     Create, modify, or destroy rooms on a group-by-group basis
/// </summary>
public class TcGroupControlCommand : GuildMessageCommand
{
    private readonly ILogger<TcGroupControlCommand> _log;
    private static readonly Lazy<IDeserializer> YamlDeserializer = new(() => new DeserializerBuilder().Build());
    private readonly DbContextProvider _dbProvider;

    public TcGroupControlCommand(ILogger<TcGroupControlCommand> log, DbContextProvider dbProvider) : base(true)
    {
        _log = log;
        _dbProvider = dbProvider;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            创建和管理文字频道组
            ---
            您可以批量创建一组频道，这些频道可以一并显示或隐藏，也可以单独更新信息
            """,
            """
            1. 列表：列出全部的频道组
            2. 配置 [频道组名称] #引导文字频道 [配置文本]：配置频道组（具体内容看下文）
            3. 改名 [频道组名称] [子频道原名] [子频道新名]：更改对应子频道名称
            4. 同步 [频道组名称]：同步所属分组频道权限和子频道频道权限
            5. 解绑 [频道组名称]：解绑频道组下所有子频道，子频道将不再受到以上指令控制说（无法恢复，请谨慎操作）
            6. 解绑 [频道组名称] [子频道名称]：解绑单独子频道
            7. 删除 [频道组名称]：删除频道组和下面全部的子频道（无法恢复，请谨慎操作）
            8. 删除 [频道组名称] [子频道名称]：删除指定子频道
            ---
            **配置指令参数解释**
            1. # 引导文字频道：一个普通的文字频道，生成的全部子频道将参考该频道所在的分组信息。
            引导文字频道应是一个 Kook 引用（输入 # 和频道名称进行引用），在消息中为蓝色文本。
            ---
            2. 配置文本：格式如下（[Yaml 格式](https://yaml.cn/)）：
            ```yaml
            - 名称：频道名称1
              允许查看: 用户1#1234，ABC权限
              描述: |
                备注信息
                支持换行
            - 名称：频道名称1
              允许查看: 用户1#1234，123权限
              描述: |
                备注信息
                支持换行
            ```
            为避免打扰到成员，配置文本中提到的权限和用户均不为 Kook 引用（在消息中为灰色文本）
            其中，`-`, `:` 和 `|` 均为英文标点
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
        else if (args.StartsWith("同步"))
            isSuccess = await SyncGroup(args[2..].TrimStart(), channel);
        else if (args.StartsWith("解绑"))
            isSuccess = await DeleteGroupOrInstance(args[2..].TrimStart(), false, channel);
        else if (args.StartsWith("删除"))
            isSuccess = await DeleteGroupOrInstance(args[2..].TrimStart(), true, channel);
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    /// <summary>
    ///     List all channel groups
    /// </summary>
    /// <param name="channel">Sender channel</param>
    private async Task ListGroups(SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
        var groups =
            from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
            where g.GuildId == channel.Guild.Id
            orderby g.Id
            select $"名称：{g.Name}，频道数量：{g.GroupInstances.Count}";
        if (groups.IsEmpty())
        {
            await channel.SendInfoCardAsync("当前服务器尚未创建过频道组", false);
            return;
        }

        await channel.SendInfoCardAsync(string.Join("\n", groups), false);
    }

    /// <summary>
    ///     Sync group permission and location as the target channel
    ///     (or the category channel specify on created)
    /// </summary>
    /// <param name="groupName">Raw command args text</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> SyncGroup(string groupName, SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
        var tcGroup = (from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
            where g.Name == groupName && g.GuildId == channel.Guild.Id
            select g).FirstOrDefault();

        if (tcGroup is null)
        {
            await channel.SendErrorCardAsync("指定频道组不存在！", true);
            return false;
        }

        await channel.SendInfoCardAsync($"开始同步组 {groupName} 下全部频道", false);
        _log.LogInformation("开始同步组 {GroupName} 下全部频道", groupName);
        foreach (var instance in tcGroup.GroupInstances)
        {
            var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
            if (textChannel is null)
            {
                await channel.SendWarningCardAsync(
                    $"指定文字频道已被删除，请使用 `!频道组 删除 {groupName} {instance.Name}` 指令手动清理已删频道",
                    false
                );
                continue;
            }

            if (textChannel.CategoryId is null)
            {
                await channel.SendInfoCardAsync($"频道 {instance.Name} 不属于任何分组，已跳过权限同步", false);
                continue;
            }

            await textChannel.SyncPermissionsAsync();
            _log.LogInformation("频道 {ChannelName} 权限已同步", textChannel.Name);
        }

        await channel.SendSuccessCardAsync("频道组权限同步完成", false);
        return true;
    }

    /// <summary>
    ///     Config channel group
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> ConfigGroup(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendErrorCardAsync("参数不足，具体内容可以查看：`!频道组 帮助`", true);
            return false;
        }

        // Load origin text channel from the message
        var rawMention = args[1];

        if (!rawMention.StartsWith(KookConstants.ChannelMention))
        {
            await channel.SendErrorCardAsync("请在指令中指定引导文字频道，具体内容请参考：`!频道组 帮助`", true);
            return false;
        }

        if (!MentionUtils.TryParseChannel(rawMention, out var textChannelId, TagMode.KMarkdown))
        {
            await channel.SendErrorCardAsync("引导文字频道应是一个频道引用（蓝色文本），请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return false;
        }

        var originalChannel = channel.Guild.GetTextChannel(textChannelId)!;

        // Load a config object from the message
        List<InstanceDef> defMapping = null!;
        try
        {
            defMapping = YamlDeserializer.Value.Deserialize<List<InstanceDef>>(args[2]);
            await channel.SendSuccessCardAsync("已成功解析配置内容，已计划批量创建", false);
        }
        catch (Exception e)
        {
            _log.LogError(e, "解析批量频道定义失败");
            await channel.SendErrorCardAsync(
                "批量频道定义数据格式不正确，请使用正确的 Yaml 格式，具体内容请参考：`!频道组 帮助`",
                true);
        }

        await channel.SendSuccessCardAsync("开始执行批量创建操作", false);
        await CreateOrUpdateTc(args[0], defMapping, originalChannel, channel);
        await channel.SendSuccessCardAsync("批量频道创建完成！", false);
        return true;
    }

    /// <summary>
    ///     Create or update channel
    /// </summary>
    /// <param name="groupName">Channel group name</param>
    /// <param name="defMapping">Bulk channel config mapping</param>
    /// <param name="originalChannel">Reference channel to locate</param>
    /// <param name="channel">Sender channel</param>
    private async Task CreateOrUpdateTc(string groupName,
        List<InstanceDef> defMapping,
        SocketTextChannel originalChannel, SocketTextChannel channel)
    {
        await using var dbCtx = _dbProvider.Provide();
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(e => e.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == groupName
                select g)
            .FirstOrDefault();

        if (tcGroup is null)
        {
            tcGroup = new TcGroup(groupName, channel.Guild.Id);
            dbCtx.TcGroups.Add(tcGroup);
            dbCtx.SaveChanges();
        }

        var instanceMap = tcGroup.GroupInstances.ToDictionary(instance => instance.Name);

        // Load a config mapping from the message
        _log.LogInformation("开始批量创建频道");
        foreach (var def in defMapping)
        {
            _log.LogInformation("频道 {DefName} 创建开始", def.Name);
            var description = string.IsNullOrWhiteSpace(def.Description) ? null : def.Description.Trim();
            var instance = instanceMap.GetValueOrDefault(def.Name);
            try
            {
                var childChannel = await CloneRoom(instanceMap, originalChannel, def, channel);
                // Update description
                if (instance is null)
                {
                    await RecordAndDescribeNewChannel(childChannel, def.Name, description, tcGroup, instanceMap,
                        dbCtx);
                }
                else
                {
                    instance.TextChannelId = childChannel.Id;

                    // Diff room description
                    await DescribeExistChannel(instance, def.Name, description, childChannel);
                }

                dbCtx.SaveChanges();
                _log.LogInformation("频道 {DefName} 创建完毕", def.Name);
            }
            catch (Exception e)
            {
                _log.LogError(e, "频道 {DefName} 创建失败", def.Name);
                await channel.SendErrorCardAsync($"频道 {def.Name} 创建失败，请稍后重试", false);
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
    private async Task DescribeExistChannel(TcGroupInstance instance, string name, string? description,
        IMessageChannel channel)
    {
        if (instance.Description != description)
        {
            var messageId = instance.DescriptionMessageId;
            if (instance.DescriptionMessageId is not null)
            {
                await channel.DeleteMessageAsync((Guid)instance.DescriptionMessageId);
            }

            if (description is not null)
            {
                var resp = await channel.SendTextSafeAsync(description);
                if (!resp.HasValue)
                {
                    _log.LogError("Bot 无法发送描述消息，因为 Bot 已被屏蔽");
                    await channel.SendErrorCardAsync("Bot 无法发送描述消息，因为 Bot 已被屏蔽", false);
                    return;
                }

                messageId = resp.Value.Id;
            }

            instance.Description = description;
            instance.DescriptionMessageId = messageId;
            _log.LogInformation("频道 {Name} 备注信息更新完毕", name);
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
    private async Task RecordAndDescribeNewChannel(
        IMessageChannel childChannel,
        string name,
        string? description,
        TcGroup tcGroup,
        Dictionary<string, TcGroupInstance> instanceMap,
        DatabaseContext dbCtx)
    {
        _log.LogInformation("检测到频道 {Name} 尚未被记录，正在记录", name);
        Guid? messageId = null;
        if (description is not null)
        {
            var resp = await childChannel.SendTextSafeAsync(description);
            if (!resp.HasValue)
            {
                _log.LogError("Bot 无法发送描述消息，因为 Bot 已被屏蔽");
                await childChannel.SendErrorCardAsync("Bot 无法发送描述消息，因为 Bot 已被屏蔽", false);
                return;
            }

            messageId = resp.Value.Id;
        }

        var instance = messageId is null
            ? new TcGroupInstance(tcGroup.Id, name, childChannel.Id)
            : new TcGroupInstance(tcGroup.Id, name, childChannel.Id, description!, (Guid)messageId);

        dbCtx.TcGroupInstances.Add(instance);
        instanceMap.Add(name, instance);
        _log.LogInformation("频道 {Name} 记录完毕", name);
    }

    /// <summary>
    ///     Clone room
    /// </summary>
    /// <param name="instances">All existing room instance</param>
    /// <param name="originalChannel">Original channel for clone</param>
    /// <param name="definition">New channel definition</param>
    /// <param name="senderChannel">Sender channel</param>
    private static async Task<ITextChannel> CloneRoom(IReadOnlyDictionary<string, TcGroupInstance> instances,
        SocketTextChannel originalChannel, InstanceDef definition, IMessageChannel senderChannel)
    {
        var guild = originalChannel.Guild;
        var instance = instances.GetValueOrDefault(definition.Name);
        ITextChannel newChannel;
        if (instance is null || guild.GetTextChannel(instance.TextChannelId) is null)
        {
            // Create room
            newChannel = await guild.CreateTextChannelAsync(definition.Name,
                p => p.CategoryId = originalChannel.CategoryId);
        }
        else
        {
            // Update room
            newChannel = guild.GetTextChannel(instance.TextChannelId)!;
            await newChannel.ModifyAsync(c => c.CategoryId = originalChannel.CategoryId);
        }

        if (originalChannel.CategoryId is not null)
        {
            await newChannel.SyncPermissionsAsync();
        }

        // Handle additional permission
        if (!definition.Allows.IsNotNullOrWhiteSpace()) return newChannel;

        foreach (var allowName in Regexs.MatchSpaceComma().Split(definition.Allows))
        {
            if (allowName.Contains('#'))
            {
                var found = (
                    from u in originalChannel.Guild.Users
                    where $"{u.DisplayName()}#{u.IdentifyNumber}" == allowName
                    select u
                ).FirstOrDefault();
                if (found is null)
                {
                    await senderChannel.SendWarningCardAsync(
                        $"未找到用户 {allowName}，将不会为其添加频道 {definition.Name} 的权限",
                        false
                    );
                    continue;
                }

                await newChannel.OverrideUserPermissionAsync(found, p => p.Modify(viewChannel: PermValue.Allow));
            }
            else
            {
                var found = (
                    from r in originalChannel.Guild.Roles
                    where r.Name == allowName
                    select r
                ).FirstOrDefault();

                if (found is null)
                {
                    await senderChannel.SendWarningCardAsync(
                        $"未找到权限 {allowName}，将不会将其添加到频道 {definition.Name}",
                        false
                    );
                    continue;
                }

                await newChannel.OverrideRolePermissionAsync(found, p => p.Modify(viewChannel: PermValue.Allow));
            }
        }

        return newChannel;
    }

    /// <summary>
    ///     Update channel name
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="channel">Sender channel</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> UpdateChannelName(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendErrorCardAsync("请提供频道组名称，频道原名和修改后名称", true);
            return false;
        }

        var configName = args[0];
        var oldName = args[1];
        var newName = args[1];

        await using var dbCtx = _dbProvider.Provide();
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(g => g.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == configName
                select g).FirstOrDefault();

        if (tcGroup is null)
        {
            await channel.SendErrorCardAsync("指定频道组不存在！", true);
            return false;
        }

        var instance = (from ins in tcGroup.GroupInstances
            where ins.Name == oldName
            select ins).FirstOrDefault();

        if (instance is null)
        {
            await channel.SendErrorCardAsync("指定频道不存在！", true);
            return false;
        }

        if (oldName != newName)
        {
            var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
            if (textChannel is null)
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
    ///     Delete group or instance
    /// </summary>
    /// <param name="rawArgs">Raw command args text</param>
    /// <param name="hardDel">Is delete or deregister</param>
    /// <param name="channel">Sender text channel</param>
    /// <returns>Is command success of not</returns>
    private async Task<bool> DeleteGroupOrInstance(string rawArgs, bool hardDel, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);
        if (args.Length < 1)
        {
            await channel.SendErrorCardAsync("请提供频道组名称，频道原名和修改后名称", true);
            return false;
        }

        var configName = args[0];
        await using var dbCtx = _dbProvider.Provide();
        var tcGroup =
            (from g in dbCtx.TcGroups.Include(g => g.GroupInstances)
                where g.GuildId == channel.Guild.Id && g.Name == configName
                select g).FirstOrDefault();

        if (tcGroup is null)
        {
            await channel.SendErrorCardAsync("指定频道组不存在！", true);
            return false;
        }

        if (args.Length == 2)
        {
            // Delete specify channel
            var channelName = args[1];
            var instance = (from ins in tcGroup.GroupInstances
                where ins.Name == channelName
                select ins).FirstOrDefault();

            if (instance is null)
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
    private async Task DeleteWholeGroup(
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
                if (textChannel is null)
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
                        _log.LogError(e, "房间 {InstanceName} 删除失败", instance.Name);
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
    private static Task DeleteChildChannel(bool hardDel, TcGroupInstance instance,
        SocketTextChannel channel, DatabaseContext dbCtx)
    {
        if (hardDel)
        {
            var textChannel = channel.Guild.GetTextChannel(instance.TextChannelId);
            return textChannel is null ? channel.SendWarningCardAsync("指定文字频道早已被删除", true) : textChannel.DeleteAsync();
        }

        dbCtx.TcGroupInstances.Remove(instance);

        return Task.CompletedTask;
    }
}
