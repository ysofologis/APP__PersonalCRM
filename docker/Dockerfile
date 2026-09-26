# syntax=docker/dockerfile:1.7

############################
# Stage 1 — restore + build
############################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first for better layer caching
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/PersonalCrm.Core/PersonalCrm.Core.csproj              src/PersonalCrm.Core/
COPY src/PersonalCrm.Shared/PersonalCrm.Shared.csproj          src/PersonalCrm.Shared/
COPY src/PersonalCrm.Infrastructure/PersonalCrm.Infrastructure.csproj src/PersonalCrm.Infrastructure/
COPY src/PersonalCrm.App/PersonalCrm.App.csproj                src/PersonalCrm.App/
COPY src/PersonalCrm.Migrations/PersonalCrm.Migrations.csproj src/PersonalCrm.Migrations/
COPY tests/PersonalCrm.Tests.Unit/PersonalCrm.Tests.Unit.csproj             tests/PersonalCrm.Tests.Unit/
COPY tests/PersonalCrm.Tests.Integration/PersonalCrm.Tests.Integration.csproj tests/PersonalCrm.Tests.Integration/

# Restore as a distinct layer
RUN dotnet restore "src/PersonalCrm.App/PersonalCrm.App.csproj"

# Copy the rest of the source
COPY . .

# Build + publish the App
RUN dotnet publish "src/PersonalCrm.App/PersonalCrm.App.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

############################
# Stage 2 — runtime
############################
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for runtime
RUN groupadd --system --gid 1001 personalcrm \
    && useradd  --system --uid 1001 --gid personalcrm \
                --home-dir /app --shell /usr/sbin/nologin \
                personalcrm \
    && mkdir -p /app/data/attachments /app/data/keys /app/data/backups \
    && chown -R personalcrm:personalcrm /app

COPY --from=build --chown=personalcrm:personalcrm /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    HOME=/app

USER personalcrm
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD ["dotnet", "--info"] >/dev/null 2>&1 \
    || exit 1

ENTRYPOINT ["dotnet", "PersonalCrm.App.dll"]
