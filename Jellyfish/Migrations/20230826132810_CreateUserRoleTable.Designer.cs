﻿// <auto-generated />
using System;
using Jellyfish.Loader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20230826132810_CreateUserRoleTable")]
    partial class CreateUserRoleTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Jellyfish.Command.Role.Data.UserCommandPermission", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("CommandName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("command_name");

                    b.Property<long>("UserRoleId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_role_id");

                    b.HasKey("Id")
                        .HasName("pk_user_command_permissions");

                    b.HasIndex("UserRoleId")
                        .HasDatabaseName("ix_user_command_permissions_user_role_id");

                    b.ToTable("user_command_permissions", (string)null);
                });

            modelBuilder.Entity("Jellyfish.Command.Role.Data.UserRole", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool?>("Enabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("enabled");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("KookId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("kook_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_user_roles");

                    b.ToTable("user_roles", (string)null);
                });

            modelBuilder.Entity("Jellyfish.Command.TeamPlay.Data.TpConfig", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp")
                        .HasColumnName("create_time")
                        .HasDefaultValueSql("current_timestamp");

                    b.Property<bool?>("Enabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("enabled");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<decimal?>("TextChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("text_channel_id");

                    b.Property<DateTime>("UpdateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp")
                        .HasColumnName("update_time")
                        .HasDefaultValueSql("current_timestamp");

                    b.Property<decimal>("VoiceChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("voice_channel_id");

                    b.Property<int>("VoiceQuality")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(2)
                        .HasColumnName("voice_quality");

                    b.HasKey("Id")
                        .HasName("pk_tp_configs");

                    b.ToTable("tp_configs", (string)null);
                });

            modelBuilder.Entity("Jellyfish.Command.TeamPlay.Data.TpRoomInstance", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp")
                        .HasColumnName("create_time")
                        .HasDefaultValueSql("current_timestamp");

                    b.Property<decimal>("CreatorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("creator_id");

                    b.Property<long>("MemberLimit")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValue(10L)
                        .HasColumnName("member_limit");

                    b.Property<long>("TpConfigId")
                        .HasColumnType("bigint")
                        .HasColumnName("tp_config_id");

                    b.Property<DateTime>("UpdateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp")
                        .HasColumnName("update_time")
                        .HasDefaultValueSql("current_timestamp");

                    b.Property<decimal>("VoiceChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("voice_channel_id");

                    b.HasKey("Id")
                        .HasName("pk_tp_room_instances");

                    b.HasIndex("TpConfigId")
                        .HasDatabaseName("ix_tp_room_instances_tp_config_id");

                    b.ToTable("tp_room_instances", (string)null);
                });

            modelBuilder.Entity("Jellyfish.Command.Role.Data.UserCommandPermission", b =>
                {
                    b.HasOne("Jellyfish.Command.Role.Data.UserRole", "UserRole")
                        .WithMany("CommandPermissions")
                        .HasForeignKey("UserRoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_command_permissions_user_roles_user_role_id");

                    b.Navigation("UserRole");
                });

            modelBuilder.Entity("Jellyfish.Command.TeamPlay.Data.TpRoomInstance", b =>
                {
                    b.HasOne("Jellyfish.Command.TeamPlay.Data.TpConfig", "TpConfig")
                        .WithMany("RoomInstances")
                        .HasForeignKey("TpConfigId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_tp_room_instances_tp_configs_tp_config_id");

                    b.Navigation("TpConfig");
                });

            modelBuilder.Entity("Jellyfish.Command.Role.Data.UserRole", b =>
                {
                    b.Navigation("CommandPermissions");
                });

            modelBuilder.Entity("Jellyfish.Command.TeamPlay.Data.TpConfig", b =>
                {
                    b.Navigation("RoomInstances");
                });
#pragma warning restore 612, 618
        }
    }
}