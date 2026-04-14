# Використовуємо офіційний образ .NET SDK для збірки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо файл проекту та відновлюємо залежності
COPY ["KhduSouvenirShop.API/KhduSouvenirShop.API.csproj", "KhduSouvenirShop.API/"]
RUN dotnet restore "KhduSouvenirShop.API/KhduSouvenirShop.API.csproj"

# Копіюємо решту файлів та збираємо проект
COPY . .
WORKDIR "/src/KhduSouvenirShop.API"
RUN dotnet build "KhduSouvenirShop.API.csproj" -c Release -o /app/build

# Публікуємо проект
FROM build AS publish
RUN dotnet publish "KhduSouvenirShop.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Використовуємо образ runtime для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KhduSouvenirShop.API.dll"]
