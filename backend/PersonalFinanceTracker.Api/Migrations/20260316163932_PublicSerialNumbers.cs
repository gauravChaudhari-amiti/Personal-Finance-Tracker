using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class PublicSerialNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserNumber",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TransactionNumber",
                table: "transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS user_number_seq AS integer START WITH 1 INCREMENT BY 1;

                UPDATE users AS u
                SET "UserNumber" = numbered.row_num
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt", "Id") AS row_num
                    FROM users
                ) AS numbered
                WHERE u."Id" = numbered."Id";

                SELECT setval('user_number_seq', COALESCE((SELECT MAX("UserNumber") FROM users), 0));

                ALTER TABLE users ALTER COLUMN "UserNumber" SET DEFAULT nextval('user_number_seq');
                ALTER TABLE users ALTER COLUMN "UserNumber" SET NOT NULL;
                """);

            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS transaction_number_seq AS bigint START WITH 1 INCREMENT BY 1;

                UPDATE transactions AS t
                SET "TransactionNumber" = numbered.row_num
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt", "Id") AS row_num
                    FROM transactions
                ) AS numbered
                WHERE t."Id" = numbered."Id";

                SELECT setval('transaction_number_seq', COALESCE((SELECT MAX("TransactionNumber") FROM transactions), 0));

                ALTER TABLE transactions ALTER COLUMN "TransactionNumber" SET DEFAULT nextval('transaction_number_seq');
                ALTER TABLE transactions ALTER COLUMN "TransactionNumber" SET NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_UserNumber",
                table: "users",
                column: "UserNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransactionNumber",
                table: "transactions",
                column: "TransactionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_UserNumber",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_transactions_TransactionNumber",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "UserNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "transactions");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS user_number_seq;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS transaction_number_seq;");
        }
    }
}
