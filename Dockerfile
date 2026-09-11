FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Dharmatlas.Host/Dharmatlas.Host.csproj --ignore-failed-sources -p:NuGetAudit=false --nologo
RUN dotnet publish src/Dharmatlas.Host/Dharmatlas.Host.csproj -c Release -o /app/publish --no-restore --nologo

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dharmatlas.Host.dll"]
