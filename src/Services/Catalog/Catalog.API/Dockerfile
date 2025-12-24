# Build stage - Bu stage'de projeyi derleyeceğiz
# mcr.microsoft.com/dotnet/sdk:9.0 = .NET 9 SDK içeren Docker image
# SDK image'i büyüktür (derleme için gerekli araçlar var) ama sadece build sırasında kullanılır
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# WORKDIR = Container içinde çalışma dizinini ayarlar
# /src = Container içinde tüm kaynak kodların kopyalanacağı klasör
WORKDIR /src

# COPY komutu = Host makineden container'a dosya kopyalar
# Bu satırlar solution root'tan (.) Dockerfile'ın olduğu yere dosyaları kopyalar
# Directory.Build.props ve Directory.Packages.props = NuGet paket versiyonlarını merkezi yönetmek için
# global.json = .NET SDK versiyonunu belirler
# Format: COPY [kaynak, hedef] - köşeli parantez kullanmak pattern matching için
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Catalog.API proje dosyasını kopyala
# .csproj dosyası NuGet paketlerini restore etmek için gerekli
COPY ["src/Services/Catalog/Catalog.API/Catalog.API.csproj", "src/Services/Catalog/Catalog.API/"]

# BuildingBlocks projelerini kopyala (Catalog.API bu projelere bağımlı)
# Catalog.API, BuildingBlocks.Exceptions ve BuildingBlocks.Behaviors'ı kullanır
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]

# dotnet restore = NuGet paketlerini indirir ve restore eder
# Bu işlem sadece .csproj dosyalarına bakarak paketleri indirir (Layer caching için önemli)
# Layer caching = Docker build sırasında değişmeyen layer'lar cache'den kullanılır (hız artışı)
RUN dotnet restore "src/Services/Catalog/Catalog.API/Catalog.API.csproj"

# COPY . . = Tüm kaynak kodları kopyala (solution root'tan container'a)
# Bu komut restore'dan SONRA gelir çünkü kaynak kodlar sık değişir
# Eğer önce kaynak kodları kopyalarsak, her kod değişikliğinde restore da yeniden çalışır (yavaş)
COPY . .

# WORKDIR değiştir = Catalog.API klasörüne git
# Bu klasörde build komutunu çalıştıracağız
WORKDIR "/src/src/Services/Catalog/Catalog.API"

# dotnet build = Projeyi derler (Debug veya Release modunda)
# -c Release = Release modunda derle (optimize edilmiş, production için)
# -o /app/build = Derlenmiş dosyaları /app/build klasörüne kopyala
RUN dotnet build "Catalog.API.csproj" -c Release -o /app/build

# dotnet publish = Projeyi publish eder (çalıştırılabilir hale getirir)
# Publish işlemi sadece gerekli dosyaları içerir (daha küçük)
# /p:UseAppHost=false = App host oluşturma (container'da gerekmez, dotnet komutu ile çalıştıracağız)
RUN dotnet publish "Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Bu stage'de sadece runtime ve publish edilmiş dosyalar olacak
# mcr.microsoft.com/dotnet/aspnet:9.0 = .NET 9 runtime (sadece çalıştırma için)
# Bu image SDK'dan ÇOK daha küçüktür (sadece runtime, derleme araçları yok)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# WORKDIR = Final stage'de çalışma dizini
WORKDIR /app

# COPY --from=build = Build stage'den dosya kopyala
# Sadece publish edilmiş dosyaları kopyalıyoruz (kaynak kodları değil)
# Bu sayede final image küçük kalır
COPY --from=build /app/publish .

# wget kurulumu (health check için gerekli)
# Docker health check'leri için wget kullanılacak
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*

# EXPOSE = Container'ın hangi portu dinleyeceğini belirtir (dokümantasyon amaçlı)
# 8080 = Internal container port (Docker Compose'da map edeceğiz)
EXPOSE 8080

# ENV = Environment variable ayarlar
# ASPNETCORE_URLS = ASP.NET Core'un hangi URL'leri dinleyeceğini belirtir
# http://+:8080 = Tüm network interface'lerinde 8080 portunu dinle
# + işareti = 0.0.0.0 anlamına gelir (tüm IP adresleri)
ENV ASPNETCORE_URLS=http://+:8080

# ENTRYPOINT = Container başladığında çalışacak komut
# ["dotnet", "Catalog.API.dll"] = dotnet komutu ile Catalog.API.dll dosyasını çalıştır
# Bu komut container başladığında web server'ı başlatır
ENTRYPOINT ["dotnet", "Catalog.API.dll"]

