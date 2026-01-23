using Basket.API.Data;
using Basket.API.GrpcServices;
using BuildingBlocks.Behaviors.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using Discount.Grpc.Protos;
using FluentValidation;
using Grpc.Net.Client;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

// PostgreSQL
builder.Services.AddDbContext<BasketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration!);
});

// Repository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// gRPC Client - HTTP/2 cleartext (h2c) desteği için
// Docker container içinde TLS olmadan HTTP/2 kullanımı için gerekli
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddSingleton<DiscountProtoService.DiscountProtoServiceClient>(sp =>
{
    var address = builder.Configuration["GrpcSettings:DiscountUrl"]!;
    
    // HTTP/2 cleartext (h2c) desteği için SocketsHttpHandler kullan
    // HttpClientHandler HTTP/2 cleartext'i desteklemez
    var socketsHandler = new System.Net.Http.SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true
    };
    
    var httpClient = new HttpClient(socketsHandler)
    {
        DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher,
        DefaultRequestVersion = System.Net.HttpVersion.Version20
    };
    
    var channelOptions = new GrpcChannelOptions
    {
        HttpClient = httpClient,
        // HTTP/2 cleartext (h2c) için Insecure credentials kullan
        Credentials = Grpc.Core.ChannelCredentials.Insecure
    };
    
    var channel = GrpcChannel.ForAddress(address, channelOptions);
    return new DiscountProtoService.DiscountProtoServiceClient(channel);
});

// DiscountGrpcService
builder.Services.AddScoped<DiscountGrpcService>();

// MassTransit
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"]);
    });
});

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        timeout: TimeSpan.FromSeconds(10),
        tags: new[] { "redis", "ready" })
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    await context.Database.MigrateAsync();
}

// Exception Handler Middleware
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Developer Exception Page (detaylı hata mesajları için)
    app.UseDeveloperExceptionPage();
    
    // Swagger UI (OpenAPI spesifikasyonunu görselleştirir)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket API v1");
        c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger'da göster
    });
}

// HTTPS redirection kaldırıldı - Container içinde HTTP kullanılıyor
// app.UseHttpsRedirection();

// Controllers
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health");

app.Run();
