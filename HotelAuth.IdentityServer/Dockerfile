# Етап 1: збірка (спершу лише .csproj -> restore кешується окремим шаром)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HotelAuth.IdentityServer/HotelAuth.IdentityServer.csproj", "HotelAuth.IdentityServer/"]
RUN dotnet restore "HotelAuth.IdentityServer/HotelAuth.IdentityServer.csproj"
COPY . .
WORKDIR "/src/HotelAuth.IdentityServer"
RUN dotnet build "HotelAuth.IdentityServer.csproj" -c Release -o /app/build

# Етап 2: публікація
FROM build AS publish
RUN dotnet publish "HotelAuth.IdentityServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelAuth.IdentityServer.dll"]