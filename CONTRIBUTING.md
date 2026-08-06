# Contributing to Cinedex

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the Cinedex project.

## Code of Conduct

Be respectful, inclusive, and professional in all interactions.

## Getting Started

### Prerequisites
- .NET 10.0 or later
- Node.js 22 or later (with npm)
- Docker (for containerized development or running the full stack)
- Git
- A code editor (VS Code, Visual Studio, Rider, etc.)

### Setup Development Environment

#### Option A — Docker Compose (full stack)

Full walkthrough — `.env` setup, first-run Seq setup, access points, and troubleshooting — is in
**[docs/getting-started.md](docs/getting-started.md)**. Short version:

```bash
cp .env.example .env       # fill in the database, Seq, and Mailpit values
docker compose up --build
```

The one-shot database migrator applies pending migrations for both `FilmDbContext` and
`AuthDbContext` automatically — nothing else to run by hand for a fresh database.

#### Option B — Local development

**Backend** (all `dotnet` commands run from `backend/`):

1. Clone the repository and move into the backend folder:
   ```bash
   git clone https://github.com/felipedferreira/Cinedex.git
   cd Cinedex/backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

**Frontend** (commands run from `frontend/`, the npm workspace root):

1. Install dependencies for every package:
   ```bash
   npm ci
   ```

2. Start the dev server (https://localhost:9000):
   ```bash
   npm run dev
   ```

3. Browse the component library (http://localhost:9001):
   ```bash
   npm run storybook
   ```

4. Run tests:
   ```bash
   npm run test:run    # single pass across all packages
   ```
   Watch mode is per-package: `npm run test -w cinadex-app` or `-w @cinedex/components`.

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b your-feature-name
```

Branches use short kebab-case descriptions with no prefix convention — recent examples:
`asp-net-identity-auth`, `smtp-client`. The change *type* is expressed in the commit
message instead (see below).

### 2. Make Your Changes

- Follow the code style guidelines (enforced by EditorConfig and StyleCop)
- Write clear, descriptive commit messages
- Include tests for new functionality
- Update documentation as needed

### 3. Run Tests and Lint

Before pushing, ensure all tests pass and code quality checks pass.

**Backend** (from `backend/`):

```bash
dotnet test
dotnet build
```

**Frontend** (from `frontend/`):

```bash
npm run lint
npm run format:check
npm run test:run
npm run build-storybook
```

Your IDE should automatically enforce EditorConfig rules (backend) and Prettier/ESLint rules (frontend). Most formatting issues can be fixed automatically with `npm run lint:fix` and `npm run format`.

### 4. Commit and Push

```bash
git add .
git commit -m "feat: add new feature description"
git push origin your-feature-name
```

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):
`type(scope): summary`, with an optional scope. Types in use: `feat`, `fix`, `refactor`,
`test`, `docs`, `chore`.

### 5. Create a Pull Request

- Push your branch and create a PR on GitHub
- Write a clear PR description explaining the changes
- Reference any related issues (e.g., "Fixes #123")
- Ensure all CI checks pass
- Request review from maintainers

## Versioning (Pre-1.0 Development)

We use [Semantic Versioning](https://semver.org/): **MAJOR.MINOR.PATCH**

### Current Versioning Scheme

- **PATCH** (0.1.x): Bug fixes and small improvements
- **MINOR** (0.x.0): New features or significant improvements
- **MAJOR**: Reserved for production release (1.0.0+)

### When to Bump Version

1. **PATCH version** (0.1.x → 0.1.1):
   - Bug fixes
   - Small improvements
   - Documentation updates
   - No API changes

2. **MINOR version** (0.1.0 → 0.2.0):
   - New features
   - Significant improvements
   - Substantial changes to existing functionality
   - Still backward compatible

3. **MAJOR version** (0.x.0 → 1.0.0):
   - Breaking changes to the public API
   - Reserved for production-ready release

### Updating Version Numbers

Version numbers are centralized in `backend/Directory.Build.props`:

```xml
<PropertyGroup>
  <Version>0.6.0</Version>
  <FileVersion>0.6.0</FileVersion>
  <InformationalVersion>0.6.0</InformationalVersion>
</PropertyGroup>
```

Update all three properties together for consistency, and keep them in step with the
version headings in `CHANGELOG.md`.

## Code Standards

### C# Code Style

The project uses:
- **EditorConfig** - Enforces formatting rules (automatic)
- **StyleCop** - Static analysis for code quality
- **Code analysis** - Warnings treated as errors

Key rules:
- Use PascalCase for public types and methods
- Use camelCase for local variables and parameters
- Use _camelCase for private fields
- One type per file (enforced by SA1402)
- Blank lines between property groups (enforced by ReSharper rules)

### Writing Tests

- Write integration tests for endpoint behavior
- Use xUnit for testing framework
- Follow the pattern: `Action_Condition_Result`
- Example: `GetMovie_WithUnknownId_ReturnsNotFound`
- Tests should be isolated and deterministic
- Integration tests require Docker to be running (Testcontainers spins up a Postgres container)
- Catalog endpoints are members-only — use the fixture's `AuthenticatedClient` for them

### Documentation

- Write XML documentation for public members
- Keep README.md up to date
- Update CHANGELOG.md for significant changes — **edit only the root `CHANGELOG.md`**.
  `backend/CHANGELOG.md` is a build-managed copy (the web service serves it as the app's
  changelog page, and the Docker build can't see the repo root); a local backend build
  refreshes it, and CI fails if the two files differ
- Document API endpoints and their behavior

## Middleware and Exception Handling

### Exception handler chain

Unhandled exceptions are processed by a chain of `IExceptionHandler` implementations under
`Cinedex.WebService/ExceptionHandlers/` (registration order matters — `DefaultExceptionHandler` last):

- `ValidationExceptionHandler` — `ValidationException` → HTTP 400 with a per-field error map
- `EntityNotFoundExceptionHandler` — `EntityNotFoundException` → HTTP 404
- `InvalidCredentialsExceptionHandler` — `InvalidCredentialsException` → HTTP 401
- `DefaultExceptionHandler` — catch-all → HTTP 500, logs the exception

All error responses are RFC 7807 Problem Details and carry the request's correlation ID.

### CorrelationIdMiddleware

- Generates or passes through correlation IDs
- Helps track requests through the system
- Included in error responses for correlation

## Build and CI/CD

### GitHub Actions

The project has automated CI/CD configured in `.github/workflows/build-and-test.yml`:

- Runs on every push to main and on all pull requests
- **Backend job** — changelog-sync check (root `CHANGELOG.md` vs `backend/CHANGELOG.md`), Release build, tests
- **Frontend job** — `lint`, `format:check`, `build`, `build-storybook`, coverage (one summary per workspace package)

Status checks are **required** to merge to main.

### Branch Protection Rules

Main branch is protected with:
- ✅ Require status checks to pass
- ✅ Require branches to be up to date before merge
- ✅ Prevent stale PRs from being merged

## Common Tasks

All commands below run from the `backend/` folder.

### Running a Specific Test

```bash
dotnet test --filter "CreateTitleEndpointTests"
```

### Running with Verbose Output

```bash
dotnet test --verbosity detailed
```

### Building in Release Mode

```bash
dotnet build --configuration Release
```

### Cleaning Build Artifacts

```bash
dotnet clean
```

## Pull Request Checklist

Before submitting a PR, ensure:

- [ ] Code builds without errors
- [ ] All tests pass (`dotnet test` and/or `npm run test:run`)
- [ ] Code follows style guidelines (EditorConfig + StyleCop for backend; ESLint + Prettier for frontend)
- [ ] New features have tests
- [ ] Documentation is updated
- [ ] Commit messages are clear and descriptive
- [ ] Branch is up to date with main
- [ ] No unrelated changes are included

## Questions or Need Help?

- Check existing issues and documentation
- Review the CHANGELOG.md for recent changes
- Look at existing code for examples
- Create an issue to discuss ideas before starting major work

---

Thank you for contributing! Your efforts help make this project better. 🙏
