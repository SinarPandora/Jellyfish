using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class CreateWeiboPushTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:board_type", "score,vote,match")
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:board_type", "score,vote,match")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");

            migrationBuilder.CreateTable(
                name: "weibo_crawl_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uid = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weibo_crawl_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weibo_push_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    alias = table.Column<string>(type: "text", nullable: false),
                    uid = table.Column<string>(type: "text", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weibo_push_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weibo_push_instances",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weibo_push_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_weibo_push_instances_weibo_push_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "weibo_push_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weibo_push_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<long>(type: "bigint", nullable: false),
                    crawl_history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weibo_push_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_weibo_push_histories_weibo_push_instances_instance_id",
                        column: x => x.instance_id,
                        principalTable: "weibo_push_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_configs_alias_guild_id",
                table: "weibo_push_configs",
                columns: new[] { "alias", "guild_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_configs_uid_guild_id",
                table: "weibo_push_configs",
                columns: new[] { "uid", "guild_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_histories_instance_id_crawl_history_id",
                table: "weibo_push_histories",
                columns: new[] { "instance_id", "crawl_history_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_instances_config_id_channel_id",
                table: "weibo_push_instances",
                columns: new[] { "config_id", "channel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weibo_crawl_histories");

            migrationBuilder.DropTable(
                name: "weibo_push_histories");

            migrationBuilder.DropTable(
                name: "weibo_push_instances");

            migrationBuilder.DropTable(
                name: "weibo_push_configs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:board_type", "score,vote,match")
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:board_type", "score,vote,match")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm,thread")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");
        }
    }
}
