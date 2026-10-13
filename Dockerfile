# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AdLocalAPI/AdLocalAPI.csproj AdLocalAPI/
RUN dotnet restore AdLocalAPI/AdLocalAPI.csproj

COPY AdLocalAPI/. AdLocalAPI/
WORKDIR /src/AdLocalAPI

RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# ===============================
# VARIABLES DE ENTORNO
# ===============================

ENV ASPNETCORE_URLS=http://+:8080
ENV AllowedHosts=*

# Connection String (EF Core)





# JWT
ENV JWT__Issuer=AdLocalAPI

# Supabase
ENV SUPABASE__URL="https://uzgnfwbztoizcctyfdiv.supabase.co"

# ===============================

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AdLocalAPI.dll"]
