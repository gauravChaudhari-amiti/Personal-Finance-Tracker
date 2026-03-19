using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class GoalCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "goals",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_goals_CategoryId",
                table: "goals",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_goals_categories_CategoryId",
                table: "goals",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goals_categories_CategoryId",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "IX_goals_CategoryId",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "goals");
        }
    }
}
