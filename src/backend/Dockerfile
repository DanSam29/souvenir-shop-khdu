# Backend Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KhduSouvenirShop.API.csproj", "./"]
RUN dotnet restore "./KhduSouvenirShop.API.csproj"
COPY . .
RUN dotnet build "KhduSouvenirShop.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KhduSouvenirShop.API.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KhduSouvenirShop.API.dll"]
