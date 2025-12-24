using Discount.Grpc.Data;
using Discount.Grpc.Services; // Added for DiscountService
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Kestrel: Docker container içinde HTTP/2 (gRPC) ve HTTP/1.1 (health check) desteği
// NOT: Container içinde 0.0.0.0:8080 adresinde dinlemelidir
builder.WebHost.ConfigureKestrel(options =>
{
    // Port 8080: Docker container içinde kullanılan port
    // ListenAnyIP: Tüm network interface'lerde dinle (container network için gerekli)
    options.ListenAnyIP(8080, listenOptions =>
    {
        // Http1AndHttp2: Hem HTTP/1.1 (health check için) hem HTTP/2 (gRPC için) destekle
        // Container içinde TLS olmadan HTTP/2 cleartext (h2c) desteklenir
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
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

// Health Checks
app.MapHealthChecks("/health");

app.Run();
