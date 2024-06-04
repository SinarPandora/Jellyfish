using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNameColumnForClockInTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "clock_in_configs");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "clock_in_configs",
                type: "text",
                nullable: false,
                defaultValue: "每日打卡",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "clock_in_configs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "每日打卡");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "clock_in_configs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
