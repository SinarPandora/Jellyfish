using Jellyfish.Core.Enum;
using Jellyfish.Module.ExpireExtendSession.Data;
using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Module.TmpChannel.Data;
using Kook;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    private const string CreateTimeProp = nameof(TrackableEntity.CreateTime);
    private const string UpdateTimeProp = nameof(TrackableEntity.UpdateTime);

    public DbSet<TpConfig> TpConfigs { get; set; } = null!;
    public DbSet<TpRoomInstance> TpRoomInstances { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserCommandPermission> UserCommandPermissions { get; set; } = null!;
    public DbSet<TcGroup> TcGroups { get; set; } = null!;
    public DbSet<TcGroupInstance> TcGroupInstances { get; set; } = null!;
    public DbSet<TmpTextChannel> TmpTextChannels { get; set; } = null!;
    public DbSet<ExpireExtendSession> ExpireExtendSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ChannelType>();
        modelBuilder.HasPostgresEnum<TimeUnit>();
        modelBuilder.HasPostgresEnum<ExtendTargetType>();

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

            entity
                .HasOne(e => e.TmpTextChannel)
                .WithMany()
                .HasForeignKey(e => e.TmpTextChannelId);
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

        modelBuilder.Entity<TmpTextChannel>(entity =>
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
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified) continue;
            var actionTimestamp = DateTime.Now;
            if (entry.Metadata.FindProperty(UpdateTimeProp) != null)
            {
                Entry(entry.Entity).Property(UpdateTimeProp).CurrentValue = actionTimestamp;
            }

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty(CreateTimeProp) != null)
            {
                Entry(entry.Entity).Property(CreateTimeProp).CurrentValue = actionTimestamp;
            }
        }

        return base.SaveChanges();
    }
}
