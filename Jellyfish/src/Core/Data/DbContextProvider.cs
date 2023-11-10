using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Data;

/// <summary>
///     Database context provider
/// </summary>
public class DbContextProvider
{
    private readonly DbContextOptions<DatabaseContext> _options;

    public DbContextProvider(DbContextOptions<DatabaseContext> options)
    {
        _options = options;
    }

    /// <summary>
    ///     Provide new DbCtx object
    /// </summary>
    /// <returns>Database context object</returns>
    public DatabaseContext Provide() => new(_options);
}
