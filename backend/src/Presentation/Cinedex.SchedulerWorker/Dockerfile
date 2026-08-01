FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Presentation/Cinedex.SchedulerWorker/Cinedex.SchedulerWorker.csproj", "src/Presentation/Cinedex.SchedulerWorker/"]
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
