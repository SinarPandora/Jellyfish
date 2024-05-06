using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Data;

/// <summary>
///     Database context provider
/// </summary>
public class DbContextProvider(DbContextOptions<DatabaseContext> options)
{
    /// <summary>
    ///     Provide new DbCtx object
    /// </summary>
    /// <returns>Database context object</returns>
    public DatabaseContext Provide() => new(options);
}
