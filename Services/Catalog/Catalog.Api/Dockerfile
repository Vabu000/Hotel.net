FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj", "Services/Catalog/Catalog.Api/"]
COPY ["Services/Catalog/Catalog.Bll/HotelCatalog.Bll.csproj", "Services/Catalog/Catalog.Bll/"]
COPY ["Services/Catalog/Catalog.Dal/HotelCatalog.Dal.csproj", "Services/Catalog/Catalog.Dal/"]
COPY ["Services/Catalog/Catalog.Domain/HotelCatalog.Domain.csproj", "Services/Catalog/Catalog.Domain/"]
COPY ["Platform.ServiceDefaults/HotelPlatform.ServiceDefaults.csproj", "Platform.ServiceDefaults/"]
COPY ["Platform.EventBus/Platform.EventBus.csproj", "Platform.EventBus/"]
RUN dotnet restore "Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj"

COPY . .
WORKDIR "/src/Services/Catalog/Catalog.Api"
RUN dotnet build "HotelCatalog.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HotelCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelCatalog.Api.dll"]
