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
ENV SUPABASE_DB_CONNECTION=Host=ep-empty-moon-adsd2mcc-pooler.c-2.us-east-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_vXt9sekfG3rY;SslMode=Require
ENV STRIPE_WEBHOOK_SECRET=whsec_vRaXms0x16TSCd2LYEAEDGBdBtmuT1tz
ENV SecretKey=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





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
