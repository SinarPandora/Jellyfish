using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfish.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCrawlHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_weibo_push_histories_instance_id_crawl_history_id",
                table: "weibo_push_histories");

            migrationBuilder.DropColumn(
                name: "crawl_history_id",
                table: "weibo_push_histories");

            migrationBuilder.AddColumn<string>(
                name: "hash",
                table: "weibo_push_histories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "content",
                table: "weibo_crawl_histories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "images",
                table: "weibo_crawl_histories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "weibo_crawl_histories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_histories_instance_id",
                table: "weibo_push_histories",
                column: "instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_weibo_push_histories_instance_id",
                table: "weibo_push_histories");

            migrationBuilder.DropColumn(
                name: "hash",
                table: "weibo_push_histories");

            migrationBuilder.DropColumn(
                name: "content",
                table: "weibo_crawl_histories");

            migrationBuilder.DropColumn(
                name: "images",
                table: "weibo_crawl_histories");

            migrationBuilder.DropColumn(
                name: "username",
                table: "weibo_crawl_histories");

            migrationBuilder.AddColumn<Guid>(
                name: "crawl_history_id",
                table: "weibo_push_histories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_weibo_push_histories_instance_id_crawl_history_id",
                table: "weibo_push_histories",
                columns: new[] { "instance_id", "crawl_history_id" },
                unique: true);
        }
    }
}
