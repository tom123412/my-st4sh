using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});

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

var auth0ClientId = builder.Configuration["Auth0:ClientId"];
if (string.IsNullOrWhiteSpace(auth0ClientId))
{
    throw new InvalidOperationException("Auth0:ClientId must be configured.");
}

var auth0ClientSecret = builder.Configuration["Auth0:ClientSecret"];
if (string.IsNullOrWhiteSpace(auth0ClientSecret))
{
    throw new InvalidOperationException("Auth0:ClientSecret must be configured.");
}

builder.Services
    .AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = authorityUri.Host;
        options.ClientId = auth0ClientId;
        options.ClientSecret = auth0ClientSecret;
        options.ResponseType = "code";
        options.CallbackPath = "/api/callback";
        options.Scope = "openid profile email";
    })
    .WithAccessToken(options => options.Audience = auth0Audience);

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());

builder.Services.AddHttpClient("CoreApiClient", client =>
{
    client.BaseAddress = new Uri("https://coreapi");
});

var app = builder.Build();

app.UseForwardedHeaders();
app.MapDefaultEndpoints();

app
    .MapGet("/", () => "Hello World!")
    .AllowAnonymous();

app
    .MapGet("/api/login", (string? returnUrl) =>
    {
        var properties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(IsLocalReturnUrl(returnUrl) ? returnUrl! : "/")
            .Build();
        return Results.Challenge(properties, [Auth0Constants.AuthenticationScheme]);
    })
    .AllowAnonymous()
    ;

app.MapPost("/api/logout", async (HttpContext httpContext) =>
{
    var properties = new LogoutAuthenticationPropertiesBuilder()
        .WithRedirectUri("/")
        .Build();

    await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, properties);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Empty;
});

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

static bool IsLocalReturnUrl(string? returnUrl) =>
    !string.IsNullOrEmpty(returnUrl)
    && returnUrl[0] == '/'
    && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));

public record ProductDto(int Id, string Name, string Category, decimal Price);
