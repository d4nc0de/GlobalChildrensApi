# Imagen base para correr la app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Puerto interno donde escuchará la API dentro del contenedor
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Imagen para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos primero el csproj y restauramos dependencias
COPY ["GlobalChildrensApi/GlobalChildrensApi.csproj", "GlobalChildrensApi/"]
RUN dotnet restore "GlobalChildrensApi/GlobalChildrensApi.csproj"

# Copiamos el resto del código
COPY . .
WORKDIR "/src/GlobalChildrensApi"

# Publicamos en modo Release
RUN dotnet publish "GlobalChildrensApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Punto de entrada
ENTRYPOINT ["dotnet", "GlobalChildrensApi.dll"]
