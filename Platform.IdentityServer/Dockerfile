FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Platform.IdentityServer/Platform.IdentityServer.csproj", "Platform.IdentityServer/"]
COPY ["Platform.ServiceDefaults/HotelPlatform.ServiceDefaults.csproj", "Platform.ServiceDefaults/"]
RUN dotnet restore "Platform.IdentityServer/Platform.IdentityServer.csproj"

COPY . .
WORKDIR /src/Platform.IdentityServer
RUN dotnet publish "Platform.IdentityServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Platform.IdentityServer.dll"]
