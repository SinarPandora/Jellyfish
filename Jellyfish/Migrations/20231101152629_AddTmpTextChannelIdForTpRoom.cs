using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddTmpTextChannelIdForTpRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tmp_text_channel_id",
                table: "tp_room_instances",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tp_room_instances_tmp_text_channel_id",
                table: "tp_room_instances",
                column: "tmp_text_channel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tp_room_instances_tmp_text_channels_tmp_text_channel_id",
                table: "tp_room_instances",
                column: "tmp_text_channel_id",
                principalTable: "tmp_text_channels",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tp_room_instances_tmp_text_channels_tmp_text_channel_id",
                table: "tp_room_instances");

            migrationBuilder.DropIndex(
                name: "ix_tp_room_instances_tmp_text_channel_id",
                table: "tp_room_instances");

            migrationBuilder.DropColumn(
                name: "tmp_text_channel_id",
                table: "tp_room_instances");
        }
    }
}
