FROM node:24-alpine AS web-build
WORKDIR /web
COPY web/package*.json ./
RUN npm ci
COPY web/ ./
# Configurable public tile style without secrets. Override at build time with
# --build-arg VITE_TILE_URL=... and VITE_TILE_ATTRIBUTION=...
ARG VITE_TILE_URL=https://demotiles.maplibre.org/style.json
ARG VITE_TILE_ATTRIBUTION=© OpenStreetMap contributors © MapLibre demotiles
ENV VITE_TILE_URL=$VITE_TILE_URL
ENV VITE_TILE_ATTRIBUTION=$VITE_TILE_ATTRIBUTION
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Dharmatlas.Host/Dharmatlas.Host.csproj --ignore-failed-sources -p:NuGetAudit=false --nologo
RUN dotnet publish src/Dharmatlas.Host/Dharmatlas.Host.csproj -c Release -o /app/publish --no-restore --nologo
COPY --from=web-build /web/dist/ /app/publish/wwwroot/

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
# The official .NET image ships a non-root `app` user; run as it and hand it
# ownership of the published output. No default credentials are baked in.
RUN chown -R app:app /app
USER app
ENTRYPOINT ["dotnet", "Dharmatlas.Host.dll"]
