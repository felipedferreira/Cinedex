FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

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
COPY ["NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/FoundryOceanus.Observability.OpenTelemetry.csproj", "NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/"]
COPY ["NuGetLibraries/Contracts/FoundryOceanus.WebService.Contracts/FoundryOceanus.WebService.Contracts.csproj", "NuGetLibraries/Contracts/FoundryOceanus.WebService.Contracts/"]
RUN dotnet restore "src/Presentation/Cinedex.WebService/Cinedex.WebService.csproj"
COPY . .
WORKDIR "/src/src/Presentation/Cinedex.WebService"
RUN dotnet build "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinedex.WebService.dll"]
