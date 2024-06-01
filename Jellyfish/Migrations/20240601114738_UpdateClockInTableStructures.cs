using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClockInTableStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_qualified_users_clock_in_configs_config_id",
                table: "clock_in_qualified_users");

            migrationBuilder.DropColumn(
                name: "update_time",
                table: "clock_in_qualified_users");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "clock_in_qualified_users");

            migrationBuilder.RenameColumn(
                name: "config_id",
                table: "clock_in_qualified_users",
                newName: "user_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_qualified_users_config_id",
                table: "clock_in_qualified_users",
                newName: "ix_clock_in_qualified_users_user_status_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "clock_in_histories",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "current_timestamp",
                oldClrType: typeof(DateTime),
                oldType: "timestamp");

            migrationBuilder.CreateTable(
                name: "user_clock_in_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    all_clock_in_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_clock_in_today = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_clock_in_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_clock_in_status_clock_in_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "clock_in_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_histories_create_time",
                table: "clock_in_histories",
                column: "create_time",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_histories_user_id",
                table: "clock_in_histories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_clock_in_status_config_id",
                table: "user_clock_in_status",
                column: "config_id");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_qualified_users_user_clock_in_status_user_status_id",
                table: "clock_in_qualified_users",
                column: "user_status_id",
                principalTable: "user_clock_in_status",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_qualified_users_user_clock_in_status_user_status_id",
                table: "clock_in_qualified_users");

            migrationBuilder.DropTable(
                name: "user_clock_in_status");

            migrationBuilder.DropIndex(
                name: "ix_clock_in_histories_create_time",
                table: "clock_in_histories");

            migrationBuilder.DropIndex(
                name: "ix_clock_in_histories_user_id",
                table: "clock_in_histories");

            migrationBuilder.RenameColumn(
                name: "user_status_id",
                table: "clock_in_qualified_users",
                newName: "config_id");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_qualified_users_user_status_id",
                table: "clock_in_qualified_users",
                newName: "ix_clock_in_qualified_users_config_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "update_time",
                table: "clock_in_qualified_users",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "current_timestamp");

            migrationBuilder.AddColumn<decimal>(
                name: "user_id",
                table: "clock_in_qualified_users",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "clock_in_histories",
                type: "timestamp",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "current_timestamp");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_qualified_users_clock_in_configs_config_id",
                table: "clock_in_qualified_users",
                column: "config_id",
                principalTable: "clock_in_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
