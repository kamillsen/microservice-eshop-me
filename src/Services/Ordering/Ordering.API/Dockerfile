# Build stage - Ordering.API için build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Solution seviyesi dosyalar
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Ordering.API proje dosyasını kopyala
COPY ["src/Services/Ordering/Ordering.API/Ordering.API.csproj", "src/Services/Ordering/Ordering.API/"]

# BuildingBlocks projelerini kopyala
# Ordering.API, 3 BuildingBlocks projesine bağımlı:
# - BuildingBlocks.Exceptions: Exception handling için
# - BuildingBlocks.Behaviors: MediatR pipeline behaviors için
# - BuildingBlocks.Messaging: RabbitMQ Consumer için (BasketCheckoutEvent'i dinler)
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/BuildingBlocks.Messaging/"]

# NuGet paketlerini restore et
RUN dotnet restore "src/Services/Ordering/Ordering.API/Ordering.API.csproj"

# Tüm kaynak kodları kopyala
COPY . .

# Ordering.API klasörüne git
WORKDIR "/src/src/Services/Ordering/Ordering.API"

# Release modunda derle
RUN dotnet build "Ordering.API.csproj" -c Release -o /app/build

# Publish et
RUN dotnet publish "Ordering.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Publish edilmiş dosyaları kopyala
COPY --from=build /app/publish .

# wget kurulumu (health check için gerekli)
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*

# Container portu
EXPOSE 8080

# ASP.NET Core port ayarı
ENV ASPNETCORE_URLS=http://+:8080

# Container başladığında Ordering.API'yi çalıştır
ENTRYPOINT ["dotnet", "Ordering.API.dll"]

