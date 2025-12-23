using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// YARP Middleware
app.MapReverseProxy();

app.Run();
