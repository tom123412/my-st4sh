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
            migrationBuilder.Sql(
                sql: @"
                    CREATE OR ALTER TRIGGER [dbo].[TR_Transactions_UpdateBalance]
                    ON [dbo].[Transactions]
                    AFTER INSERT, UPDATE, DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
                        IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

                        WITH AffectedAccounts AS (
                            SELECT AccountId FROM inserted WHERE AccountId IS NOT NULL
                            UNION
                            SELECT AccountId FROM deleted WHERE AccountId IS NOT NULL
                        ),
                        Calculated AS (
                            SELECT
                                t.Id,
                                ROW_NUMBER() OVER (PARTITION BY t.AccountId ORDER BY t.[Date], t.Id) AS NewRowIndex,
                                SUM(
                                    CASE
                                        WHEN c.CategoryTypeId = 2 THEN t.Amount
                                        WHEN c.CategoryTypeId = 1 THEN -t.Amount
                                        ELSE 0
                                    END
                                ) OVER (
                                    PARTITION BY t.AccountId
                                    ORDER BY t.[Date], t.Id
                                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                                ) AS NewBalance
                            FROM [dbo].[Transactions] t
                            INNER JOIN AffectedAccounts aa ON t.AccountId = aa.AccountId
                            LEFT JOIN [dbo].[Categories] c ON t.CategoryId = c.Id
                        )
                        UPDATE t
                        SET
                            t.RowIndex = calc.NewRowIndex,
                            t.Balance = COALESCE(calc.NewBalance, 0)
                        FROM [dbo].[Transactions] t
                        INNER JOIN Calculated calc ON t.Id = calc.Id;
                    END;';",
                 suppressTransaction: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_Transactions_UpdateBalance];");
        }
    }
}
