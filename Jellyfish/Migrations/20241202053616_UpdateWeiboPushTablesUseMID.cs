using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWeiboPushTablesUseMID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                delete from weibo_crawl_histories;
                delete from weibo_push_histories;
                """);

            migrationBuilder.RenameColumn(
                name: "url",
                table: "weibo_crawl_histories",
                newName: "mid");

            migrationBuilder.AddColumn<Guid>(
                name: "message_id",
                table: "weibo_push_histories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "mid",
                table: "weibo_push_histories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                delete from weibo_crawl_histories;
                delete from weibo_push_histories;
                """);

            migrationBuilder.DropColumn(
                name: "message_id",
                table: "weibo_push_histories");

            migrationBuilder.DropColumn(
                name: "mid",
                table: "weibo_push_histories");

            migrationBuilder.RenameColumn(
                name: "mid",
                table: "weibo_crawl_histories",
                newName: "url");
        }
    }
}
