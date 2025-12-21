using Ordering.API.Data;
using Microsoft.EntityFrameworkCore;
using MediatR;
using BuildingBlocks.Behaviors.Behaviors;
using FluentValidation;
using BuildingBlocks.Exceptions.Handler;
using MassTransit;
using Ordering.API.EventHandlers;
using RabbitMQ.Client;

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

// MassTransit (Consumer)
builder.Services.AddMassTransit(config =>
{
    // Consumer'ı ekle
    config.AddConsumer<BasketCheckoutConsumer>();

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"]);

        // Endpoint'leri otomatik configure et
        cfg.ConfigureEndpoints(context);
    });
});

// PostgreSQL
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// RabbitMQ Connection Factory (Health Check için)
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var connectionString = builder.Configuration["MessageBroker:Host"]!;
    var uri = new Uri(connectionString);
    return new ConnectionFactory
    {
        Uri = uri,
        AutomaticRecoveryEnabled = true
    };
});

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRabbitMQ(); // DI container'dan IConnectionFactory'yi otomatik alır

var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await context.Database.MigrateAsync();
}

// Exception Handler Middleware
app.UseExceptionHandler();

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

// Health Checks
app.MapHealthChecks("/health");

app.Run();
