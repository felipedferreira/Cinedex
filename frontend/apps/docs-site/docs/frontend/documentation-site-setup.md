---
sidebar_position: 3
---

# Documentation site setup and environment configuration

The Cinedex documentation site is built with [Docusaurus](https://docusaurus.io/) and can be deployed to different environments. This guide covers how to configure environment-specific behavior, particularly the Storybook link.

## Local development

To run the documentation site locally:

```bash
cd frontend
npm run docs-site
```

The site will be available at `http://localhost:9004` and will link to Storybook at `http://localhost:9001` (the default local development Storybook port).

## Docker Compose

When running the full stack with Docker Compose:

```bash
docker compose up --build
```

The documentation site is accessible at `https://localhost:9000/documentation/` through the Caddy edge proxy. The Storybook link will point to `http://localhost:9001`, which is the Storybook service running as a container.

## Production and custom environments

### Storybook base URL

The documentation site includes a link to Storybook in the **Design choices and theme** section. By default, this link points to:

- **Development**: `http://localhost:9001`
- **Production**: `https://cinedex.online/storybook`

To deploy to a different environment and point to a different Storybook URL, set the `STORYBOOK_BASE_URL` build argument when building the Docker image:

```bash
docker build \
  --build-arg STORYBOOK_BASE_URL=https://your-domain.com/storybook \
  -f frontend/apps/docs-site/Dockerfile \
  -t cinedex-docs-site .
```

Or with Docker Compose, update the `cinedex-docs-site` service in `compose.yaml`:

```yaml
cinedex-docs-site:
  build:
    args:
      STORYBOOK_BASE_URL: 'https://your-domain.com/storybook'
```

### Base URL path

The documentation site itself is served from a base path in Docker deployments. By default:

- **Local dev** (`npm run docs-site`): Serves from `/`
- **Docker Compose**: Served from `/documentation/` by Caddy
- **Production**: Set via `DOCUSAURUS_BASE_URL` build argument

For example, in a production Docker build:

```bash
docker build \
  --build-arg DOCUSAURUS_BASE_URL=/docs/ \
  --build-arg STORYBOOK_BASE_URL=https://cinedex.online/storybook \
  -f frontend/apps/docs-site/Dockerfile \
  -t cinedex-docs-site .
```

## Implementation details

The Storybook link is rendered by the `StoryboookLink` component (`src/components/StoryboookLink.tsx`), which reads the URL from the Docusaurus configuration that was set at build time from the `STORYBOOK_BASE_URL` environment variable.

The configuration flow:

1. `STORYBOOK_BASE_URL` environment variable (or Docker build arg) is set
2. Passed to `docusaurus.config.ts` as `process.env.STORYBOOK_BASE_URL`
3. Stored in `siteConfig.customFields.storybookBaseUrl`
4. React component `StoryboookLink` reads and renders the link

This approach allows the link destination to be determined at build time without requiring markdown regeneration or runtime configuration files.

## Changelog page

The `/changelog` page is automatically generated from the repository's root `CHANGELOG.md` file before each build. This is handled by `scripts/sync-changelog.mjs` and requires access to the root `CHANGELOG.md` file, which is why the Docker image build context is the repository root, not the `frontend/` directory.

Edit only the root `CHANGELOG.md` file—never edit `apps/docs-site/src/pages/changelog.md` directly, as it will be overwritten on the next build.
