using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyStash.BlazorBff.Features.Transactions;

internal static class TransactionEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapTransactions()
        {
            app.MapGet("/api/fragments/transactions", async (HttpContext httpContext, IHttpClientFactory clientFactory) =>
            {
                var accessToken = await httpContext.GetTokenAsync("access_token");

                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var filter = Uri.EscapeDataString(
                    $"Date ge {sixMonthsAgo.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)}");
                var client = clientFactory.CreateClient("CoreApiClient");

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/odata/Transactions?api-version=1.0&$filter={filter}&$orderby=Date%20desc");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var response = await client.SendAsync(request, httpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode((int)response.StatusCode);
                }

                var feed = await response.Content.ReadFromJsonAsync<ODataFeed<TransactionDto>>(
                    httpContext.RequestAborted);
                if (feed?.Value is null)
                {
                    return Results.Problem(
                        "The transactions response was empty or invalid.",
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return new RazorComponentResult<TransactionList>(
                    new { Transactions = feed.Value });
            });

            return app;
        }
    }
}

internal sealed record ODataFeed<T>(List<T>? Value);

public sealed record TransactionDto(
    DateTime Date,
    decimal Amount,
    bool IsExempt,
    decimal Balance,
    string CategoryName,
    string CategoryTypeName);
