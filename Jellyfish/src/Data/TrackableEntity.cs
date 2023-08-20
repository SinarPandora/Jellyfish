using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Data;

public abstract class TrackableEntity
{
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; set; }
    [Column(TypeName = "timestamp")] public DateTime UpdateTime { get; set; }
}
