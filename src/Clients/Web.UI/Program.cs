using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
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

// Register Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

await builder.Build().RunAsync();
