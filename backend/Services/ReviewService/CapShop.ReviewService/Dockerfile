# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/Services/ReviewService/CapShop.ReviewService/CapShop.ReviewService.csproj", "Services/ReviewService/CapShop.ReviewService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/ReviewService/CapShop.ReviewService/CapShop.ReviewService.csproj"

COPY ["backend/", "."]

WORKDIR "/src/Services/ReviewService/CapShop.ReviewService"
RUN dotnet build "CapShop.ReviewService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src

COPY ["backend/Services/ReviewService/CapShop.ReviewService/CapShop.ReviewService.csproj", "Services/ReviewService/CapShop.ReviewService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/ReviewService/CapShop.ReviewService/CapShop.ReviewService.csproj"

COPY ["backend/", "."]

WORKDIR "/src/Services/ReviewService/CapShop.ReviewService"
RUN dotnet publish "CapShop.ReviewService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5020
ENV ASPNETCORE_URLS=http://+:5020
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.ReviewService.dll"]
