FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:22-alpine AS ts-build
WORKDIR /work
COPY ["src/Presentation/Cinedex.WebService/package.json", "src/Presentation/Cinedex.WebService/package-lock.json", "./"]
RUN npm ci
COPY ["src/Presentation/Cinedex.WebService/tsconfig.json", "src/Presentation/Cinedex.WebService/vite.config.ts", "src/Presentation/Cinedex.WebService/index.html", "src/Presentation/Cinedex.WebService/changelog.html", "./"]
COPY ["src/Presentation/Cinedex.WebService/src", "./src"]
COPY ["src/Presentation/Cinedex.WebService/public", "./public"]
RUN npx vite build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Presentation/Cinedex.WebService/Cinedex.WebService.csproj", "src/Presentation/Cinedex.WebService/"]
COPY ["src/Application/Cinedex.Application.csproj", "src/Application/"]
COPY ["src/Domain/Cinedex.Domain.csproj", "src/Domain/"]
COPY ["src/Adapters/Cinedex.Auth.Identity/Cinedex.Auth.Identity.csproj", "src/Adapters/Cinedex.Auth.Identity/"]
COPY ["src/Adapters/Cinedex.Email.Smtp/Cinedex.Email.Smtp.csproj", "src/Adapters/Cinedex.Email.Smtp/"]
COPY ["src/Adapters/Cinedex.Persistence.Postgres/Cinedex.Persistence.Postgres.csproj", "src/Adapters/Cinedex.Persistence.Postgres/"]
COPY ["NuGetLibraries/Cinedex.Observability.OpenTelemetry/Cinedex.Observability.OpenTelemetry.csproj", "NuGetLibraries/Cinedex.Observability.OpenTelemetry/"]
COPY ["NuGetLibraries/Cinedex.WebService.Contracts/Cinedex.WebService.Contracts.csproj", "NuGetLibraries/Cinedex.WebService.Contracts/"]
RUN dotnet restore "src/Presentation/Cinedex.WebService/Cinedex.WebService.csproj"
COPY . .
COPY --from=ts-build /work/wwwroot /src/src/Presentation/Cinedex.WebService/wwwroot
WORKDIR "/src/src/Presentation/Cinedex.WebService"
RUN dotnet build "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:SkipFrontendBuild=true

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SkipFrontendBuild=true

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinedex.WebService.dll"]
