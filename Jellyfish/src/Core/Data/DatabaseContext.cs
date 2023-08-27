using System.Configuration;
using Jellyfish.Core.Kook.Protocol;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.TeamPlay.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Data;

public class DatabaseContext : DbContext
{
    public DbSet<TpConfig> TpConfigs { get; set; } = null!;
    public DbSet<TpRoomInstance> TpRoomInstances { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserCommandPermission> UserCommandPermissions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(ConfigurationManager.ConnectionStrings["DbConnectionStr"].ConnectionString)
            .UseSnakeCaseNamingConvention();
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
                .Property(e => e.Enabled)
                .HasDefaultValue(true);

            entity
                .Property(e => e.VoiceQuality)
                .HasDefaultValue(VoiceQuality.Medium);

            entity
                .HasMany(e => e.RoomInstances)
                .WithOne(e => e.TpConfig)
                .HasForeignKey(e => e.TpConfigId)
                .IsRequired();
        });

        modelBuilder.Entity<TpRoomInstance>(entity =>
        {
            entity
                .Property(e => e.MemberLimit)
                .HasDefaultValue(10);

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
                .Property(e => e.Enabled)
                .HasDefaultValue(true);

            entity
                .HasMany(e => e.CommandPermissions)
                .WithOne(e => e.UserRole)
                .HasForeignKey(e => e.UserRoleId)
                .IsRequired();
        });
    }

    public override int SaveChanges()
    {
        var entities = ChangeTracker.Entries().ToList();

        foreach (var entry in entities)
            switch (entry.State)
            {
                case EntityState.Added:
                    Entry(entry.Entity).Property(nameof(TrackableEntity.CreateTime)).CurrentValue = DateTime.Now;
                    break;
                case EntityState.Modified:
                    Entry(entry.Entity).Property(nameof(TrackableEntity.UpdateTime)).CurrentValue = DateTime.Now;
                    break;
            }

        return base.SaveChanges();
    }
}
