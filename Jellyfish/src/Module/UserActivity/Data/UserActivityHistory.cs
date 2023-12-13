using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Module.UserActivity.Enum;

namespace Jellyfish.Module.UserActivity.Data;

/// <summary>
///     User activity update history
/// </summary>
public class UserActivityHistory(ulong userId, ulong guildId, string reason, ActivityScoreAction action, decimal delta)
{
    public Guid Id { get; set; }
    public ulong UserId { get; set; } = userId;
    public ulong GuildId { get; set; } = guildId;
    public string Reason { get; set; } = reason;
    public ActivityScoreAction Action { get; set; } = action;
    public decimal Delta { get; set; } = delta;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; set; } = DateTime.Now;
}
