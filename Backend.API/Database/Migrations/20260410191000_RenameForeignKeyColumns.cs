using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameForeignKeyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename veichle_id to vehicle_id in tags table
            migrationBuilder.DropForeignKey(
                name: "fk_tags_vehicles_veichle_id",
                table: "tags");

            migrationBuilder.RenameColumn(
                name: "veichle_id",
                table: "tags",
                newName: "vehicle_id");

            migrationBuilder.RenameIndex(
                name: "ix_tags_veichle_id",
                table: "tags",
                newName: "ix_tags_vehicle_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tags_vehicles_vehicle_id",
                table: "tags",
                column: "vehicle_id",
                principalTable: "vehicles",
                principalColumn: "vehicle_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tags_vehicles_vehicle_id",
                table: "tags");

            migrationBuilder.RenameColumn(
                name: "vehicle_id",
                table: "tags",
                newName: "veichle_id");

            migrationBuilder.RenameIndex(
                name: "ix_tags_vehicle_id",
                table: "tags",
                newName: "ix_tags_veichle_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tags_vehicles_veichle_id",
                table: "tags",
                column: "veichle_id",
                principalTable: "vehicles",
                principalColumn: "vehicle_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
