# Contributing to Movies

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the Movies project.

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

Run everything from the repository root:

```bash
docker compose up --build
```

- **UI:** http://localhost:9000
- **API:** http://localhost:8080
- **PostgreSQL:** localhost:5432

#### Option B — Local development

**Backend** (all `dotnet` commands run from `backend/`):

1. Clone the repository and move into the backend folder:
   ```bash
   git clone https://github.com/felipedferreira/Movies.git
   cd Movies/backend
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

**Frontend** (commands run from `frontend/cinadex-ui/`):

1. Install dependencies:
   ```bash
   npm ci
   ```

2. Start the dev server (http://localhost:9000):
   ```bash
   npm run dev
   ```

3. Run tests:
   ```bash
   npm test
   ```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Branch naming convention:
- `feature/` - New features
- `bugfix/` - Bug fixes
- `chore/` - Maintenance and refactoring
- `docs/` - Documentation updates

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

**Frontend** (from `frontend/cinadex-ui/`):

```bash
npm run lint
npm run format:check
npm run test:run
```

Your IDE should automatically enforce EditorConfig rules (backend) and Prettier/ESLint rules (frontend). Most formatting issues can be fixed automatically with `npm run lint:fix` and `npm run format`.

### 4. Commit and Push

```bash
git add .
git commit -m "feat: add new feature description"
git push origin feature/your-feature-name
```

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
  <Version>0.4.0</Version>
  <AssemblyVersion>0.4.0</AssemblyVersion>
  <FileVersion>0.4.0</FileVersion>
  <InformationalVersion>0.4.0</InformationalVersion>
</PropertyGroup>
```

Update all four properties together for consistency.

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
- Follow the pattern: `When<Action>_<ExpectedResult>`
- Example: `GetMovie_WithUnknownId_ReturnsNotFound`
- Tests should be isolated and deterministic

### Documentation

- Write XML documentation for public members
- Keep README.md up to date
- Update CHANGELOG.md for significant changes
- Document API endpoints and their behavior

## Middleware and Exception Handling

### ExceptionHandlingMiddleware

- Catches all unhandled exceptions
- Returns RFC 7807 Problem Details responses
- Includes trace ID for correlation with logs
- Logs errors with correlation ID for debugging

### CorrelationIdMiddleware

- Generates or passes through correlation IDs
- Helps track requests through the system
- Included in error responses for correlation

## Build and CI/CD

### GitHub Actions

The project has automated CI/CD configured in `.github/workflows/build-and-test.yml`:

- Runs on every push to main
- Runs on all pull requests
- Builds the solution
- Runs all tests
- Fails if tests don't pass

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
dotnet test --filter "ExceptionHandlingMiddlewareTests"
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
