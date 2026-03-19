using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AuthEmailVerificationAndGoogleSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "password");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET "AuthProvider" = 'password',
                    "IsEmailVerified" = TRUE,
                    "EmailVerifiedAt" = COALESCE("EmailVerifiedAt", "CreatedAt");
                """);

            migrationBuilder.CreateTable(
                name: "user_action_tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_action_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_action_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_action_tokens_TokenHash",
                table: "user_action_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_action_tokens_UserId_Type",
                table: "user_action_tokens",
                columns: new[] { "UserId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_action_tokens");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "users");
        }
    }
}
