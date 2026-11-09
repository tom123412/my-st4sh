using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using MyStash.CoreApi.Data;

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

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddSqlServerDbContext<MyStashDbContext>("appdb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyStashDbContext>();
    // await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapGet("/api/products", () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, Name = "Cloud Integration Engine", Category = "Enterprise", Price = 299.00m },
        new { Id = 2, Name = "Serverless Ingress Mesh", Category = "Standard", Price = 49.00m }
    });
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
        return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
