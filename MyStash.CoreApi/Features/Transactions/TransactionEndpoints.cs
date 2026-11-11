using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace MyStash.CoreApi.Features.Transactions;

internal static class TransactionEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapTransactions()
        {
            app.MapGet("/api/transactions", async Task<Results<
                Ok<IReadOnlyList<TransactionResponse>>,
                ForbidHttpResult>> (
                ITransactionService transactionService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var emailAddress = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;
                if (string.IsNullOrWhiteSpace(emailAddress))
                    return TypedResults.Forbid();

                var transactions = await transactionService.GetAllAsync(
                    emailAddress,
                    cancellationToken);
                return TypedResults.Ok(transactions);
            })
            .WithName("GetAllTransactions")
            .WithSummary("Get all transactions")
            .WithDescription("Returns all transactions, including their account and category IDs.")
            .Produces<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

            return app;
        }
    }
}
