using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Core.Data;

public abstract class TrackableEntity
{
    [Column(TypeName = "timestamp")]
    public DateTime CreateTime { get; init; } = DateTime.Now;

    [Column(TypeName = "timestamp")]
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}
