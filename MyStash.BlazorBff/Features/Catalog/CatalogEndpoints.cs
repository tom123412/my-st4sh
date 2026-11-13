using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace MyStash.BlazorBff.Features.Catalog;

internal static class CatalogEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapCatalog()
        {
            app.MapGet("/api/fragments/catalog", async (HttpContext httpContext, IHttpClientFactory clientFactory) =>
            {
                var accessToken = await httpContext.GetTokenAsync("access_token");
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return Results.Unauthorized();
                }

                var client = clientFactory.CreateClient("CoreApiClient");

                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var response = await client.SendAsync(request, httpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode((int)response.StatusCode);
                }

                var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(
                    httpContext.RequestAborted);

                var html = new System.Text.StringBuilder();
                if (products is not null)
                {
                    foreach (var item in products)
                    {
                        html.Append($@"
                <div class='mt-3 rounded-lg border-l-4 border-sky-600 bg-sky-50 px-4 py-3 text-slate-700'>
                    <strong class='font-semibold text-slate-900'>{item.Name}</strong> ({item.Category}) - ${item.Price} <br>
                    <small class='text-sm text-slate-500'>Sourced from CoreApi via Blazor BFF</small>
                </div>");
                    }
                }

                return Results.Content(html.ToString(), "text/html");
            });

            return app;
        }
    }
}

internal sealed record ProductDto(int Id, string Name, string Category, decimal Price);
