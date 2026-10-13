
# ==========================================
# ETAPA 1: BUILD
# ==========================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

ARG BUILD_CONFIGURATION=Release

# Restaurar dependencias
COPY AdLocalAPI/AdLocalAPI.csproj AdLocalAPI/

RUN dotnet restore AdLocalAPI/AdLocalAPI.csproj

# Copiar código fuente
COPY AdLocalAPI/. AdLocalAPI/

WORKDIR /src/AdLocalAPI

# Compilar y publicar
RUN dotnet publish AdLocalAPI.csproj \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false


# ==========================================
# ETAPA 2: RUNTIME - PRODUCCIÓN
# ==========================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# ==========================================
# CONFIGURACIÓN DEL ENTORNO
# ==========================================

ENV ASPNETCORE_ENVIRONMENT=Production

# Puerto que utiliza Program.cs
ENV PORT=8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Configuración pública de JWT
ENV JWT__Issuer=AdLocalAPI
ENV JWT__Audience=AdLocal

# URL pública de Supabase
ENV SUPABASE__URL=https://uzgnfwbztoizcctyfdiv.supabase.co

# Las credenciales se suministran en el
# entorno de despliegue:
#
# DB_CONNECTION
# JWT__Key
# STRIPE_WEBHOOK_SECRET
# Stripe__SecretKey
# SUPABASE__KEY
# R2__AccountId
# R2__AccessKeyId
# R2__SecretAccessKey

# Copiar los archivos publicados
COPY --from=build /app/publish .

# Ejecutar con usuario sin privilegios
USER $APP_UID

# Puerto interno
EXPOSE 8080

# Ejecutar aplicación
ENTRYPOINT ["dotnet", "AdLocalAPI.dll"]
