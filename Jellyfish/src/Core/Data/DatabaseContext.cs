using Jellyfish.Core.Enum;
using Jellyfish.Module.Board.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Module.CountDownName.Data;
using Jellyfish.Module.ExpireExtendSession.Data;
using Jellyfish.Module.GroupControl.Data;
using Jellyfish.Module.GuildSetting.Data;
using Jellyfish.Module.GuildSetting.Enum;
using Jellyfish.Module.Role.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Module.TmpChannel.Data;
using Kook;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Jellyfish.Core.Data;

/// <summary>
///     EFCore database context
/// </summary>
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    private const string CreateTimeProp = nameof(TrackableEntity.CreateTime);
    private const string UpdateTimeProp = nameof(TrackableEntity.UpdateTime);
    private const string GuildSettingDetailsProp = nameof(GuildSetting.Setting);

    // ----------------------------------- Team Play -----------------------------------
    public DbSet<TpConfig> TpConfigs { get; set; } = null!;

    public DbSet<TpRoomInstance> TpRoomInstances { get; set; } = null!;

    // ----------------------------------- Permission -----------------------------------
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    public DbSet<UserCommandPermission> UserCommandPermissions { get; set; } = null!;

    // ----------------------------- Temporary Text Channel -----------------------------
    public DbSet<TcGroup> TcGroups { get; set; } = null!;
    public DbSet<TcGroupInstance> TcGroupInstances { get; set; } = null!;
    public DbSet<TmpTextChannel> TmpTextChannels { get; set; } = null!;

    public DbSet<ExpireExtendSession> ExpireExtendSessions { get; set; } = null!;

    // --------------------------------- Guild Settings ---------------------------------
    public DbSet<GuildSetting> GuildSettings { get; set; } = null!;

    // -------------------------------- Countdown Channel -------------------------------
    public DbSet<CountDownChannel> CountDownChannels { get; set; } = null!;

    // ----------------------------------- Kook Board -----------------------------------
    public DbSet<BoardConfig> BoardConfigs { get; set; } = null!;
    public DbSet<BoardItem> BoardItems { get; set; } = null!;
    public DbSet<BoardInstance> BoardInstances { get; set; } = null!;
    public DbSet<BoardPermission> BoardPermissions { get; set; } = null!;

    public DbSet<BoardItemHistory> BoardItemHistories { get; set; } = null!;

    // ------------------------------------ Clock In ------------------------------------
    public DbSet<ClockInConfig> ClockInConfigs { get; set; } = null!;
    public DbSet<ClockInCardInstance> ClockInCardInstances { get; set; } = null!;
    public DbSet<ClockInStage> ClockInStages { get; set; } = null!;
    public DbSet<ClockInHistory> ClockInHistories { get; set; } = null!;
    public DbSet<ClockInStageQualifiedHistory> ClockInStageQualifiedHistories { get; set; } = null!;
    public DbSet<UserClockInStatus> UserClockInStatuses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ChannelType>();
        modelBuilder.HasPostgresEnum<TimeUnit>();
        modelBuilder.HasPostgresEnum<ExtendTargetType>();
        modelBuilder.HasPostgresEnum<GuildCustomFeature>();
        modelBuilder.HasPostgresEnum<BoardType>();

        modelBuilder.Entity<TpConfig>(entity =>
        {
            HasTrackableColumns(entity);

            entity
                .Property(e => e.DefaultMemberLimit)
                .HasDefaultValue(0);

            entity
                .HasMany(e => e.RoomInstances)
                .WithOne(e => e.TpConfig)
                .HasForeignKey(e => e.TpConfigId)
                .IsRequired();

            entity
                .Property(e => e.Enabled)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<TpRoomInstance>(entity =>
        {
            HasTrackableColumns(entity);

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

            entity
                .Property(e => e.Enabled)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<TcGroup>(entity =>
        {
            HasTrackableColumns(entity);

            entity
                .HasMany(e => e.GroupInstances)
                .WithOne(e => e.Group)
                .HasForeignKey(e => e.TcGroupId)
                .IsRequired();
        });

        modelBuilder.Entity<TcGroupInstance>(HasTrackableColumns);

        modelBuilder.Entity<TmpTextChannel>(HasTrackableColumns);

        modelBuilder.Entity<GuildSetting>(entity =>
        {
            // Store GuildSettingDetails as JSON
            entity
                .Property(e => e.Setting)
                .HasColumnType("jsonb")
                // Use Newtonsoft json to custom json serialize because it supports Hashset
                .HasConversion(r => JsonConvert.SerializeObject(r),
                    json => JsonConvert.DeserializeObject<GuildSettingDetails>(json)!);

            HasTrackableColumns(entity);
        });

        modelBuilder.Entity<CountDownChannel>(entity =>
        {
            entity
                .HasIndex(c => new { c.GuildId, c.ChannelId })
                .IsUnique();

            HasTrackableColumns(entity);
        });

        modelBuilder.Entity<BoardConfig>(entity =>
        {
            entity
                .HasMany(e => e.Items)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .HasMany(e => e.Instances)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .HasMany(e => e.Permissions)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .Property(e => e.Finished)
                .HasDefaultValue(false);

            HasTrackableColumns(entity);
        });

        modelBuilder.Entity<BoardInstance>(HasTrackableColumns);

        modelBuilder.Entity<BoardItem>(entity =>
        {
            entity
                .HasMany(e => e.Histories)
                .WithOne(e => e.Item)
                .HasForeignKey(e => e.ItemId)
                .IsRequired();

            HasTrackableColumns(entity);
        });

        modelBuilder.Entity<BoardItemHistory>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");
        });

        modelBuilder.Entity<ClockInConfig>(entity =>
        {
            entity
                .HasMany(e => e.CardInstances)
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
                .HasMany(e => e.UserStatuses)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired();

            entity
                .HasIndex(e => e.GuildId)
                .IsUnique();

            entity
                .Property(e => e.ButtonText)
                .HasDefaultValue("打卡！");

            entity
                .Property(e => e.Title)
                .HasDefaultValue("每日打卡");

            entity
                .Property(e => e.Enabled)
                .HasDefaultValue(true);

            entity
                .Property(e => e.TodayClockInCount)
                .HasDefaultValue(0);

            entity
                .Property(e => e.AllClockInCount)
                .HasDefaultValue(0);

            HasTrackableColumns(entity);
        });

        modelBuilder.Entity<ClockInStage>(entity =>
        {
            entity
                .HasMany(e => e.QualifiedHistories)
                .WithOne(e => e.Stage)
                .HasForeignKey(e => e.StageId)
                .IsRequired();

            entity
                .Property(e => e.AllowBreakDays)
                .HasDefaultValue(0);

            entity
                .Property(e => e.Enabled)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<UserClockInStatus>(entity =>
        {
            HasTrackableColumns(entity);

            entity
                .HasMany(e => e.QualifiedHistories)
                .WithOne(e => e.UserStatus)
                .HasForeignKey(e => e.UserStatusId)
                .IsRequired();

            entity
                .HasIndex(e => e.UserId);

            entity
                .HasIndex(e => new { e.ConfigId, e.UserId })
                .IsUnique();

            entity
                .Property(e => e.AllClockInCount)
                .HasDefaultValue(0);

            entity
                .Property(e => e.IsClockInToday)
                .HasDefaultValue(false);

            entity
                .Property(e => e.StartDate)
                .HasDefaultValueSql("current_date");
        });

        modelBuilder.Entity<ClockInStageQualifiedHistory>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");
        });

        modelBuilder.Entity<ClockInHistory>(entity =>
        {
            entity
                .Property(e => e.CreateTime)
                .HasDefaultValueSql("current_timestamp");

            entity
                .HasIndex(e => e.CreateTime)
                .IsDescending();

            entity
                .HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<ClockInCardInstance>(entity =>
        {
            entity
                .HasIndex(e => new { e.ConfigId, e.ChannelId })
                .IsUnique();
        });
    }

    public override int SaveChanges()
    {
        var entities = ChangeTracker.Entries().ToList();

        foreach (var entry in entities)
        {
            // Mark custom json field GuildSetting.Setting always modified,
            // to solve nested objects that cannot be detected for change
            if (entry.Metadata.ClrType == typeof(GuildSetting))
            {
                Entry(entry.Entity).Property(GuildSettingDetailsProp).IsModified = true;
            }

            if (entry.State != EntityState.Added && entry.State != EntityState.Modified) continue;
            var actionTimestamp = DateTime.Now;
            if (entry.Metadata.FindProperty(UpdateTimeProp) is not null)
            {
                Entry(entry.Entity).Property(UpdateTimeProp).CurrentValue = actionTimestamp;
            }

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty(CreateTimeProp) is not null)
            {
                Entry(entry.Entity).Property(CreateTimeProp).CurrentValue = actionTimestamp;
            }
        }

        return base.SaveChanges();
    }

    private static void HasTrackableColumns<T>(EntityTypeBuilder<T> entity) where T : TrackableEntity
    {
        entity
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("current_timestamp");

        entity
            .Property(e => e.UpdateTime)
            .HasDefaultValueSql("current_timestamp");
    }
}
