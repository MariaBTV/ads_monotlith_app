# Dockerfile for RetailMonolith.Checkout.Api
# Multi-stage build for optimised container size

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5100
EXPOSE 5101

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RetailMonolith.Checkout.Api/RetailMonolith.Checkout.Api.csproj", "RetailMonolith.Checkout.Api/"]
RUN dotnet restore "RetailMonolith.Checkout.Api/RetailMonolith.Checkout.Api.csproj"
COPY . .
WORKDIR "/src/RetailMonolith.Checkout.Api"
RUN dotnet build "RetailMonolith.Checkout.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RetailMonolith.Checkout.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RetailMonolith.Checkout.Api.dll"]
