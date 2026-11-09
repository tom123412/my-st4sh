using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var auth0Authority = builder.Configuration["Auth0:Authority"];
if (!Uri.TryCreate(auth0Authority, UriKind.Absolute, out var authorityUri)
    || authorityUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException("Auth0:Authority must be configured as an absolute HTTPS URL.");
}

var auth0Audience = builder.Configuration["Auth0:Audience"];
if (string.IsNullOrWhiteSpace(auth0Audience))
{
    throw new InvalidOperationException("Auth0:Audience must be configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authorityUri.ToString();
        options.Audience = auth0Audience;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpClient("CoreApiClient", client =>
{
    client.BaseAddress = new Uri("http://coreapi");
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello World!")
    .AllowAnonymous();

app.MapGet("api/fragments/catalog", async (HttpContext httpContext, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CoreApiClient");
    
    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
    request.Headers.Authorization = AuthenticationHeaderValue.Parse(
        httpContext.Request.Headers.Authorization.ToString());
    using var response = await client.SendAsync(request, httpContext.RequestAborted);

    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(
        httpContext.RequestAborted);

    // Render the data into an HTML fragment for HTMX to swap into the DOM
    var html = new System.Text.StringBuilder();
    if (products is not null)
    {
        foreach (var item in products)
        {
            html.Append($@"
                <div class='catalog-item'>
                    <strong>{item.Name}</strong> ({item.Category}) - ${item.Price} <br>
                    <small>Sourced from CoreApi via Blazor BFF</small>
                </div>");
        }
    }

    return Results.Content(html.ToString(), "text/html");
});

app.UseAuthentication();
app.UseAuthorization();
app.Run();


public record ProductDto(int Id, string Name, string Category, decimal Price);
