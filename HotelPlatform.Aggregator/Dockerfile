# Етап 1: збірка (спершу лише .csproj -> restore кешується окремим шаром)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HotelPlatform.Aggregator/HotelPlatform.Aggregator.csproj", "HotelPlatform.Aggregator/"]
RUN dotnet restore "HotelPlatform.Aggregator/HotelPlatform.Aggregator.csproj"
COPY . .
WORKDIR "/src/HotelPlatform.Aggregator"
RUN dotnet build "HotelPlatform.Aggregator.csproj" -c Release -o /app/build

# Етап 2: публікація
FROM build AS publish
RUN dotnet publish "HotelPlatform.Aggregator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelPlatform.Aggregator.dll"]