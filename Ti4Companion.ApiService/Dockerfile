# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer-cached): copy only the project files the API needs.
COPY Ti4Companion.ServiceDefaults/Ti4Companion.ServiceDefaults.csproj Ti4Companion.ServiceDefaults/
COPY Ti4Companion.Shared/Ti4Companion.Shared.csproj                   Ti4Companion.Shared/
COPY Ti4Companion.Web/Ti4Companion.Web.csproj                         Ti4Companion.Web/
COPY Ti4Companion.ApiService/Ti4Companion.ApiService.csproj           Ti4Companion.ApiService/
RUN dotnet restore Ti4Companion.ApiService/Ti4Companion.ApiService.csproj

# Build & publish. Publishing the API also bundles the Blazor WebAssembly client's static assets.
COPY Ti4Companion.ServiceDefaults/ Ti4Companion.ServiceDefaults/
COPY Ti4Companion.Shared/          Ti4Companion.Shared/
COPY Ti4Companion.Web/             Ti4Companion.Web/
COPY Ti4Companion.ApiService/      Ti4Companion.ApiService/
RUN dotnet publish Ti4Companion.ApiService/Ti4Companion.ApiService.csproj \
    -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ti4Companion.ApiService.dll"]
