using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health Checks (Downstream Services)
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5001/health"), name: "catalog-api")
    .AddUrlGroup(new Uri("http://localhost:5278/health"), name: "basket-api")
    .AddUrlGroup(new Uri("http://localhost:5103/health"), name: "ordering-api");

var app = builder.Build();

// Health Check Endpoint
app.MapHealthChecks("/health");

// YARP Middleware
app.MapReverseProxy();

app.Run();
