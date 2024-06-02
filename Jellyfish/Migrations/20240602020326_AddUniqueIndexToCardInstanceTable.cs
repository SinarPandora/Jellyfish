using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToCardInstanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_channels_clock_in_configs_config_id",
                table: "clock_in_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_qualified_users_clock_in_stages_stage_id",
                table: "clock_in_qualified_users");

            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_qualified_users_user_clock_in_status_user_status_id",
                table: "clock_in_qualified_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_clock_in_qualified_users",
                table: "clock_in_qualified_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_clock_in_channels",
                table: "clock_in_channels");

            migrationBuilder.DropIndex(
                name: "ix_clock_in_channels_config_id",
                table: "clock_in_channels");

            migrationBuilder.RenameTable(
                name: "clock_in_qualified_users",
                newName: "clock_in_stage_qualified_histories");

            migrationBuilder.RenameTable(
                name: "clock_in_channels",
                newName: "clock_in_card_instances");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_qualified_users_user_status_id",
                table: "clock_in_stage_qualified_histories",
                newName: "ix_clock_in_stage_qualified_histories_user_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_qualified_users_stage_id",
                table: "clock_in_stage_qualified_histories",
                newName: "ix_clock_in_stage_qualified_histories_stage_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_clock_in_stage_qualified_histories",
                table: "clock_in_stage_qualified_histories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_clock_in_card_instances",
                table: "clock_in_card_instances",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_card_instances_config_id_channel_id",
                table: "clock_in_card_instances",
                columns: new[] { "config_id", "channel_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_card_instances_clock_in_configs_config_id",
                table: "clock_in_card_instances",
                column: "config_id",
                principalTable: "clock_in_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_stage_qualified_histories_clock_in_stages_stage_id",
                table: "clock_in_stage_qualified_histories",
                column: "stage_id",
                principalTable: "clock_in_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_status_use",
                table: "clock_in_stage_qualified_histories",
                column: "user_status_id",
                principalTable: "user_clock_in_status",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_card_instances_clock_in_configs_config_id",
                table: "clock_in_card_instances");

            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_stage_qualified_histories_clock_in_stages_stage_id",
                table: "clock_in_stage_qualified_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_clock_in_stage_qualified_histories_user_clock_in_status_use",
                table: "clock_in_stage_qualified_histories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_clock_in_stage_qualified_histories",
                table: "clock_in_stage_qualified_histories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_clock_in_card_instances",
                table: "clock_in_card_instances");

            migrationBuilder.DropIndex(
                name: "ix_clock_in_card_instances_config_id_channel_id",
                table: "clock_in_card_instances");

            migrationBuilder.RenameTable(
                name: "clock_in_stage_qualified_histories",
                newName: "clock_in_qualified_users");

            migrationBuilder.RenameTable(
                name: "clock_in_card_instances",
                newName: "clock_in_channels");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_stage_qualified_histories_user_status_id",
                table: "clock_in_qualified_users",
                newName: "ix_clock_in_qualified_users_user_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_clock_in_stage_qualified_histories_stage_id",
                table: "clock_in_qualified_users",
                newName: "ix_clock_in_qualified_users_stage_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_clock_in_qualified_users",
                table: "clock_in_qualified_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_clock_in_channels",
                table: "clock_in_channels",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_clock_in_channels_config_id",
                table: "clock_in_channels",
                column: "config_id");

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_channels_clock_in_configs_config_id",
                table: "clock_in_channels",
                column: "config_id",
                principalTable: "clock_in_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_qualified_users_clock_in_stages_stage_id",
                table: "clock_in_qualified_users",
                column: "stage_id",
                principalTable: "clock_in_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clock_in_qualified_users_user_clock_in_status_user_status_id",
                table: "clock_in_qualified_users",
                column: "user_status_id",
                principalTable: "user_clock_in_status",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
