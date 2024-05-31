using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddClockInTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clock_in_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    button_text = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    result_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clock_in_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clock_in_channels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    kook_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_sync = table.Column<bool>(type: "boolean", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    read_only = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clock_in_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_clock_in_channels_clock_in_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "clock_in_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clock_in_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clock_in_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_clock_in_histories_clock_in_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "clock_in_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clock_in_stages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    days = table.Column<long>(type: "bigint", nullable: false),
                    must_continuous = table.Column<bool>(type: "boolean", nullable: false),
                    qualified_message_pattern = table.Column<string>(type: "text", nullable: true),
                    qualified_role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clock_in_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_clock_in_stages_clock_in_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "clock_in_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clock_in_qualified_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clock_in_qualified_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_clock_in_qualified_users_clock_in_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "clock_in_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_clock_in_qualified_users_clock_in_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "clock_in_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_channels_config_id",
                table: "clock_in_channels",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_histories_config_id",
                table: "clock_in_histories",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_qualified_users_config_id",
                table: "clock_in_qualified_users",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_qualified_users_stage_id",
                table: "clock_in_qualified_users",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_stages_config_id",
                table: "clock_in_stages",
                column: "config_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clock_in_channels");

            migrationBuilder.DropTable(
                name: "clock_in_histories");

            migrationBuilder.DropTable(
                name: "clock_in_qualified_users");

            migrationBuilder.DropTable(
                name: "clock_in_stages");

            migrationBuilder.DropTable(
                name: "clock_in_configs");
        }
    }
}
