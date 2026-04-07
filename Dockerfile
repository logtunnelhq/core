# syntax=docker/dockerfile:1.7

# ---------- Build stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# global.json is excluded via .dockerignore — see the comment there.
# The image's bundled .NET 8 SDK is used as-is.
#
# Copy project manifests first so 'dotnet restore' is cached on its own
# Docker layer and only re-runs when a csproj changes.
COPY src/Layered.Core/Layered.Core.csproj src/Layered.Core/
COPY src/Layered.Api/Layered.Api.csproj src/Layered.Api/

RUN dotnet restore src/Layered.Api/Layered.Api.csproj

# Copy the rest of the build context. .dockerignore filters out bin/,
# obj/, .git/, IDE files, the Cli/Tests projects we do not need at
# runtime, etc.
COPY . .

RUN dotnet publish src/Layered.Api/Layered.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- Runtime stage -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# All other configuration is supplied at run time via environment
# variables (LLM__PROVIDER, LLM__APIKEY, LLM__MODEL, LLM__BASEURL,
# Layered__Config__OutputDirectory, ASPNETCORE_ENVIRONMENT, ...).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Layered.Api.dll"]
