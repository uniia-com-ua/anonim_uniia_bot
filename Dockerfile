# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/UniiaAnonim.TGBot.Api/UniiaAnonim.TGBot.Api.csproj", "src/UniiaAnonim.TGBot.Api/"]
COPY ["src/UniiaAnonim.TGBot.Infrastructure/UniiaAnonim.TGBot.Infrastructure.csproj", "src/UniiaAnonim.TGBot.Infrastructure/"]
COPY ["src/UniiaAnonim.TGBot.Domain/UniiaAnonim.TGBot.Domain.csproj", "src/UniiaAnonim.TGBot.Domain/"]
COPY ["src/UniiaAnonim.TGBot.Services/UniiaAnonim.TGBot.Application.csproj", "src/UniiaAnonim.TGBot.Services/"]
COPY ["src/UniiaAnonim.TGBot.Shared/UniiaAnonim.TGBot.Shared.csproj", "src/UniiaAnonim.TGBot.Shared/"]
RUN dotnet restore "./src/UniiaAnonim.TGBot.Api/UniiaAnonim.TGBot.Api.csproj"
COPY . .
WORKDIR "/src/src/UniiaAnonim.TGBot.Api"
RUN dotnet build "./UniiaAnonim.TGBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UniiaAnonim.TGBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=2m --timeout=5s --start-period=30s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "UniiaAnonim.TGBot.Api.dll"]