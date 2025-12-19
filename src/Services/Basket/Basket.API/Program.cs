using Basket.API.Data;
using Basket.API.GrpcServices;
using Discount.Grpc.Protos;
using Grpc.Net.Client;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Basket API",
        Version = "v1",
        Description = "E-ticaret Basket Service API - Sepet yönetimi için REST API"
    });
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration!);
});

// Repository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// gRPC Client
builder.Services.AddSingleton<DiscountProtoService.DiscountProtoServiceClient>(sp =>
{
    var address = builder.Configuration["GrpcSettings:DiscountUrl"]!;
    var channel = GrpcChannel.ForAddress(address);
    return new DiscountProtoService.DiscountProtoServiceClient(channel);
});

// DiscountGrpcService
builder.Services.AddScoped<DiscountGrpcService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger UI (OpenAPI spesifikasyonunu görselleştirir)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket API v1");
        c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger'da göster
    });
}

app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
