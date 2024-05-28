using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToCreateCountDownChannelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_count_down_channels_guild_id_channel_id",
                table: "count_down_channels",
                columns: new[] { "guild_id", "channel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_count_down_channels_guild_id_channel_id",
                table: "count_down_channels");
        }
    }
}
