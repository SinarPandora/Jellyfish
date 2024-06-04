using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClockInHistoryReferenceUserIdToStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clock_in_histories_user_id",
                table: "clock_in_histories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "clock_in_histories");

            migrationBuilder.AddColumn<long>(
                name: "user_status_id",
                table: "clock_in_histories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_histories_user_status_id",
                table: "clock_in_histories",
                column: "user_status_id");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_histories_user_clock_in_statuses_user_status_id",
                table: "clock_in_histories",
                column: "user_status_id",
                principalTable: "user_clock_in_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_histories_user_clock_in_statuses_user_status_id",
                table: "clock_in_histories");

            migrationBuilder.DropIndex(
                name: "ix_clock_in_histories_user_status_id",
                table: "clock_in_histories");

            migrationBuilder.DropColumn(
                name: "user_status_id",
                table: "clock_in_histories");

            migrationBuilder.AddColumn<decimal>(
                name: "user_id",
                table: "clock_in_histories",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_histories_user_id",
                table: "clock_in_histories",
                column: "user_id");
        }
    }
}
