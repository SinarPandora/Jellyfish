using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class AddFkToTcGroupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "description_message_id",
                table: "tc_group_instances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tc_group_id",
                table: "tc_group_instances",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tc_group_instances_tc_group_id",
                table: "tc_group_instances",
                column: "tc_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances",
                column: "tc_group_id",
                principalTable: "tc_groups",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tc_group_instances_tc_groups_tc_group_id",
                table: "tc_group_instances");

            migrationBuilder.DropIndex(
                name: "ix_tc_group_instances_tc_group_id",
                table: "tc_group_instances");

            migrationBuilder.DropColumn(
                name: "description_message_id",
                table: "tc_group_instances");

            migrationBuilder.DropColumn(
                name: "tc_group_id",
                table: "tc_group_instances");
        }
    }
}
