using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessFkToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "access_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_access_id",
                table: "transactions",
                column: "access_id");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_accesses_access_id",
                table: "transactions",
                column: "access_id",
                principalTable: "accesses",
                principalColumn: "access_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_accesses_access_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_access_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "access_id",
                table: "transactions");
        }
    }
}
