FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:22-alpine AS ts-build
WORKDIR /work
COPY ["src/Presentation/Cinedex.WebService/package.json", "src/Presentation/Cinedex.WebService/package-lock.json", "./"]
RUN npm ci
COPY ["src/Presentation/Cinedex.WebService/tsconfig.json", "./"]
COPY ["src/Presentation/Cinedex.WebService/scripts", "./scripts"]
RUN npx tsc -p tsconfig.json

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Presentation/Cinedex.WebService/Cinedex.WebService.csproj", "src/Presentation/Cinedex.WebService/"]
COPY ["src/Application/Cinedex.Application.csproj", "src/Application/"]
COPY ["src/Domain/Cinedex.Domain.csproj", "src/Domain/"]
COPY ["src/Adapters/Cinedex.Persistence.Postgres/Cinedex.Persistence.Postgres.csproj", "src/Adapters/Cinedex.Persistence.Postgres/"]
COPY ["NuGetLibraries/Cinedex.WebService.Contracts/Cinedex.WebService.Contracts.csproj", "NuGetLibraries/Cinedex.WebService.Contracts/"]
RUN dotnet restore "src/Presentation/Cinedex.WebService/Cinedex.WebService.csproj"
COPY . .
COPY --from=ts-build /work/wwwroot/js /src/src/Presentation/Cinedex.WebService/wwwroot/js
WORKDIR "/src/src/Presentation/Cinedex.WebService"
RUN dotnet build "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:SkipTypeScriptBuild=true

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cinedex.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SkipTypeScriptBuild=true

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinedex.WebService.dll"]
