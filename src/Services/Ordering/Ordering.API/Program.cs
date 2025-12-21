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

var app = builder.Build();

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
