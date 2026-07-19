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

# UID/GID fixes (999) pour runAsNonRoot Kubernetes (noms seuls ne suffisent pas).
RUN groupadd -r -g 999 locatic && useradd -r -u 999 -g locatic locatic

# Chemin dédié au volume persistant SQLite
RUN mkdir -p /data && chown -R locatic:locatic /data

COPY --from=build /app/publish .
RUN chown -R locatic:locatic /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/agence.db"

USER 999:999
EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "Locatic.dll"]
