namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Metadata of Weibo
/// </summary>
public record WeiboMetadata(string Mid, bool IsTop);

public record WeiboContent(string Username, string Content, string[] Images);
