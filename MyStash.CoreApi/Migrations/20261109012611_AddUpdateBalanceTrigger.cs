using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStash.CoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateBalanceTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [dbo].[TR_Transactions_UpdateBalance]
                ON [dbo].[Transactions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Guard against infinite trigger recursion
                    IF TRIGGER_NESTLEVEL() > 1 RETURN;

                    SET RowIndex = t.r
                    FROM
                        (SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY accountid ORDER BY date, id) AS r
                        FROM Transactions
                        WHERE
                            AccountId IN (SELECT AccountId FROM inserted) OR AccountId IN (SELECT AccountId FROM deleted)
                        ) t
                    WHERE Transactions.Id = t.Id

                    UPDATE Transactions
                    SET Balance = COALESCE(t1.c, 0) - COALESCE(t1.d, 0)
                    FROM
                        (SELECT
                            (SELECT
                                SUM(Amount)
                            FROM Transactions
                            WHERE RowIndex <= t.RowIndex AND AccountId = t.AccountId AND Categoryid IN (SELECT Id FROM Categories WHERE CategoryTypeId = 2)
                            ) c,
                            (SELECT
                                SUM(Amount)
                            FROM Transactions
                            WHERE RowIndex <= t.RowIndex AND AccountId = t.AccountId AND Categoryid IN (SELECT Id FROM Categories WHERE CategoryTypeId = 1)
                            ) d,
                            Id
                        FROM Transactions t
                        WHERE AccountId IN (SELECT AccountId FROM inserted) OR AccountId IN (SELECT AccountId FROM deleted)
                        ) t1
                    WHERE Transactions.Id = t1.Id
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS [dbo].[TR_Transactions_UpdateBalance];
                """);
        }
    }
}
