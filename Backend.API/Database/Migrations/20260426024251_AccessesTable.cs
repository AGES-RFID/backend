using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AccessesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.Acess_Type", "exit,entry")
                .Annotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .Annotation("Npgsql:Enum:public.user_role", "admin,customer")
                .OldAnnotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .OldAnnotation("Npgsql:Enum:public.user_role", "admin,customer");

            migrationBuilder.CreateTable(
                name: "accesses",
                columns: table => new
                {
                    accesses_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "Acess_Type", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accesses", x => x.accesses_id);
                    table.ForeignKey(
                        name: "fk_accesses_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "Tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accesses_tag_id",
                table: "accesses",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accesses");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .Annotation("Npgsql:Enum:public.user_role", "admin,customer")
                .OldAnnotation("Npgsql:Enum:public.Acess_Type", "exit,entry")
                .OldAnnotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .OldAnnotation("Npgsql:Enum:public.user_role", "admin,customer");
        }
    }
}
