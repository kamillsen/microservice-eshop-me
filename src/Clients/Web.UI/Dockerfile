# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["src/Clients/Web.UI/Web.UI.csproj", "src/Clients/Web.UI/"]

# Restore dependencies
RUN dotnet restore "src/Clients/Web.UI/Web.UI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Clients/Web.UI"
RUN dotnet build "Web.UI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Web.UI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime - nginx for static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy published files
COPY --from=publish /app/publish/wwwroot .

    # Copy nginx configuration
    COPY src/Clients/Web.UI/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

