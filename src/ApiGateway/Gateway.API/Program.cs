var builder = WebApplication.CreateBuilder(args);

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

// YARP Middleware
app.MapReverseProxy();

app.Run();
