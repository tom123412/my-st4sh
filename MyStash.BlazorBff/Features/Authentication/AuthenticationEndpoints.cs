using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MyStash.BlazorBff.Features.Authentication;

internal static class AuthenticationEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapAuthentication()
        {
            app
                .MapGet("/api/login", (string? returnUrl) =>
                {
                    var properties = new LoginAuthenticationPropertiesBuilder()
                        .WithRedirectUri(IsLocalReturnUrl(returnUrl) ? returnUrl! : "/")
                        .Build();
                    return Results.Challenge(properties, [Auth0Constants.AuthenticationScheme]);
                })
                .AllowAnonymous();

            app.MapPost("/api/logout", async (HttpContext httpContext) =>
            {
                var properties = new LogoutAuthenticationPropertiesBuilder()
                    .WithRedirectUri("/")
                    .Build();

                await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, properties);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Empty;
            });

            return app;
        }
    }

    private static bool IsLocalReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl)
        && returnUrl[0] == '/'
        && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));
}
