# Етап 1: збірка (спершу лише .csproj -> restore кешується окремим шаром)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HotelReservationSystem/HotelReservation.Api.csproj", "HotelReservationSystem/"]
RUN dotnet restore "HotelReservationSystem/HotelReservation.Api.csproj"
COPY . .
WORKDIR "/src/HotelReservationSystem"
RUN dotnet build "HotelReservation.Api.csproj" -c Release -o /app/build

# Етап 2: публікація
FROM build AS publish
RUN dotnet publish "HotelReservation.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelReservation.Api.dll"]