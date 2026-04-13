using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class TagTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    tag_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.tag_id);
                });

            migrationBuilder.AddColumn<string>(
                name: "tag_id",
                table: "vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_tag_id",
                table: "vehicles",
                column: "tag_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_vehicles_tags_tag_id",
                table: "vehicles",
                column: "tag_id",
                principalTable: "Tags",
                principalColumn: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vehicles_tags_tag_id",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_tag_id",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "tag_id",
                table: "vehicles");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
