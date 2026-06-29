using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reader_status",
                columns: table => new
                {
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_status = table.Column<string>(type: "text", nullable: false),
                    last_ping = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    antenna_list = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_status", x => x.reader_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reader_status");
        }
    }
}
