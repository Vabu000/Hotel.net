FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Services/Reviews/Reviews.WebApi/HotelReviews.WebApi.csproj"
RUN dotnet build "Services/Reviews/Reviews.WebApi/HotelReviews.WebApi.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "Services/Reviews/Reviews.WebApi/HotelReviews.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelReviews.WebApi.dll"]
