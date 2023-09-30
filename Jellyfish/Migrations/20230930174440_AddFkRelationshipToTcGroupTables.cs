using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddFkRelationshipToTcGroupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances");

            migrationBuilder.AlterColumn<long>(
                name: "tc_group_id",
                table: "tc_group_instances",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances",
                column: "tc_group_id",
                principalTable: "tc_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances");

            migrationBuilder.AlterColumn<long>(
                name: "tc_group_id",
                table: "tc_group_instances",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances",
                column: "tc_group_id",
                principalTable: "tc_groups",
                principalColumn: "id");
        }
    }
}
