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
COPY ["src/Applications/Movies.WebService/Movies.WebService.csproj", "src/Applications/Movies.WebService/"]
COPY ["src/Core/Movies.Domain/Movies.Domain.csproj", "src/Core/Movies.Domain/"]
COPY ["src/Adapters/Movies.Persistance.Postgres/Movies.Persistance.Postgres.csproj", "src/Adapters/Movies.Persistance.Postgres/"]
RUN dotnet restore "src/Applications/Movies.WebService/Movies.WebService.csproj"
COPY . .
WORKDIR "/src/src/Applications/Movies.WebService"
RUN dotnet build "./Movies.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Movies.WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Movies.WebService.dll"]
