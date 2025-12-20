using Basket.API.Data;
using Basket.API.GrpcServices;
using BuildingBlocks.Behaviors.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using Discount.Grpc.Protos;
using FluentValidation;
using Grpc.Net.Client;
using MassTransit;
using MediatR;
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

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

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

// MassTransit
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

// Exception Handler Middleware
app.UseExceptionHandler();

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

// Health Checks
app.MapHealthChecks("/health");

app.Run();
