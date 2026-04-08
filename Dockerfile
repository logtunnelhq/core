# syntax=docker/dockerfile:1.7

# ---------- Build stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# global.json is excluded via .dockerignore — see the comment there.
# The image's bundled .NET 8 SDK is used as-is.
#
# Copy project manifests first so 'dotnet restore' is cached on its own
# Docker layer and only re-runs when a csproj changes. Infrastructure is
# restored alongside Api so the migrations stage below has everything it
# needs without re-restoring.
COPY src/LogTunnel.Core/LogTunnel.Core.csproj src/LogTunnel.Core/
COPY src/LogTunnel.Api/LogTunnel.Api.csproj src/LogTunnel.Api/
COPY src/LogTunnel.Infrastructure/LogTunnel.Infrastructure.csproj src/LogTunnel.Infrastructure/

RUN dotnet restore src/LogTunnel.Api/LogTunnel.Api.csproj && \
    dotnet restore src/LogTunnel.Infrastructure/LogTunnel.Infrastructure.csproj

# Copy the rest of the build context. .dockerignore filters out bin/,
# obj/, .git/, IDE files, the Cli/Tests projects we do not need at
# runtime, etc.
COPY . .

RUN dotnet publish src/LogTunnel.Api/LogTunnel.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- Migrations stage ----------------------------------------------
# A one-shot image that applies the LogTunnel.Infrastructure EF migrations
# against an externally-supplied connection string and then exits. Used
# by the 'migrations' service in docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS migrations
WORKDIR /repo

# Pull the restored source from the build stage so we don't pay for the
# restore twice.
COPY --from=build /repo /repo

# Install the matching dotnet-ef CLI as a local tool.
RUN dotnet tool install --tool-path /tools dotnet-ef --version 8.0.10
ENV PATH="/tools:${PATH}"

# CONNECTION_STRING is injected at run time via the compose file.
ENTRYPOINT ["sh", "-c", "dotnet ef database update --project src/LogTunnel.Infrastructure/LogTunnel.Infrastructure.csproj --connection \"$CONNECTION_STRING\""]

# ---------- Runtime stage -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# All other configuration is supplied at run time via environment
# variables (LLM__PROVIDER, LLM__APIKEY, LLM__MODEL, LLM__BASEURL,
# LogTunnel__Config__OutputDirectory, Postgres__ConnectionString,
# ASPNETCORE_ENVIRONMENT, ...).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LogTunnel.Api.dll"]
