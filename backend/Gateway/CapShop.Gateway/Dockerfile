# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ["backend/Gateway/CapShop.Gateway/CapShop.Gateway.csproj", "Gateway/CapShop.Gateway/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Gateway/CapShop.Gateway/CapShop.Gateway.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build the application
WORKDIR "/src/Gateway/CapShop.Gateway"
RUN dotnet build "CapShop.Gateway.csproj" -c Release -o /app/build

# Stage 2: Publish stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Gateway/CapShop.Gateway/CapShop.Gateway.csproj", "Gateway/CapShop.Gateway/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Gateway/CapShop.Gateway/CapShop.Gateway.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Gateway/CapShop.Gateway"
RUN dotnet publish "CapShop.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

# Expose port 5041 (Gateway default port)
EXPOSE 5041

ENV ASPNETCORE_URLS=http://+:5041
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.Gateway.dll"]
