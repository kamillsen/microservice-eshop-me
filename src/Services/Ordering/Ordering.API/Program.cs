using Ordering.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Ordering API",
        Version = "v1",
        Description = "E-ticaret Ordering Service API - Sipariş yönetimi için REST API"
    });
});

// PostgreSQL
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger UI (OpenAPI spesifikasyonunu görselleştirir)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering API v1");
        c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger'da göster (http://localhost:5003/swagger)
    });
}

app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
