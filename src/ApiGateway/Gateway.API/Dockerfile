# Build stage - Gateway.API için build aşaması
# Gateway.API, YARP (Yet Another Reverse Proxy) kullanan API Gateway servisidir
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Solution seviyesi dosyalar
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Gateway.API proje dosyasını kopyala
# NOT: Gateway.API, BuildingBlocks projelerine bağımlı DEĞİL
# Sadece YARP (reverse proxy) kullanır, exception handling vs. yok
COPY ["src/ApiGateway/Gateway.API/Gateway.API.csproj", "src/ApiGateway/Gateway.API/"]

# NuGet paketlerini restore et
# Gateway.API sadece YARP ve Health Checks paketlerini kullanır
RUN dotnet restore "src/ApiGateway/Gateway.API/Gateway.API.csproj"

# Tüm kaynak kodları kopyala
COPY . .

# Gateway.API klasörüne git
WORKDIR "/src/src/ApiGateway/Gateway.API"

# Release modunda derle
RUN dotnet build "Gateway.API.csproj" -c Release -o /app/build

# Publish et
RUN dotnet publish "Gateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
# Gateway.API, tüm servislere giriş noktasıdır (port 5000'den erişilir, container içinde 8080)
EXPOSE 8080

# ASP.NET Core port ayarı
ENV ASPNETCORE_URLS=http://+:8080

# Container başladığında Gateway.API'yi çalıştır
ENTRYPOINT ["dotnet", "Gateway.API.dll"]

