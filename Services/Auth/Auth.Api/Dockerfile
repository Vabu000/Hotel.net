FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/Auth/Auth.Api/Auth.Api.csproj", "Services/Auth/Auth.Api/"]
COPY ["Platform.ServiceDefaults/HotelPlatform.ServiceDefaults.csproj", "Platform.ServiceDefaults/"]
RUN dotnet restore "Services/Auth/Auth.Api/Auth.Api.csproj"

COPY . .
WORKDIR /src/Services/Auth/Auth.Api
RUN dotnet publish "Auth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
