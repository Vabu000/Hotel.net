FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj", "Services/Catalog/Catalog.Api/"]
COPY ["Services/Catalog/Catalog.Bll/HotelCatalog.Bll.csproj", "Services/Catalog/Catalog.Bll/"]
COPY ["Services/Catalog/Catalog.Dal/HotelCatalog.Dal.csproj", "Services/Catalog/Catalog.Dal/"]
COPY ["Services/Catalog/Catalog.Domain/HotelCatalog.Domain.csproj", "Services/Catalog/Catalog.Domain/"]
RUN dotnet restore "Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj"

COPY . .
RUN dotnet publish "Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HotelCatalog.Api.dll"]
