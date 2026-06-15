# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **`EntityNotFoundException`** - Domain exception in `Movies.Application/Exceptions/` thrown by handlers when a requested entity does not exist; carries `entityName` and `id` for a self-describing message
- **Chain of Responsibility exception handling** - Replaced the monolithic `ExceptionHandlingMiddleware` with three focused `IExceptionHandler` implementations under `Movies.WebService/ExceptionHandlers/`:
  - `ValidationExceptionHandler` — `ValidationException` → HTTP 400 with per-field error map
  - `EntityNotFoundExceptionHandler` — `EntityNotFoundException` → HTTP 404 with the exception message as `detail`
  - `DefaultExceptionHandler` — catch-all → HTTP 500; also logs the exception (was missing before)
- **Per-use-case handlers with FluentValidation** - Decomposed `IMovieService` / `MovieService` into discrete CQRS-style handler classes in `Movies.Application/Movies/`:
  - `CreateMovieHandler` + `CreateMovieValidator` (title max 256 chars, year range 1888–current)
  - `UpdateMovieHandler` + `UpdateMovieValidator`
  - `DeleteMovieHandler`, `GetMovieByIdHandler`, `ListMoviesHandler`
  - Each feature folder contains command/query record, handler, interface, and optional validator
- **React + TypeScript + Vite frontend** - Scaffolded `frontend/cinadex-ui` with React 19, TypeScript, Vite, Vitest, ESLint, and Prettier; includes a working `App.tsx` shell and an `App.test.tsx` smoke test
- **`SmokeTests`** - Backend integration test asserting the API returns HTTP 200 on `GET /api/movies`
- **`CreateMovieEndpointTests`** - Integration tests for `POST /api/movies` covering happy path and FluentValidation error scenarios (missing title, out-of-range year, etc.)

### Changed
- **`GetMovieByIdHandler`, `UpdateMovieHandler`, `DeleteMovieHandler`** - Now throw `EntityNotFoundException` when the entity is not found instead of returning `null` / `false`; handler interfaces updated to non-nullable return types (`Task<MovieDto>` / `Task`)
- **`GetMovieByIdEndpoint`, `UpdateMovieEndpoint`, `DeleteMovieEndpoint`** - Null / bool guards removed; not-found signal is now the handler's responsibility via exception
- **`Program.cs`** - `UseMiddleware<ExceptionHandlingMiddleware>()` replaced by `UseExceptionHandler()`; three `AddExceptionHandler<T>()` registrations (order matters — `DefaultExceptionHandler` last)
- **`DependencyInjection.cs`** (Application) - Handlers registered individually; validators auto-discovered via `AddValidatorsFromAssembly`; `FluentValidation` and `MediatR`-style dependencies added to `Movies.Application.csproj`
- **CI workflow** - Updated to build and test the frontend alongside the backend

### Added (continued)
- **Mono-repo layout** - Restructured the repository for a future standalone frontend:
  - All .NET solution files (`src/`, `tests/`, `Movies.slnx`, build props, `global.json`, coverage scripts) moved under `backend/` with `git mv` to preserve history
  - `frontend/` placeholder added for the upcoming SPA (Angular or React), which will consume the backend's OpenAPI spec
  - New repository-level `README.md` landing page; the architecture guide moved to `backend/README.md`
  - `compose.yaml` build context repointed to `./backend`; CI workflow now runs `dotnet` commands from `backend/`
- **Movies CRUD API** - First real resource, implemented with [FastEndpoints](https://fast-endpoints.com/) using the REPR (Request-Endpoint-Response) pattern — one class per endpoint under `Endpoints/Movies/`:
  - `GET /api/movies`, `GET /api/movies/{id}`, `POST /api/movies`, `PUT /api/movies/{id}`, `DELETE /api/movies/{id}`
  - `MovieMappings` - single translation point between `Contracts` DTOs and the `Domain.Movie` model
- **`MovieRepository`** - EF Core implementation of `IMovieRepository` in `Movies.Persistence.Postgres` (uses `ExecuteUpdateAsync`/`ExecuteDeleteAsync`)
- **`AddPersistence(connectionString)`** - DI extension that now owns the `MoviesDbContext` and repository registrations
- **Docker Compose configuration** - Complete multi-container setup with PostgreSQL:
  - `compose.yaml` with web service and PostgreSQL 17 Alpine
  - Health checks for database readiness before application startup
  - Environment variables for database configuration

### Changed
- **`GetMovieByIdHandler`, `UpdateMovieHandler`, `DeleteMovieHandler`** - Now throw `EntityNotFoundException` when the entity is not found instead of returning `null` / `false`; handler interfaces updated to non-nullable return types (`Task<MovieDto>` / `Task`)
- **`GetMovieByIdEndpoint`, `UpdateMovieEndpoint`, `DeleteMovieEndpoint`** - Null / bool guards removed; not-found signal is now the handler's responsibility via exception
- **`Program.cs`** - `UseMiddleware<ExceptionHandlingMiddleware>()` replaced by `UseExceptionHandler()`; three `AddExceptionHandler<T>()` registrations (order matters — `DefaultExceptionHandler` last)
- **`DependencyInjection.cs`** (Application) - Handlers registered individually; validators auto-discovered via `AddValidatorsFromAssembly`; `FluentValidation` and `MediatR`-style dependencies added to `Movies.Application.csproj`
- **CI workflow** - Updated to build and test the frontend alongside the backend
- **Hexagonal layer layout** - Reorganized the backend folders to make the Ports & Adapters layers explicit, dependencies still inward-only (`Domain ← Application ← {Adapters, Presentation}`). All moves used `git mv` to preserve history:
  - `src/Applications/` → `src/Presentation/` (hosts `Movies.WebService`)
  - `src/Core/` dissolved; the singleton `Movies.Application` and `Movies.Domain` projects now sit directly under `src/` (Application and Domain layers will never have a second project)
  - `src/Adapters/` retained as-is (may host additional adapters)
  - Updated all `ProjectReference` paths, `Movies.slnx` solution folders, the `Dockerfile` COPY paths, and `compose.yaml`'s `dockerfile` path; the Dockerfile now also copies `Movies.Application.csproj` before `dotnet restore` (was missing)
- **`Program.cs`** (earlier) - Registers `AddApplication()` + `AddPersistence()` and `AddFastEndpoints()`/`UseFastEndpoints()`; serves the app under the `/api` base path via `UsePathBase`; `MoviesDbContext` registration moved out into `AddPersistence`
- **`Directory.Packages.props`** - Centralized versions for `FastEndpoints` and `Microsoft.Extensions.DependencyInjection.Abstractions`
- **`Movies.WebService.csproj`** - Added `FastEndpoints` package reference and a project reference to `Movies.WebService.Contracts`
- **`Movies.Application.csproj`** - Added `Microsoft.Extensions.DependencyInjection.Abstractions` (for the `AddApplication` extension)
- **Dockerfile** - Fixed build paths to match actual project structure:
  - Updated COPY paths from `src/Movies.WebService/` to `src/Applications/Movies.WebService/`
  - Added copying of `Directory.Build.props` and `Directory.Packages.props` before restore
  - Added explicit COPY steps for domain and persistence layer dependencies
- **Clean-architecture cleanup** - Tidied the Core and adapter layout:
  - Renamed `Movies.Persistance.Postgres` → `Movies.Persistence.Postgres` (project, folder, and namespace) to fix the long-standing typo
  - Merged `Movies.Application.Abstractions` into `Movies.Application`; the `IMovieRepository`/`IMovieService` ports now live under `Movies.Application/Abstractions/` with their namespace unchanged
  - Rewired references accordingly: `Movies.Persistence.Postgres` now references `Movies.Application` for the ports, and `Movies.Application` references `Movies.Domain` directly. Dependency direction is unchanged (still inward-only)

### Removed
- **`IMovieService` / `MovieService`** - Replaced by per-use-case handlers; the single-service pattern did not scale as use cases multiplied
- **`ExceptionHandlingMiddleware`** - Replaced by the `IExceptionHandler` chain; the class violated SRP by owning both exception routing and response formatting for every exception type
- **Template sample endpoints** - Deleted the project-template placeholders now that Movies is the first real resource:
  - `GET /weatherforecast` endpoint and the `WeatherForecast` record, plus `WeatherForecastEndpointTests`
  - `GET /test-exception` endpoint and `ExceptionHandlingMiddlewareTests` (the endpoint existed only to exercise the exception middleware)

### Fixed
- **Docker build** - Corrected path references to resolve NuGet restore failures

---

## [0.5.0] - Entity Framework Core with PostgreSQL

### Added
- **Entity Framework Core 10** - ORM integration using `Npgsql.EntityFrameworkCore.PostgreSQL`
- **`MoviesDbContext`** - EF Core `DbContext` in `Movies.Persistance.Postgres`; uses Fluent API configuration via `ApplyConfigurationsFromAssembly` to keep domain models free of EF annotations
- **`MoviesDbContextFactory`** - `IDesignTimeDbContextFactory` implementation enabling `dotnet ef` CLI tools to run without starting the full application
- **Database connection string** - `DefaultConnection` added to `appsettings.json` with a default local PostgreSQL configuration

### Changed
- **`Program.cs`** - Registers `MoviesDbContext` via `AddDbContext<MoviesDbContext>` wired to `ConnectionStrings:DefaultConnection`
- **`Directory.Packages.props`** - Centralized versions for `Microsoft.EntityFrameworkCore.Design` and `Npgsql.EntityFrameworkCore.PostgreSQL`
- **`Movies.Persistance.Postgres.csproj`** - Added EF Core and Npgsql package references
- **`Movies.WebService.csproj`** - Added `Microsoft.EntityFrameworkCore.Design` (required as the migrations startup project)

---

## [0.4.1] - Exception Handling Tests and Formatting Rules

### Added
- **ExceptionHandlingMiddlewareTests** - Comprehensive integration tests for exception handling middleware:
  - `TestException_ReturnsInternalServerError` - Verifies 500 status code on unhandled exceptions
  - `TestException_ReturnsProblemDetailsJson` - Validates RFC 7807 Problem Details response format
  - `TestException_ReturnsProblemDetailsWithRequiredFields` - Ensures all required fields in error response
  - `TestException_IncludesTraceIdInResponse` - Confirms trace ID in error extensions for debugging
- **EditorConfig ReSharper rules** - Enforce blank lines between POCO object properties:
  - `resharper_blank_lines_around_auto_property = 1` - Blank lines around auto-properties
  - `resharper_blank_lines_around_property = 1` - Blank lines around full properties
  - `resharper_blank_lines_after_block_statements = 1` - Blank lines after block statements

### Fixed
- **Build configuration** - Added `GenerateDocumentationFile=true` to `Movies.WebService.Contracts.csproj`
  - Enables IDE0005 (Remove unnecessary imports) rule enforcement on build

---

## [0.4.0] - Exception Handling Middleware (#4)

### Added
- **ExceptionHandlingMiddleware** - Proper error handling for unhandled exceptions:
  - Returns RFC 7807 Problem Details format
  - Includes trace ID in response extensions for request correlation
  - Proper async/await pattern for middleware execution
  - Correct content-type header (`application/problem+json`)
- **Test endpoint `/test-exception`** - Endpoint for testing exception handling behavior
- **GitHub branch protection** - Configured to prevent problematic merges:
  - Requires status checks to pass before merging
  - Enforces branches must be up-to-date before merge

### Changed
- **ExceptionHandlingMiddleware** - Improved JSON serialization:
  - Uses manual JSON serialization to preserve content-type
  - Properly serializes `extensions` object with `traceId`
  - Removed dependency on `Microsoft.AspNetCore.Mvc.ProblemDetails`
- **Program.cs** - Code organization improvements with better blank lines

---

## [0.3.1] - Code Quality Improvements

### Changed
- **Code quality enhancements** - Refactoring and standards improvements
- **Documentation file generation** - Added documentation generation for code analysis

---

## [0.3.0] - Structured Logging with Serilog (#3)

### Added
- **Serilog integration** - Structured logging with console output
- **CorrelationIdMiddleware** - Middleware for request correlation tracking
- **Request logging** - Automatic logging of HTTP requests via Serilog

### Changed
- **Logging configuration** - Replaced default logging with Serilog structured logging

---

## [0.2.1] - Integration Tests and Code Coverage

### Added
- **WeatherForecastEndpointTests** - Integration tests for WeatherForecast endpoint:
  - Test for 200 OK status
  - Test for JSON array response
  - Test for 5-day forecast data
  - Test for required fields in forecast objects
- **WebApplicationFixture** - Test fixture for integration testing setup
- **Code coverage configuration** - Local code coverage reporting setup

---

## [0.2.0] - Request Timeout Configuration (#2)

### Added
- **Kestrel timeout configuration** - Request timeout settings:
  - `RequestHeadersTimeout` - Protection against slowloris attacks
  - `KeepAliveTimeout` - Idle connection timeout management

---

## [0.1.0] - Initial Project Setup

### Added
- **GitHub Actions CI/CD** - Automated build and test workflow (build-and-test.yml)
- **.NET SDK configuration** - Global.json for SDK version management
- **Docker support** - Docker configuration and .dockerignore
- **Project structure** - Core project organization with:
  - Movies.WebService (main API)
  - Movies.Domain (domain models)
  - Movies.Application (business logic)
  - Movies.Application.Abstractions (interfaces)
  - Movies.Persistance.Postgres (data access)
  - Movies.WebService.Contracts (DTOs)
  - Integration tests project
- **WeatherForecast endpoint** - Sample endpoint implementation

---

## Version Numbering Strategy

This project follows [Semantic Versioning](https://semver.org/) with the following scheme for pre-1.0 development:

- **PATCH** (0.x.y) - Bug fixes, testing infrastructure, tooling improvements, code quality
- **MINOR** (0.x.0) - New features or significant capability additions
- **MAJOR** (1.0+) - Reserved for production-ready release with breaking changes
