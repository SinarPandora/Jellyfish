using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceQualityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "member_limit",
                table: "team_play_room_instance",
                type: "bigint",
                nullable: false,
                defaultValue: 10L,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "team_play_config",
                type: "boolean",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<int>(
                name: "voice_quality",
                table: "team_play_config",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "voice_quality",
                table: "team_play_config");

            migrationBuilder.AlterColumn<long>(
                name: "member_limit",
                table: "team_play_room_instance",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 10L);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "team_play_config",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: true);
        }
    }
}
