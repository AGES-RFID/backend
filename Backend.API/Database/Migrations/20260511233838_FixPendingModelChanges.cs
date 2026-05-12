using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vehicles_tags_tag_id",
                table: "vehicles");

            migrationBuilder.DropForeignKey(
                name: "fk_accesses_tags_tag_id",
                table: "accesses");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_tags_tid",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "tid",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "tid",
                table: "accesses");

            migrationBuilder.AlterColumn<string>(
                name: "tag_id",
                table: "Tags",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "tag_id",
                table: "accesses",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_accesses_tags_tag_id",
                table: "accesses",
                column: "tag_id",
                principalTable: "Tags",
                principalColumn: "tag_id");

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

            migrationBuilder.DropForeignKey(
                name: "fk_accesses_tags_tag_id",
                table: "accesses");

            migrationBuilder.AlterColumn<Guid>(
                name: "tag_id",
                table: "Tags",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "tid",
                table: "Tags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "tag_id",
                table: "accesses",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "tid",
                table: "accesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_tags_tid",
                table: "Tags",
                column: "tid");

            migrationBuilder.AddForeignKey(
                name: "fk_accesses_tags_tag_id",
                table: "accesses",
                column: "tag_id",
                principalTable: "Tags",
                principalColumn: "tag_id");

            migrationBuilder.AddForeignKey(
                name: "fk_vehicles_tags_tag_id",
                table: "vehicles",
                column: "tag_id",
                principalTable: "Tags",
                principalColumn: "tid");
        }
    }
}
