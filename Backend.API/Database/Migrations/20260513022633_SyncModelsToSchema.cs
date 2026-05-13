using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelsToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_settings");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "tags");

            // tag_id cannot be cast automatically from text to uuid (e.g. "TAG-001" → uuid).
            // Drop FKs referencing tags.tag_id, truncate, change types, then recreate FKs.
            migrationBuilder.Sql("ALTER TABLE accesses DROP CONSTRAINT fk_accesses_tags_tag_id;");
            migrationBuilder.Sql("ALTER TABLE vehicles DROP CONSTRAINT fk_vehicles_tags_tag_id;");

            migrationBuilder.Sql("TRUNCATE TABLE accesses, vehicles, tags RESTART IDENTITY CASCADE;");

            migrationBuilder.Sql("ALTER TABLE tags ALTER COLUMN tag_id TYPE uuid USING NULL::uuid;");
            migrationBuilder.Sql("ALTER TABLE vehicles ALTER COLUMN tag_id TYPE uuid USING NULL::uuid;");
            migrationBuilder.Sql("ALTER TABLE accesses ALTER COLUMN tag_id TYPE uuid USING NULL::uuid;");

            migrationBuilder.Sql("ALTER TABLE accesses ADD CONSTRAINT fk_accesses_tags_tag_id FOREIGN KEY (tag_id) REFERENCES tags(tag_id);");
            migrationBuilder.Sql("ALTER TABLE vehicles ADD CONSTRAINT fk_vehicles_tags_tag_id FOREIGN KEY (tag_id) REFERENCES tags(tag_id);");

            migrationBuilder.AddColumn<string>(
                name: "epc",
                table: "tags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tid",
                table: "tags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "parking_prices",
                columns: table => new
                {
                    parking_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tolerance_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    base_price = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 15.00m),
                    hourly_rate = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 5.00m),
                    threshold_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 180),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_prices", x => x.parking_price_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_prices");

            migrationBuilder.DropColumn(
                name: "epc",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "tid",
                table: "tags");

            migrationBuilder.RenameTable(
                name: "tags",
                newName: "Tags");

            migrationBuilder.AlterColumn<string>(
                name: "tag_id",
                table: "vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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

            migrationBuilder.CreateTable(
                name: "parking_settings",
                columns: table => new
                {
                    parking_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 15.00m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    hourly_rate = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 5.00m),
                    tolerance_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_settings", x => x.parking_settings_id);
                });
        }
    }
}
