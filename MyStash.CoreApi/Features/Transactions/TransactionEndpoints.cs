using Microsoft.AspNetCore.OData;
using Asp.Versioning;
using System.Security.Claims;

namespace MyStash.CoreApi.Features.Transactions;

internal static class TransactionEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapTransactions()
        {
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            app.MapODataMetadata("odata/Transactions/$metadata", TransactionODataModel.GetModel())
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithSummary("Get OData metadata");

            app.MapGet("/odata/Transactions", (
                ITransactionService transactionService,
                ClaimsPrincipal user) =>
            {
                return transactionService.GetAll(GetEmailAddress(user)!);
            })
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                !string.IsNullOrWhiteSpace(GetEmailAddress(context.User))))
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
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
