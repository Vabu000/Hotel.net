FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Platform.ApiGateway/HotelPlatform.ApiGateway.csproj", "Platform.ApiGateway/"]
COPY ["Platform.ServiceDefaults/HotelPlatform.ServiceDefaults.csproj", "Platform.ServiceDefaults/"]
RUN dotnet restore "Platform.ApiGateway/HotelPlatform.ApiGateway.csproj"

COPY . .
WORKDIR "/src/Platform.ApiGateway"
RUN dotnet build "HotelPlatform.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HotelPlatform.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelPlatform.ApiGateway.dll"]
