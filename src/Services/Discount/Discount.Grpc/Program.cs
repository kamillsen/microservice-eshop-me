using Discount.Grpc.Data;
using Discount.Grpc.Services; // Added for DiscountService
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// HTTPS'i devre dışı bırak - Container içinde HTTP kullan
builder.WebHost.UseUrls("http://*:8080");

// Kestrel: Docker container içinde HTTP/2 (gRPC) ve HTTP/1.1 (health check) desteği
// NOT: Container içinde 0.0.0.0:8080 adresinde dinlemelidir
builder.WebHost.ConfigureKestrel(options =>
{
    // Port 8080: HTTP/2 ONLY - gRPC için (Prior Knowledge mode)
    // HTTP/2 cleartext (h2c) için sadece Http2 protokolü kullanılmalı
    options.ListenAnyIP(8080, listenOptions =>
    {
        // Http2: Sadece HTTP/2 (gRPC için) - Prior Knowledge mode
        // Bu, istemcinin bağlantıyı başlatırken doğrudan HTTP/2 kullanacağını varsayar
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
    
    // Port 8081: HTTP/1.1 ONLY - Health check için
    // Health check HTTP/1.1 gerektirir, bu yüzden ayrı port kullanıyoruz
    options.ListenAnyIP(8081, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

// DbContext
builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Migration ve Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
    
    // 1. Migration uygula (tabloları oluştur)
    await context.Database.MigrateAsync();
    
    // 2. Seed data ekle (eğer boşsa)
    await SeedData.InitializeAsync(context);
}

       // Configure the HTTP request pipeline.
       // gRPC servisini map et
       app.MapGrpcService<DiscountService>();
       
       app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Health Checks - HTTP/1.1 portunda (8081) çalışır
// Health check endpoint'i HTTP/1.1 portunda (8081) erişilebilir
// HTTP/2 portunda (8080) sadece gRPC çalışır
app.MapHealthChecks("/health");

app.Run();
