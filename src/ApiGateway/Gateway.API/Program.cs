var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health Checks
// Downstream servislerin health check'leri (opsiyonel, ayrı endpoint'te kullanılacak)
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://catalog.api:8080/health"), name: "catalog-api", timeout: TimeSpan.FromSeconds(15))
    .AddUrlGroup(new Uri("http://basket.api:8080/health"), name: "basket-api", timeout: TimeSpan.FromSeconds(15))
    .AddUrlGroup(new Uri("http://ordering.api:8080/health"), name: "ordering-api", timeout: TimeSpan.FromSeconds(15));

var app = builder.Build();

// OPTIONS preflight isteklerini handle et (YARP'dan önce)
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        context.Response.StatusCode = 204;
        return;
    }
    await next();
});

// CORS Middleware - YARP'dan önce
app.UseCors();

// Gateway'in kendi health check'i (Docker Compose için)
// Sadece Gateway'in çalışıp çalışmadığını kontrol eder
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Sadece Gateway'in kendisini kontrol et (downstream servisler kritik değil)
    Predicate = _ => false
});

// Downstream servislerin health check'leri (opsiyonel, monitoring için)
app.MapHealthChecks("/health/downstream", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Tüm downstream servislerin health check'lerini göster
    Predicate = check => check.Name.Contains("-api")
});

// YARP Middleware - Catch-all için en sonda olmalı
app.MapReverseProxy();

app.Run();
