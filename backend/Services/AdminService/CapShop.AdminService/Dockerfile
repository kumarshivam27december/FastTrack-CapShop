# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/AdminService/CapShop.AdminService/CapShop.AdminService.csproj", "Services/AdminService/CapShop.AdminService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/AdminService/CapShop.AdminService/CapShop.AdminService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/AdminService/CapShop.AdminService"
RUN dotnet build "CapShop.AdminService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/AdminService/CapShop.AdminService/CapShop.AdminService.csproj", "Services/AdminService/CapShop.AdminService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/AdminService/CapShop.AdminService/CapShop.AdminService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/AdminService/CapShop.AdminService"
RUN dotnet publish "CapShop.AdminService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5016
ENV ASPNETCORE_URLS=http://+:5016
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.AdminService.dll"]
