namespace Jellyfish.Core.Job;

/// <summary>
///     Background Async Job Interface
/// </summary>
public interface IAsyncJob
{
    /// <summary>
    ///     Execute async job
    /// </summary>
    Task ExecuteAsync();
}
