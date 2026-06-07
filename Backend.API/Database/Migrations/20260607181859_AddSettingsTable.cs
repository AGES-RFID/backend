using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_occupancy",
                table: "settings");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "value",
                table: "settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_settings_name",
                table: "settings",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_settings_name",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "name",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "value",
                table: "settings");

            migrationBuilder.AddColumn<int>(
                name: "max_occupancy",
                table: "settings",
                type: "integer",
                nullable: false,
                defaultValue: 100);
        }
    }
}
