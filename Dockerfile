# syntax=docker/dockerfile:1
# Orange Pi 5 (RK3588) runs Debian on linux/arm64.
# .NET 10 no longer publishes Debian container images (the floating tags are Ubuntu),
# so both stages use official Debian and install .NET from the Microsoft Debian feed.
# The feed ships .NET 10 for amd64 and arm64; CI publishes only linux/arm64.

FROM --platform=$BUILDPLATFORM debian:13-slim AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl -fsSL -o /tmp/packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends dotnet-sdk-10.0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["src/UniiaAnonim.TGBot.Api/UniiaAnonim.TGBot.Api.csproj", "src/UniiaAnonim.TGBot.Api/"]
COPY ["src/UniiaAnonim.TGBot.Infrastructure/UniiaAnonim.TGBot.Infrastructure.csproj", "src/UniiaAnonim.TGBot.Infrastructure/"]
COPY ["src/UniiaAnonim.TGBot.Domain/UniiaAnonim.TGBot.Domain.csproj", "src/UniiaAnonim.TGBot.Domain/"]
COPY ["src/UniiaAnonim.TGBot.Services/UniiaAnonim.TGBot.Application.csproj", "src/UniiaAnonim.TGBot.Services/"]
COPY ["src/UniiaAnonim.TGBot.Shared/UniiaAnonim.TGBot.Shared.csproj", "src/UniiaAnonim.TGBot.Shared/"]
RUN case "$TARGETARCH" in \
      amd64) DOTNET_ARCH=x64 ;; \
      arm64) DOTNET_ARCH=arm64 ;; \
      *) echo "Unsupported architecture: $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && dotnet restore "./src/UniiaAnonim.TGBot.Api/UniiaAnonim.TGBot.Api.csproj" -a "$DOTNET_ARCH"
COPY . .
WORKDIR "/src/src/UniiaAnonim.TGBot.Api"
RUN case "$TARGETARCH" in \
      amd64) DOTNET_ARCH=x64 ;; \
      arm64) DOTNET_ARCH=arm64 ;; \
      *) echo "Unsupported architecture: $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && dotnet publish "./UniiaAnonim.TGBot.Api.csproj" \
      -c "$BUILD_CONFIGURATION" \
      -a "$DOTNET_ARCH" \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

FROM debian:13-slim AS final
ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl passwd \
    && curl -fsSL -o /tmp/packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends aspnetcore-runtime-10.0 \
    && groupadd --system --gid 1654 app \
    && useradd --system --uid 1654 --gid 1654 --home-dir /app --no-create-home --shell /bin/false app \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    APP_UID=1654

WORKDIR /app
COPY --from=build --chown=1654:1654 /app/publish .
USER 1654

EXPOSE 8080
HEALTHCHECK --interval=2m --timeout=5s --start-period=30s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "UniiaAnonim.TGBot.Api.dll"]
