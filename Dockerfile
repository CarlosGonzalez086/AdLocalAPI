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
ENV SUPABASE_DB_CONNECTION="Host=aws-1-us-east-2.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.uzgnfwbztoizcctyfdiv;Password=q8dZ1szsEYIOzKrM;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=5;Timeout=60;Command Timeout=60;Keepalive=30;"


# JWT
ENV JWT__Key=AdLocal_SUPER_SECRET_KEY_PROD_2025_256_BITS
ENV JWT__Issuer=AdLocalAPI

# Supabase
ENV SUPABASE__URL="https://uzgnfwbztoizcctyfdiv.supabase.co"
ENV SUPABASE__KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV6Z25md2J6dG9pemNjdHlmZGl2Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2Njk0MzUyNywiZXhwIjoyMDgyNTE5NTI3fQ.opjCm_q7U9GX0ah7UUgRMzQJwBQhyBupWVGJQXY6v0I"

# ===============================

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AdLocalAPI.dll"]
