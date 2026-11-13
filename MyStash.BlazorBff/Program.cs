using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using MyStash.BlazorBff.Features.Authentication;
using MyStash.BlazorBff.Features.Catalog;
using MyStash.BlazorBff.Features.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents();

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

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options => options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    });

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

app.MapAuthentication();
app.MapCatalog();
app.MapTransactions();

app.UseAuthentication();
app.UseAuthorization();
app.Run();
