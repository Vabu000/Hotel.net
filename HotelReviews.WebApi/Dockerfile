# Етап 1: збірка (спершу лише .csproj -> restore кешується окремим шаром)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HotelReviews.WebApi/HotelReviews.WebApi.csproj", "HotelReviews.WebApi/"]
RUN dotnet restore "HotelReviews.WebApi/HotelReviews.WebApi.csproj"
COPY . .
WORKDIR "/src/HotelReviews.WebApi"
RUN dotnet build "HotelReviews.WebApi.csproj" -c Release -o /app/build

# Етап 2: публікація
FROM build AS publish
RUN dotnet publish "HotelReviews.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelReviews.WebApi.dll"]