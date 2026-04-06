# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/Services/PaymentService/CapShop.PaymentService/CapShop.PaymentService.csproj", "Services/PaymentService/CapShop.PaymentService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/PaymentService/CapShop.PaymentService/CapShop.PaymentService.csproj"

# Copy entire source
COPY ["backend/", "."]

# Build
WORKDIR "/src/Services/PaymentService/CapShop.PaymentService"
RUN dotnet build "CapShop.PaymentService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/PaymentService/CapShop.PaymentService/CapShop.PaymentService.csproj", "Services/PaymentService/CapShop.PaymentService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/PaymentService/CapShop.PaymentService/CapShop.PaymentService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/PaymentService/CapShop.PaymentService"
RUN dotnet publish "CapShop.PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5017
ENV ASPNETCORE_URLS=http://+:5017
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.PaymentService.dll"]
