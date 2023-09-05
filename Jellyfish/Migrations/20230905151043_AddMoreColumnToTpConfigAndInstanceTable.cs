using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreColumnToTpConfigAndInstanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "creator_id",
                table: "tp_room_instances",
                newName: "owner_id");

            migrationBuilder.AlterColumn<int>(
                name: "member_limit",
                table: "tp_room_instances",
                type: "integer",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 10L);

            migrationBuilder.AddColumn<string>(
                name: "command_text",
                table: "tp_room_instances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "room_name",
                table: "tp_room_instances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "voice_quality",
                table: "tp_configs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "tp_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "default_member_limit",
                table: "tp_configs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "room_name_pattern",
                table: "tp_configs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "command_text",
                table: "tp_room_instances");

            migrationBuilder.DropColumn(
                name: "room_name",
                table: "tp_room_instances");

            migrationBuilder.DropColumn(
                name: "default_member_limit",
                table: "tp_configs");

            migrationBuilder.DropColumn(
                name: "room_name_pattern",
                table: "tp_configs");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "tp_room_instances",
                newName: "creator_id");

            migrationBuilder.AlterColumn<long>(
                name: "member_limit",
                table: "tp_room_instances",
                type: "bigint",
                nullable: false,
                defaultValue: 10L,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "voice_quality",
                table: "tp_configs",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "tp_configs",
                type: "boolean",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
