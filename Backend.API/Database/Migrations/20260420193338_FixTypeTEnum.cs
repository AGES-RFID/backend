using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixTypeTEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "transactions",
                newName: "transaction_id");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .Annotation("Npgsql:Enum:public.user_role", "admin,customer")
                .OldAnnotation("Npgsql:Enum:public.user_role", "admin,customer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "transaction_id",
                table: "transactions",
                newName: "id");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.user_role", "admin,customer")
                .OldAnnotation("Npgsql:Enum:public.transaction_type", "deposit,withdrawal")
                .OldAnnotation("Npgsql:Enum:public.user_role", "admin,customer");
        }
    }
}
