# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Locatic/Locatic.csproj Locatic/
RUN dotnet restore Locatic/Locatic.csproj

COPY Locatic/ Locatic/
RUN dotnet publish Locatic/Locatic.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app

# curl est nécessaire au HEALTHCHECK, absent de l'image de base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r locatic && useradd -r -g locatic locatic

# Chemin dédié au volume persistant SQLite (voir infra/kubernetes plus tard)
RUN mkdir -p /data && chown -R locatic:locatic /data

COPY --from=build /app/publish .
RUN chown -R locatic:locatic /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/agence.db"

USER locatic
EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/ || exit 1

ENTRYPOINT ["dotnet", "Locatic.dll"]
