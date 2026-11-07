var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("CoreApiClient", client =>
{
    client.BaseAddress = new Uri("http://core-api");
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello World!");

app.MapGet("/fragments/catalog", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CoreApiClient");
    
    // Fetch the JSON data from the internal Core API
    var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");

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

app.Run();


public record ProductDto(int Id, string Name, string Category, decimal Price);
