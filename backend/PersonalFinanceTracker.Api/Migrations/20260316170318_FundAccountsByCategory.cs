using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class FundAccountsByCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "accounts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_CategoryId",
                table: "accounts",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_categories_CategoryId",
                table: "accounts",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accounts_categories_CategoryId",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "IX_accounts_CategoryId",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "accounts");
        }
    }
}
