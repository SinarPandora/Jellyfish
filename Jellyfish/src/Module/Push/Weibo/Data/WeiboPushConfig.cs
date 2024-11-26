namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     Weibo-Push configuration
/// </summary>
public class WeiboPushConfig(string alias, string uid, ulong guildId)
{
    public long Id { get; set; }
    public string Alias { get; set; } = alias;
    public string Uid { get; set; } = uid;
    public ulong GuildId { get; init; } = guildId;

    // References
    public ICollection<WeiboPushInstance> Instances { get; set; } = null!;
}
