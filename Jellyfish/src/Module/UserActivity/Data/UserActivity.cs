using Jellyfish.Core.Data;

namespace Jellyfish.Module.UserActivity.Data;

/// <summary>
///     User activity after statistics
/// </summary>
public class UserActivity(ulong userId, ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public ulong UserId { get; set; } = userId;
    public ulong GuildId { get; set; } = guildId;
    public uint TotalClockInDay { get; set; }
    public decimal Score { get; set; }
}
