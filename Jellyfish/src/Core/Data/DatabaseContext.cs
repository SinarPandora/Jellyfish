using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.TeamPlay.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Data;

public class DatabaseContext : DbContext
{
    private const string CreateTimeProp = nameof(TrackableEntity.CreateTime);
    private const string UpdateTimeProp = nameof(TrackableEntity.UpdateTime);

    public DbSet<TpConfig> TpConfigs { get; set; } = null!;
    public DbSet<TpRoomInstance> TpRoomInstances { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserCommandPermission> UserCommandPermissions { get; set; } = null!;
    public DbSet<TcGroup> TcGroups { get; set; } = null!;
    public DbSet<TcGroupInstance> TcGroupInstances { get; set; } = null!;

    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TpConfig>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.DefaultMemberLimit)
                .HasDefaultValue(0);

            entity
                .HasMany(e => e.RoomInstances)
                .WithOne(e => e.TpConfig)
                .HasForeignKey(e => e.TpConfigId)
                .IsRequired();
        });

        modelBuilder.Entity<TpRoomInstance>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity
                .HasMany(e => e.CommandPermissions)
                .WithOne(e => e.UserRole)
                .HasForeignKey(e => e.UserRoleId)
                .IsRequired();
        });

        modelBuilder.Entity<TcGroup>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .HasMany(e => e.GroupInstances)
                .WithOne(e => e.Group)
                .HasForeignKey(e => e.TcGroupId)
                .IsRequired();
        });

        modelBuilder.Entity<TcGroupInstance>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");
        });
    }

    public override int SaveChanges()
    {
        var entities = ChangeTracker.Entries().ToList();

        foreach (var entry in entities)
            if (entry.State == EntityState.Added)
            {
                if (entry.Metadata.FindProperty(CreateTimeProp) != null)
                {
                    Entry(entry.Entity).Property(CreateTimeProp).CurrentValue = DateTime.Now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Metadata.FindProperty(UpdateTimeProp) != null)
                {
                    Entry(entry.Entity).Property(UpdateTimeProp).CurrentValue = DateTime.Now;
                }
            }

        return base.SaveChanges();
    }
}
