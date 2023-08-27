using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddTextChannelIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "channel_id",
                table: "team_play_config",
                newName: "voice_channel_id");

            migrationBuilder.AddColumn<decimal>(
                name: "text_channel_id",
                table: "team_play_config",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "text_channel_id",
                table: "team_play_config");

            migrationBuilder.RenameColumn(
                name: "voice_channel_id",
                table: "team_play_config",
                newName: "channel_id");
        }
    }
}
