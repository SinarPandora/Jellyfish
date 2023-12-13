using Jellyfish.Core.Enum;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Module.ExpireExtendSession.Data;
using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Module.GuildSetting.Data;
using Jellyfish.Module.GuildSetting.Enum;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Module.TmpChannel.Data;
using Jellyfish.Module.UserActivity.Data;
using Jellyfish.Module.UserActivity.Enum;
using Kook;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Jellyfish.Core.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    private const string CreateTimeProp = nameof(TrackableEntity.CreateTime);
    private const string UpdateTimeProp = nameof(TrackableEntity.UpdateTime);
    private const string GuildSettingDetailsProp = nameof(GuildSetting.Setting);

    // ---------------------------------------------- Team Play --------------------------------------------------------
    public DbSet<TpConfig> TpConfigs { get; set; } = null!;
    public DbSet<TpRoomInstance> TpRoomInstances { get; set; } = null!;

    // ---------------------------------------------- Permission -------------------------------------------------------
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserCommandPermission> UserCommandPermissions { get; set; } = null!;

    // ---------------------------------------------- Channel Group ----------------------------------------------------
    public DbSet<TcGroup> TcGroups { get; set; } = null!;
    public DbSet<TcGroupInstance> TcGroupInstances { get; set; } = null!;

    // ---------------------------------------------- Temp Text Channel ------------------------------------------------
    public DbSet<TmpTextChannel> TmpTextChannels { get; set; } = null!;
    public DbSet<ExpireExtendSession> ExpireExtendSessions { get; set; } = null!;

    // ---------------------------------------------- Guild Setting ----------------------------------------------------
    public DbSet<GuildSetting> GuildSettings { get; set; } = null!;

    // ---------------------------------------------- Clock-in ---------------------------------------------------------
    public DbSet<ClockInConfig> ClockInConfigs { get; set; } = null!;
    public DbSet<ClockInChannel> ClockInChannels { get; set; } = null!;
    public DbSet<ClockInStage> ClockInStages { get; set; } = null!;
    public DbSet<ClockInHistory> ClockInHistories { get; set; } = null!;
    public DbSet<ClockInQualifiedUser> ClockInQualifiedUsers { get; set; } = null!;

    // ---------------------------------------------- User Activity ----------------------------------------------------
    public DbSet<UserActivity> UserActivities { get; set; } = null!;
    public DbSet<UserActivityHistory> UserActivityHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ChannelType>();
        modelBuilder.HasPostgresEnum<TimeUnit>();
        modelBuilder.HasPostgresEnum<ExtendTargetType>();
        modelBuilder.HasPostgresEnum<GuildCustomFeature>();
        modelBuilder.HasPostgresEnum<ActivityScoreAction>();

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

        modelBuilder.Entity<GuildSetting>(entity =>
        {
            // Store GuildSettingDetails as JSON
            entity
                .Property(e => e.Setting)
                .HasColumnType("jsonb")
                // Use Newtonsoft json to custom json serialize because it support Hashset
                .HasConversion(r => JsonConvert.SerializeObject(r),
                    json => JsonConvert.DeserializeObject<GuildSettingDetails>(json)!);

            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");
        });

        modelBuilder.Entity<ClockInConfig>(entity =>
        {
            entity
                .HasMany(e => e.Channels)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .HasMany(e => e.Stages)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .HasMany(e => e.Histories)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .Property(e => e.UpdateTime)
                .HasDefaultValueSql("current_timestamp");
        });

        modelBuilder.Entity<ClockInStage>(entity =>
        {
            entity
                .HasMany(e => e.QualifiedUsers)
                .WithOne(e => e.Stage)
                .HasForeignKey(e => e.StageId)
                .IsRequired();
        });

        modelBuilder.Entity<ClockInQualifiedUser>(entity =>
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

        foreach (var entry in entities.Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            // Mark custom json field GuildSetting.Setting always modified,
            // to solve nested objects that cannot be detected for change
            if (entry.Metadata.ClrType == typeof(GuildSetting))
            {
                Entry(entry.Entity).Property(GuildSettingDetailsProp).IsModified = true;
            }

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
