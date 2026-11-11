using Microsoft.AspNetCore.Http.HttpResults;

namespace MyStash.CoreApi.Features.Transactions;

internal static class TransactionEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapTransactions()
        {
            app.MapGet("/api/transactions", async Task<Ok<IReadOnlyList<TransactionResponse>>> (
                ITransactionService transactionService,
                CancellationToken cancellationToken) =>
            {
                var transactions = await transactionService.GetAllAsync(cancellationToken);
                return TypedResults.Ok(transactions);
            })
            .WithName("GetAllTransactions")
            .WithSummary("Get all transactions")
            .WithDescription("Returns all transactions, including their account and category IDs.")
            .Produces<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK);

            return app;
        }
    }
}
