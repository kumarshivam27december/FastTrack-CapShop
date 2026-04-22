# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/Services/AssistantService/CapShop.AssistantService/CapShop.AssistantService.csproj", "Services/AssistantService/CapShop.AssistantService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/AssistantService/CapShop.AssistantService/CapShop.AssistantService.csproj"

COPY ["backend/", "."]

WORKDIR "/src/Services/AssistantService/CapShop.AssistantService"
RUN dotnet build "CapShop.AssistantService.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY ["backend/Services/AssistantService/CapShop.AssistantService/CapShop.AssistantService.csproj", "Services/AssistantService/CapShop.AssistantService/"]
COPY ["backend/Shared/CapShop.Shared/CapShop.Shared.csproj", "Shared/CapShop.Shared/"]

RUN dotnet restore "Services/AssistantService/CapShop.AssistantService/CapShop.AssistantService.csproj"
COPY ["backend/", "."]

WORKDIR "/src/Services/AssistantService/CapShop.AssistantService"
RUN dotnet publish "CapShop.AssistantService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5019
ENV ASPNETCORE_URLS=http://+:5019
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CapShop.AssistantService.dll"]
