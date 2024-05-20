using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace Jellyfish.Module.GroupControl.Model;

/// <summary>
///     Instance definition for creating an instance group
/// </summary>
[UsedImplicitly]
public class InstanceDef
{
    [YamlMember(Alias = "名称", ApplyNamingConventions = false)]
    public string Name { get; set; } = null!;

    [YamlMember(Alias = "允许查看", ApplyNamingConventions = false)]
    public string Allows { get; set; } = null!;

    [YamlMember(Alias = "描述", ApplyNamingConventions = false)]
    public string Description { get; set; } = null!;
}
