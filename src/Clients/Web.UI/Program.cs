using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Web.UI;
using Web.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API Gateway base address
var apiGatewayBaseAddress = builder.Configuration["ApiGateway:BaseAddress"] ?? "http://localhost:5000";

// Register HTTP Clients for API services
builder.Services.AddHttpClient<ICatalogService, CatalogService>(client =>
{
    var baseUri = new Uri($"{apiGatewayBaseAddress}/catalog-service/");
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<IBasketService, BasketService>(client =>
{
    var baseUri = new Uri($"{apiGatewayBaseAddress}/basket-service/");
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<IOrderingService, OrderingService>(client =>
{
    var baseUri = new Uri($"{apiGatewayBaseAddress}/ordering-service/");
    client.BaseAddress = baseUri;
});

// Register state management services
builder.Services.AddSingleton<BasketStateService>();
builder.Services.AddScoped<UserStateService>();

await builder.Build().RunAsync();
