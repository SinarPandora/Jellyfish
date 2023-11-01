using Jellyfish.Core.Enum;
using Jellyfish.Module.ExpireExtendSession.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddExpireExtendSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "room_name",
                table: "tmp_text_channel_instances",
                newName: "name");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .Annotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .Annotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");

            migrationBuilder.AddColumn<decimal>(
                name: "channel_id",
                table: "tmp_text_channel_instances",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "expire_extend_sessions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    target_id = table.Column<long>(type: "bigint", nullable: false),
                    target_type = table.Column<ExtendTargetType>(type: "extend_target_type", nullable: false),
                    value = table.Column<long>(type: "bigint", nullable: false),
                    time_unit = table.Column<TimeUnit>(type: "time_unit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expire_extend_sessions", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expire_extend_sessions");

            migrationBuilder.DropColumn(
                name: "channel_id",
                table: "tmp_text_channel_instances");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tmp_text_channel_instances",
                newName: "room_name");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:channel_type", "unspecified,category,text,voice,dm")
                .OldAnnotation("Npgsql:Enum:extend_target_type", "tmp_text_channel")
                .OldAnnotation("Npgsql:Enum:time_unit", "second,minute,hour,day,week,month");
        }
    }
}
