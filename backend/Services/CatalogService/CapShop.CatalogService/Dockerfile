# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/CatalogService/CapShop.CatalogService/CapShop.CatalogService.csproj", "Services/CatalogService/CapShop.CatalogService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/CatalogService/CapShop.CatalogService/CapShop.CatalogService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/CatalogService/CapShop.CatalogService"
RUN dotnet build "CapShop.CatalogService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/CatalogService/CapShop.CatalogService/CapShop.CatalogService.csproj", "Services/CatalogService/CapShop.CatalogService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/CatalogService/CapShop.CatalogService/CapShop.CatalogService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/CatalogService/CapShop.CatalogService"
RUN dotnet publish "CapShop.CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5014
ENV ASPNETCORE_URLS=http://+:5014
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.CatalogService.dll"]
