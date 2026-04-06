# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/OrderService/CapShop.OrderService/CapShop.OrderService.csproj", "Services/OrderService/CapShop.OrderService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/OrderService/CapShop.OrderService/CapShop.OrderService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/OrderService/CapShop.OrderService"
RUN dotnet build "CapShop.OrderService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/OrderService/CapShop.OrderService/CapShop.OrderService.csproj", "Services/OrderService/CapShop.OrderService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/OrderService/CapShop.OrderService/CapShop.OrderService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/OrderService/CapShop.OrderService"
RUN dotnet publish "CapShop.OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5015
ENV ASPNETCORE_URLS=http://+:5015
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.OrderService.dll"]
