using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class MakeClockInStageTrackable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "create_time",
                table: "clock_in_stages",
                type: "timestamp",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "update_time",
                table: "clock_in_stages",
                type: "timestamp",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "create_time",
                table: "clock_in_stages");

            migrationBuilder.DropColumn(
                name: "update_time",
                table: "clock_in_stages");
        }
    }
}
