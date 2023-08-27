using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserRoleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_team_play_room_instance_team_play_config_tp_config_id",
                table: "team_play_room_instance");

            migrationBuilder.DropPrimaryKey(
                name: "pk_team_play_room_instance",
                table: "team_play_room_instance");

            migrationBuilder.DropPrimaryKey(
                name: "pk_team_play_config",
                table: "team_play_config");

            migrationBuilder.RenameTable(
                name: "team_play_room_instance",
                newName: "tp_room_instances");

            migrationBuilder.RenameTable(
                name: "team_play_config",
                newName: "tp_configs");

            migrationBuilder.RenameIndex(
                name: "ix_team_play_room_instance_tp_config_id",
                table: "tp_room_instances",
                newName: "ix_tp_room_instances_tp_config_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tp_room_instances",
                table: "tp_room_instances",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tp_configs",
                table: "tp_configs",
                column: "id");

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    kook_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_command_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_role_id = table.Column<long>(type: "bigint", nullable: false),
                    command_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_command_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_command_permissions_user_roles_user_role_id",
                        column: x => x.user_role_id,
                        principalTable: "user_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_command_permissions_user_role_id",
                table: "user_command_permissions",
                column: "user_role_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tp_room_instances_tp_configs_tp_config_id",
                table: "tp_room_instances",
                column: "tp_config_id",
                principalTable: "tp_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tp_room_instances_tp_configs_tp_config_id",
                table: "tp_room_instances");

            migrationBuilder.DropTable(
                name: "user_command_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tp_room_instances",
                table: "tp_room_instances");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tp_configs",
                table: "tp_configs");

            migrationBuilder.RenameTable(
                name: "tp_room_instances",
                newName: "team_play_room_instance");

            migrationBuilder.RenameTable(
                name: "tp_configs",
                newName: "team_play_config");

            migrationBuilder.RenameIndex(
                name: "ix_tp_room_instances_tp_config_id",
                table: "team_play_room_instance",
                newName: "ix_team_play_room_instance_tp_config_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_team_play_room_instance",
                table: "team_play_room_instance",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_team_play_config",
                table: "team_play_config",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_team_play_room_instance_team_play_config_tp_config_id",
                table: "team_play_room_instance",
                column: "tp_config_id",
                principalTable: "team_play_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
