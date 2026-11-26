FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Platform.Aggregator/HotelPlatform.Aggregator.csproj"
RUN dotnet build "Platform.Aggregator/HotelPlatform.Aggregator.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "Platform.Aggregator/HotelPlatform.Aggregator.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HotelPlatform.Aggregator.dll"]
