using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddPositiveColumnToCreateCountDownChannelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "positive",
                table: "count_down_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "positive",
                table: "count_down_channels");
        }
    }
}
