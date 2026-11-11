using Asp.Versioning;
using Microsoft.AspNetCore.OData;
using System.Security.Claims;

namespace MyStash.CoreApi.Features.Accounts;

internal static class AccountEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapAccounts()
        {
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            app.MapODataMetadata("odata/Accounts/$metadata", AccountODataModel.GetModel())
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithSummary("Get OData metadata");

            app.MapGet("/odata/Accounts", (
                IAccountService accountService,
                ClaimsPrincipal user) =>
            {
                return accountService.GetAll(GetEmailAddress(user)!);
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
            .WithName("GetAccountsOData")
            .WithSummary("Query accounts with OData")
            .WithDescription("Queries accounts linked to the authenticated user's email.")
            .Produces<IQueryable<AccountResponse>>(StatusCodes.Status200OK)
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
