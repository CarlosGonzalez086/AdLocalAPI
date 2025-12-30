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
ENV SUPABASE_DB_CONNECTION="User Id=postgres.uzgnfwbztoizcctyfdiv;Password=q8dZ1szsEYIOzKrM;Server=aws-1-us-east-2.pooler.supabase.com;Port=6543;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"

# JWT
ENV JWT__Key=AdLocal_SUPER_SECRET_KEY_PROD_2025_256_BITS
ENV JWT__Issuer=AdLocalAPI

# ===============================

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AdLocalAPI.dll"]
