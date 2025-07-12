# Βασική εικόνα για το .NET Core Runtime (εκτέλεση της εφαρμογής)
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Εικόνα για την κατασκευή του project (SDK)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService/OrderService.csproj"
COPY . .
WORKDIR "/src/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

# Βήμα δημοσίευσης (publish)
FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish

# Δημιουργία τελικής εικόνας
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
