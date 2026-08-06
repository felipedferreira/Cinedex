# Cinedex.Auth.Identity carries a FrameworkReference to Microsoft.AspNetCore.App, so this
# worker needs the aspnet image even though it serves no HTTP traffic.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Presentation/Cinedex.SchedulerWorker/Cinedex.SchedulerWorker.csproj", "src/Presentation/Cinedex.SchedulerWorker/"]
COPY ["src/Application/Cinedex.Application.csproj", "src/Application/"]
COPY ["src/Domain/Cinedex.Domain.csproj", "src/Domain/"]
COPY ["src/Adapters/Cinedex.Auth.Identity/Cinedex.Auth.Identity.csproj", "src/Adapters/Cinedex.Auth.Identity/"]
COPY ["NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/FoundryOceanus.Observability.OpenTelemetry.csproj", "NuGetLibraries/Observability/FoundryOceanus.Observability.OpenTelemetry/"]
RUN dotnet restore "src/Presentation/Cinedex.SchedulerWorker/Cinedex.SchedulerWorker.csproj"
COPY . .
WORKDIR "/src/src/Presentation/Cinedex.SchedulerWorker"
RUN dotnet build "./Cinedex.SchedulerWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cinedex.SchedulerWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinedex.SchedulerWorker.dll"]
