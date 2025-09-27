using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.GuildSetting.Enum;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.GuildSetting.Core;

/// <summary>
///     Command for setting custom feature
/// </summary>
public class GuildCustomFeatureCommand : GuildMessageCommand
{
    private readonly DbContextProvider _dbProvider;

    public GuildCustomFeatureCommand(DbContextProvider dbProvider) : base(true)
    {
        _dbProvider = dbProvider;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            为当前服务器开启/关闭附加功能。
            附加功能指的是针对「特殊游戏」或「场景」专门开发的功能，目前支持下列功能：
            ---
            1. 斯普拉遁游戏：斯普拉遁游戏相关功能，包括但不限于：私房规则选择器，私房积分板等
            2. 斯普拉遁3Bot联动：与 @Cypas_Nya 编写的「Bot-Splatoon3」机器人进行联动和辅助，简化操作指令
            （启用该功能需确保您的服务器内有「Bot-Splatoon3」的机器人实例，并使用 `!协同机器人 @机器人实例` 指令注册机器人实例）
            ---
            （更多功能开发中……）
            """,
            """
            1. 已启用：列出全部启用功能
            2. 启用 [功能名称]：启用指定功能
            3. 禁用 [功能名称]：禁用指定功能
            """
        );
    }

    public override string Name() => "服务器附加功能配置指令";

    public override IEnumerable<string> Keywords() => ["!附加功能", "！附加功能"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("已启用"))
            await ListEnabledFeatures(channel);
        else if (args.StartsWith("启用"))
            isSuccess = await SwitchFeature(args[2..].Trim(), channel, true);
        else if (args.StartsWith("禁用"))
            isSuccess = await SwitchFeature(args[2..].Trim(), channel, false);
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    /// <summary>
    ///     List enabled features
    /// </summary>
    /// <param name="channel">Current guild channel</param>
    private static Task<Cacheable<IUserMessage, Guid>?> ListEnabledFeatures(SocketTextChannel channel)
    {
        var setting = AppCaches.GuildSettings[channel.Guild.Id];
        if (setting.EnabledFeatures.IsEmpty())
        {
            return channel.SendInfoCardAsync($"{channel.Guild.Name} 还没有启用任何附加功能", false);
        }

        var featureList = setting.EnabledFeatures
            .Select(f => $"- {f.ToName()}").StringJoin("\n");
        return channel.SendSuccessCardAsync("当前启用的功能如下\n" + featureList, false);
    }

    /// <summary>
    ///     Switch on/off custom feature
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <param name="channel">Current channel</param>
    /// <param name="enable">Is enable action or not</param>
    /// <returns>Is command success or not</returns>
    private async Task<bool> SwitchFeature(string name, SocketTextChannel channel, bool enable)
    {
        var feature = GuildCustomFeatureHelper.FromName(name);

        if (!feature.HasValue)
        {
            await channel.SendErrorCardAsync("该功能不存在！请使用：`！附加功能 帮助` 指令查看现有附加功能", true);
            return false;
        }

        await using var dbCtx = _dbProvider.Provide();

        var setting = dbCtx.GuildSettings.Include(guildSetting => guildSetting.Setting)
            .First(s => s.GuildId == channel.Guild.Id);

        if (enable)
        {
            if (!setting.Setting.EnabledFeatures.Add(feature.Value))
            {
                await channel.SendInfoCardAsync($"{channel.Guild.Name} 已启用过该功能", true);
                return false;
            }

            await channel.SendSuccessCardAsync($"已启用功能：{feature.Value.ToName()}", false);
        }
        else
        {
            if (!setting.Setting.EnabledFeatures.Remove(feature.Value))
            {
                await channel.SendInfoCardAsync("该功能尚未启用", true);
                return false;
            }

            await channel.SendSuccessCardAsync($"已禁用功能：{feature.Value.ToName()}", false);
        }

        dbCtx.SaveChanges();
        AppCaches.GuildSettings[channel.Guild.Id] = setting.Setting;

        return true;
    }
}
