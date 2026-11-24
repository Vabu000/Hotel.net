# Етап 1: збірка (спершу лише .csproj -> restore кешується окремим шаром)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HotelCatalog.Api/HotelCatalog.Api.csproj", "HotelCatalog.Api/"]
RUN dotnet restore "HotelCatalog.Api/HotelCatalog.Api.csproj"
COPY . .
WORKDIR "/src/HotelCatalog.Api"
RUN dotnet build "HotelCatalog.Api.csproj" -c Release -o /app/build

# Етап 2: публікація
FROM build AS publish
RUN dotnet publish "HotelCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelCatalog.Api.dll"]