using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackableColumnsToCardInstanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "create_time",
                table: "clock_in_card_instances",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "current_timestamp");

            migrationBuilder.AddColumn<DateTime>(
                name: "update_time",
                table: "clock_in_card_instances",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "current_timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "create_time",
                table: "clock_in_card_instances");

            migrationBuilder.DropColumn(
                name: "update_time",
                table: "clock_in_card_instances");
        }
    }
}
