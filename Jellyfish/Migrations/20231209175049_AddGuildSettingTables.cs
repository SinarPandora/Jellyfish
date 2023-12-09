using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildSettingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");

            migrationBuilder.CreateTable(
                name: "guild_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp"),
                    Setting = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_settings");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month")
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:guild_custom_feature", "splatoon_game,bot_splatoon3")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");
        }
    }
}
