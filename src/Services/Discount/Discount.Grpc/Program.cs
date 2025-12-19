using Discount.Grpc.Data;
using Discount.Grpc.Services; // Added for DiscountService
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
