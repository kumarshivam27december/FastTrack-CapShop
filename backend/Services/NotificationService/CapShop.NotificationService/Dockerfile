# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/NotificationService/CapShop.NotificationService/CapShop.NotificationService.csproj", "Services/NotificationService/CapShop.NotificationService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/NotificationService/CapShop.NotificationService/CapShop.NotificationService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/NotificationService/CapShop.NotificationService"
RUN dotnet build "CapShop.NotificationService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/NotificationService/CapShop.NotificationService/CapShop.NotificationService.csproj", "Services/NotificationService/CapShop.NotificationService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/NotificationService/CapShop.NotificationService/CapShop.NotificationService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/NotificationService/CapShop.NotificationService"
RUN dotnet publish "CapShop.NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5018
ENV ASPNETCORE_URLS=http://+:5018
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.NotificationService.dll"]
