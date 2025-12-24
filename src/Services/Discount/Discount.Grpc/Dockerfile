# Build stage - Discount.Grpc için build aşaması
# Discount.Grpc, gRPC servisidir (REST API değil)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Solution seviyesi dosyalar
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Discount.Grpc proje dosyasını kopyala
# NOT: Discount.Grpc, BuildingBlocks projelerine bağımlı DEĞİL
# Bağımsız bir gRPC servisi (sadece gRPC ve EF Core kullanır)
COPY ["src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj", "src/Services/Discount/Discount.Grpc/"]

# NuGet paketlerini restore et
# Discount.Grpc sadece kendi paketlerini restore eder (BuildingBlocks yok)
RUN dotnet restore "src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj"

# Tüm kaynak kodları kopyala
COPY . .

# Discount.Grpc klasörüne git
WORKDIR "/src/src/Services/Discount/Discount.Grpc"

# Release modunda derle
RUN dotnet build "Discount.Grpc.csproj" -c Release -o /app/build

# Publish et
RUN dotnet publish "Discount.Grpc.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Publish edilmiş dosyaları kopyala
COPY --from=build /app/publish .

# Container portu (gRPC servisleri de HTTP/2 üzerinden çalışır)
EXPOSE 8080

# ASP.NET Core port ayarı
ENV ASPNETCORE_URLS=http://+:8080

# Container başladığında Discount.Grpc'yi çalıştır
ENTRYPOINT ["dotnet", "Discount.Grpc.dll"]

