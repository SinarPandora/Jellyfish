using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClockInColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "must_continuous",
                table: "clock_in_stages");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "clock_in_configs");

            migrationBuilder.DropColumn(
                name: "is_sync",
                table: "clock_in_channels");

            migrationBuilder.DropColumn(
                name: "read_only",
                table: "clock_in_channels");

            migrationBuilder.AddColumn<long>(
                name: "allow_break_days",
                table: "clock_in_stages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_break_days",
                table: "clock_in_stages");

            migrationBuilder.AddColumn<bool>(
                name: "must_continuous",
                table: "clock_in_stages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "clock_in_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sync",
                table: "clock_in_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "read_only",
                table: "clock_in_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
