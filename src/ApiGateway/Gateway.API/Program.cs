using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health Checks (Downstream Services)
// Container network içinde servis adlarını kullan
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://catalog.api:8080/health"), name: "catalog-api")
    .AddUrlGroup(new Uri("http://basket.api:8080/health"), name: "basket-api")
    .AddUrlGroup(new Uri("http://ordering.api:8080/health"), name: "ordering-api");

var app = builder.Build();

// Health Check Endpoint
app.MapHealthChecks("/health");

// YARP Middleware
app.MapReverseProxy();

app.Run();
