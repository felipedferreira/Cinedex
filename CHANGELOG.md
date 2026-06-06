# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
