using Microsoft.AspNetCore.OData;
using System.Security.Claims;

namespace MyStash.CoreApi.Features.Transactions;

internal static class TransactionEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapTransactions()
        {
            app.MapODataMetadata("odata", TransactionODataModel.GetModel())
                .WithSummary("Get OData metadata");

            app.MapGet("/odata/Transactions", (
                ITransactionService transactionService,
                ClaimsPrincipal user) =>
            {
                return transactionService.GetAll(GetEmailAddress(user)!);
            })
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                !string.IsNullOrWhiteSpace(GetEmailAddress(context.User))))
            .AddODataQueryEndpointFilter(_ => { }, _ => { })
            .WithODataOptions(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Count()
                .SetMaxTop(100))
            .WithODataResult()
            .WithName("GetTransactionsOData")
            .WithSummary("Query transactions with OData")
            .WithDescription("Queries transactions from accounts linked to the authenticated user's email.")
            .Produces<IQueryable<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

            return app;
        }
    }

    private static string? GetEmailAddress(ClaimsPrincipal user)
    {
        var claimTypesEmail = user.FindFirst(ClaimTypes.Email)?.Value;
        return string.IsNullOrWhiteSpace(claimTypesEmail)
            ? user.FindFirst("email")?.Value
            : claimTypesEmail;
    }
}
