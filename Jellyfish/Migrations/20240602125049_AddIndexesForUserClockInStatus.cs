using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForUserClockInStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_clock_in_statuses_config_id",
                table: "user_clock_in_statuses");

            migrationBuilder.CreateIndex(
                name: "ix_user_clock_in_statuses_config_id_user_id",
                table: "user_clock_in_statuses",
                columns: new[] { "config_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_clock_in_statuses_user_id",
                table: "user_clock_in_statuses",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_clock_in_statuses_config_id_user_id",
                table: "user_clock_in_statuses");

            migrationBuilder.DropIndex(
                name: "ix_user_clock_in_statuses_user_id",
                table: "user_clock_in_statuses");

            migrationBuilder.CreateIndex(
                name: "ix_user_clock_in_statuses_config_id",
                table: "user_clock_in_statuses",
                column: "config_id");
        }
    }
}
