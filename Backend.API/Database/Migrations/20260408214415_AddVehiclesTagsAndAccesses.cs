using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclesTagsAndAccesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the missing enums
            migrationBuilder.Sql("CREATE TYPE tag_status AS ENUM ('Available', 'InUse', 'Inactive');");
            migrationBuilder.Sql("CREATE TYPE access_type AS ENUM ('Entry', 'Exit');");

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "varchar", nullable: false),
                    brand = table.Column<string>(type: "varchar", nullable: false),
                    model = table.Column<string>(type: "varchar", nullable: false),
                    color = table.Column<string>(type: "varchar", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.vehicle_id);
                    table.ForeignKey(
                        name: "fk_vehicles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "tag_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.tag_id);
                    table.ForeignKey(
                        name: "fk_tags_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "vehicle_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "accesses",
                columns: table => new
                {
                    access_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "access_type", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accesses", x => x.access_id);
                    table.ForeignKey(
                        name: "fk_accesses_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accesses_tag_id",
                table: "accesses",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_vehicle_id",
                table: "tags",
                column: "vehicle_id",
                unique: true,
                filter: "status = 'InUse'");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_plate",
                table: "vehicles",
                column: "plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_user_id",
                table: "vehicles",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accesses");

            // Drop the enums
            migrationBuilder.Sql("DROP TYPE access_type;");
            migrationBuilder.Sql("DROP TYPE tag_status;");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
