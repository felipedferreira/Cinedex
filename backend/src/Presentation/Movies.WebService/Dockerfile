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
COPY ["src/Presentation/Movies.WebService/Movies.WebService.csproj", "src/Presentation/Movies.WebService/"]
COPY ["src/Application/Movies.Application.csproj", "src/Application/"]
COPY ["src/Domain/Movies.Domain.csproj", "src/Domain/"]
COPY ["src/Adapters/Movies.Persistence.Postgres/Movies.Persistence.Postgres.csproj", "src/Adapters/Movies.Persistence.Postgres/"]
RUN dotnet restore "src/Presentation/Movies.WebService/Movies.WebService.csproj"
COPY . .
WORKDIR "/src/src/Presentation/Movies.WebService"
RUN dotnet build "./Movies.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Movies.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Movies.WebService.dll"]
