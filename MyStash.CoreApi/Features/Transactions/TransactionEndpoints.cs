using Microsoft.AspNetCore.OData;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
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
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
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

            app.MapGet("/odata/Transactions({id:int})", async Task<Results<
                Ok<TransactionResponse>,
                NotFound,
                ForbidHttpResult>> (
                int id,
                ITransactionService transactionService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var emailAddress = GetEmailAddress(user);
                if (string.IsNullOrWhiteSpace(emailAddress))
                    return TypedResults.Forbid();

                var transaction = await transactionService.GetByIdAsync(
                    id,
                    emailAddress,
                    cancellationToken);
                return transaction is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(transaction);
            })
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("GetTransactionOData")
            .WithSummary("Get a transaction by ID")
            .WithDescription("Returns a transaction only if its account is linked to the authenticated user's email.")
            .Produces<TransactionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

            app.MapPost("/odata/Transactions", async Task<Results<
                Created<TransactionResponse>,
                NotFound,
                ForbidHttpResult>> (
                CreateTransactionRequest request,
                ITransactionService transactionService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var emailAddress = GetEmailAddress(user);
                if (string.IsNullOrWhiteSpace(emailAddress))
                    return TypedResults.Forbid();

                var transaction = await transactionService.CreateAsync(
                    request,
                    emailAddress,
                    cancellationToken);
                return transaction is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created(
                        $"/odata/Transactions({transaction.Id})",
                        transaction);
            })
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                !string.IsNullOrWhiteSpace(GetEmailAddress(context.User))))
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("CreateTransactionOData")
            .WithSummary("Create a transaction")
            .WithDescription("Creates a transaction for an active category and an account linked to the authenticated user's email.")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
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
