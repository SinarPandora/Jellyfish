using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class FixDefaultValueIssuesAndUpdateClockInColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "qualified_message_pattern",
                table: "clock_in_stages",
                newName: "qualified_message");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "user_roles",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "tp_configs",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "clock_in_stages",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<long>(
                name: "allow_break_days",
                table: "clock_in_stages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "clock_in_stages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "clock_in_configs",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "button_text",
                table: "clock_in_configs",
                type: "text",
                nullable: false,
                defaultValue: "打卡！",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "all_clock_in_count",
                table: "clock_in_configs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "today_clock_in_count",
                table: "clock_in_configs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<bool>(
                name: "finished",
                table: "board_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_configs_guild_id",
                table: "clock_in_configs",
                column: "guild_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clock_in_configs_guild_id",
                table: "clock_in_configs");

            migrationBuilder.DropColumn(
                name: "name",
                table: "clock_in_stages");

            migrationBuilder.DropColumn(
                name: "all_clock_in_count",
                table: "clock_in_configs");

            migrationBuilder.DropColumn(
                name: "today_clock_in_count",
                table: "clock_in_configs");

            migrationBuilder.RenameColumn(
                name: "qualified_message",
                table: "clock_in_stages",
                newName: "qualified_message_pattern");

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "user_roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "tp_configs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "clock_in_stages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<long>(
                name: "allow_break_days",
                table: "clock_in_stages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<bool>(
                name: "enabled",
                table: "clock_in_configs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "button_text",
                table: "clock_in_configs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "打卡！");

            migrationBuilder.AlterColumn<bool>(
                name: "finished",
                table: "board_configs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
