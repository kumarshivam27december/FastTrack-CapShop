# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/AuthService/CapShop.AuthService/CapShop.AuthService.csproj", "Services/AuthService/CapShop.AuthService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/AuthService/CapShop.AuthService/CapShop.AuthService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/AuthService/CapShop.AuthService"
RUN dotnet build "CapShop.AuthService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/AuthService/CapShop.AuthService/CapShop.AuthService.csproj", "Services/AuthService/CapShop.AuthService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/AuthService/CapShop.AuthService/CapShop.AuthService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/AuthService/CapShop.AuthService"
RUN dotnet publish "CapShop.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5013
ENV ASPNETCORE_URLS=http://+:5013
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.AuthService.dll"]
