FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Presentation/Cinedex.DatabaseMigrator/Cinedex.DatabaseMigrator.csproj", "src/Presentation/Cinedex.DatabaseMigrator/"]
COPY ["src/Application/Cinedex.Application.csproj", "src/Application/"]
COPY ["src/Domain/Cinedex.Domain.csproj", "src/Domain/"]
COPY ["src/Adapters/Cinedex.Auth.Identity/Cinedex.Auth.Identity.csproj", "src/Adapters/Cinedex.Auth.Identity/"]
COPY ["src/Adapters/Cinedex.Persistence.Postgres/Cinedex.Persistence.Postgres.csproj", "src/Adapters/Cinedex.Persistence.Postgres/"]
COPY ["NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/FoundryOceanus.Observability.OpenTelemetry.csproj", "NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/"]
RUN dotnet restore "src/Presentation/Cinedex.DatabaseMigrator/Cinedex.DatabaseMigrator.csproj"
COPY . .
WORKDIR "/src/src/Presentation/Cinedex.DatabaseMigrator"
RUN dotnet build "./Cinedex.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cinedex.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinedex.DatabaseMigrator.dll"]
