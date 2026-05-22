using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parking_settings",
                columns: table => new
                {
                    parking_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tolerance_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    base_price = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 15.00m),
                    hourly_rate = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 5.00m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_settings", x => x.parking_settings_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_settings");
        }
    }
}
