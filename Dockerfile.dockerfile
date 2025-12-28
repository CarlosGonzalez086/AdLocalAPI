# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY AdLocalAPI/AdLocalAPI.csproj AdLocalAPI/
RUN dotnet restore AdLocalAPI/AdLocalAPI.csproj

# Copiar todo el proyecto
COPY AdLocalAPI/. AdLocalAPI/
WORKDIR /src/AdLocalAPI

# Compilar
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AdLocalAPI.dll"]
