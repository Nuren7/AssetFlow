FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source

COPY ["src/AssetFlow.API/AssetFlow.API.csproj", "src/AssetFlow.API/"]
COPY ["src/AssetFlow.Application/AssetFlow.Application.csproj", "src/AssetFlow.Application/"]
COPY ["src/AssetFlow.Domain/AssetFlow.Domain.csproj", "src/AssetFlow.Domain/"]
COPY ["src/AssetFlow.Infrastructure/AssetFlow.Infrastructure.csproj", "src/AssetFlow.Infrastructure/"]
RUN dotnet restore "src/AssetFlow.API/AssetFlow.API.csproj"

COPY src/ src/
RUN dotnet publish "src/AssetFlow.API/AssetFlow.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet AssetFlow.API.dll"]
