# =============================================================
# AI-LMS Backend — Multi-stage Dockerfile
# infra/docker/backend.Dockerfile
#
# Stage 1 (restore) : restore NuGet packages
# Stage 2 (build)   : compile and publish (self-contained = false, framework-dep)
# Stage 3 (runtime) : lean ASP.NET Core runtime image
#
# Build context: ./backend  (the backend/ folder)
# =============================================================

# ── Stage 1: Restore ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS restore

WORKDIR /src

# Copy solution and all .csproj files first to maximise layer cache.
# NuGet packages are re-downloaded only when these files change.
COPY LMS.sln global.json ./
COPY src/LMS.Api/LMS.Api.csproj             src/LMS.Api/
COPY src/LMS.Application/LMS.Application.csproj  src/LMS.Application/
COPY src/LMS.Domain/LMS.Domain.csproj       src/LMS.Domain/
COPY src/LMS.Infrastructure/LMS.Infrastructure.csproj src/LMS.Infrastructure/
COPY tests/LMS.Api.Tests/LMS.Api.Tests.csproj           tests/LMS.Api.Tests/
COPY tests/LMS.Application.Tests/LMS.Application.Tests.csproj tests/LMS.Application.Tests/
COPY tests/LMS.Domain.Tests/LMS.Domain.Tests.csproj     tests/LMS.Domain.Tests/

RUN dotnet restore LMS.sln


# ── Stage 2: Build & Publish ─────────────────────────────────────────────
FROM restore AS build

# Copy all source (now that packages are cached above)
COPY . .

RUN dotnet publish src/LMS.Api/LMS.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /publish


# ── Stage 3: Runtime ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install culture/timezone data required for ASP.NET Core globalisation
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

# Run as non-root user — never run web apps as root
RUN addgroup -g 1001 -S appgroup \
  && adduser -u 1001 -S appuser -G appgroup

COPY --from=build --chown=appuser:appgroup /publish .

USER appuser

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "LMS.Api.dll"]
