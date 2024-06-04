using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClockInCardTableStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "kook_id",
                table: "clock_in_channels",
                newName: "channel_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "message_id",
                table: "clock_in_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "channel_id",
                table: "clock_in_channels",
                newName: "kook_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "message_id",
                table: "clock_in_channels",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
