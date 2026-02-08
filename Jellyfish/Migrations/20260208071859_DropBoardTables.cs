using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class DropBoardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_instances");

            migrationBuilder.DropTable(
                name: "board_item_histories");

            migrationBuilder.DropTable(
                name: "board_permissions");

            migrationBuilder.DropTable(
                name: "board_items");

            migrationBuilder.DropTable(
                name: "board_configs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:board_type", "score,vote,match")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:board_type", "score,vote,match")
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");

            migrationBuilder.CreateTable(
                name: "board_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    board_type = table.Column<int>(type: "board_type", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    details = table.Column<string>(type: "text", nullable: false),
                    due = table.Column<DateTime>(type: "timestamp", nullable: false),
                    finished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_template = table.Column<bool>(type: "boolean", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "board_instances",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_instances_board_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "board_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    button_id = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    count_cache = table.Column<long>(type: "bigint", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    name = table.Column<string>(type: "text", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_items_board_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "board_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    is_role = table.Column<bool>(type: "boolean", nullable: false),
                    kook_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_permissions_board_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "board_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_item_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_histories_board_items_item_id",
                        column: x => x.item_id,
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_board_instances_config_id",
                table: "board_instances",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_item_histories_item_id",
                table: "board_item_histories",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_items_config_id",
                table: "board_items",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_permissions_config_id",
                table: "board_permissions",
                column: "config_id");
        }
    }
}
