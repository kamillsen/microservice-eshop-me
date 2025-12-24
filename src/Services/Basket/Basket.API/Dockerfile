# Build stage - Basket.API için build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Solution seviyesi dosyalar (paket yönetimi için)
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Basket.API proje dosyasını kopyala
COPY ["src/Services/Basket/Basket.API/Basket.API.csproj", "src/Services/Basket/Basket.API/"]

# BuildingBlocks projelerini kopyala
# Basket.API, 3 BuildingBlocks projesine bağımlı:
# - BuildingBlocks.Exceptions: Exception handling için
# - BuildingBlocks.Behaviors: MediatR pipeline behaviors (Validation, Logging) için
# - BuildingBlocks.Messaging: RabbitMQ event'leri için (BasketCheckoutEvent)
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/BuildingBlocks.Messaging/"]

# NuGet paketlerini restore et (BuildingBlocks.Messaging dahil)
RUN dotnet restore "src/Services/Basket/Basket.API/Basket.API.csproj"

# Tüm kaynak kodları kopyala (restore'dan sonra, layer caching için)
COPY . .

# Basket.API klasörüne git ve build et
WORKDIR "/src/src/Services/Basket/Basket.API"

# Release modunda derle
RUN dotnet build "Basket.API.csproj" -c Release -o /app/build

# Publish et (çalıştırılabilir dosyaları hazırla)
RUN dotnet publish "Basket.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Runtime image (küçük ve hafif)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Publish edilmiş dosyaları kopyala (kaynak kodlar değil, sadece çalıştırılabilir dosyalar)
COPY --from=build /app/publish .

# Container portu
EXPOSE 8080

# ASP.NET Core'un 8080 portunu dinlemesini sağla
ENV ASPNETCORE_URLS=http://+:8080

# Container başladığında Basket.API'yi çalıştır
ENTRYPOINT ["dotnet", "Basket.API.dll"]

