using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnsForSimplifyLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "member_limit",
                table: "tp_room_instances");

            migrationBuilder.DropColumn(
                name: "voice_quality",
                table: "tp_configs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "member_limit",
                table: "tp_room_instances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "voice_quality",
                table: "tp_configs",
                type: "integer",
                nullable: true);
        }
    }
}
