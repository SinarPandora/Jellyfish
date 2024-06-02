using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameColumnToClockInUserStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_status_use",
                table: "clock_in_stage_qualified_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_user_clock_in_status_clock_in_configs_config_id",
                table: "user_clock_in_status");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_clock_in_status",
                table: "user_clock_in_status");

            migrationBuilder.RenameTable(
                name: "user_clock_in_status",
                newName: "user_clock_in_statuses");

            migrationBuilder.RenameIndex(
                name: "ix_user_clock_in_status_config_id",
                table: "user_clock_in_statuses",
                newName: "ix_user_clock_in_statuses_config_id");

            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "user_clock_in_statuses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_clock_in_statuses",
                table: "user_clock_in_statuses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_statuses_u",
                table: "clock_in_stage_qualified_histories",
                column: "user_status_id",
                principalTable: "user_clock_in_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_clock_in_statuses_clock_in_configs_config_id",
                table: "user_clock_in_statuses",
                column: "config_id",
                principalTable: "clock_in_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_statuses_u",
                table: "clock_in_stage_qualified_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_user_clock_in_statuses_clock_in_configs_config_id",
                table: "user_clock_in_statuses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_clock_in_statuses",
                table: "user_clock_in_statuses");

            migrationBuilder.DropColumn(
                name: "username",
                table: "user_clock_in_statuses");

            migrationBuilder.RenameTable(
                name: "user_clock_in_statuses",
                newName: "user_clock_in_status");

            migrationBuilder.RenameIndex(
                name: "ix_user_clock_in_statuses_config_id",
                table: "user_clock_in_status",
                newName: "ix_user_clock_in_status_config_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_clock_in_status",
                table: "user_clock_in_status",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_status_use",
                table: "clock_in_stage_qualified_histories",
                column: "user_status_id",
                principalTable: "user_clock_in_status",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_clock_in_status_clock_in_configs_config_id",
                table: "user_clock_in_status",
                column: "config_id",
                principalTable: "clock_in_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
