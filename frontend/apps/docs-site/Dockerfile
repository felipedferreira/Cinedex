# Built from the repository root, not `frontend/`: the docs site's changelog
# generator reads the root CHANGELOG.md. compose.yaml sets that context and
# points `dockerfile` at this file.
FROM node:22-alpine AS build
WORKDIR /app/frontend

# Copy every workspace manifest before source so dependency changes invalidate
# npm ci while ordinary documentation and source edits reuse it. `.npmrc` must
# be present here because it enforces the install-script allowlist.
COPY frontend/package.json frontend/package-lock.json frontend/.npmrc ./
COPY frontend/apps/cinedex-app/package.json apps/cinedex-app/
COPY frontend/apps/docs-site/package.json apps/docs-site/
COPY frontend/apps/storybook/package.json apps/storybook/
COPY frontend/packages/atoms/package.json packages/atoms/
COPY frontend/packages/compounds/package.json packages/compounds/
COPY frontend/packages/solution/package.json packages/solution/
COPY frontend/packages/theme/package.json packages/theme/
RUN npm ci --workspace @cinedex/docs-site --include-workspace-root

COPY CHANGELOG.md /app/CHANGELOG.md
COPY frontend/ .
ARG DOCUSAURUS_BASE_URL=/documentation/
ENV DOCUSAURUS_BASE_URL=${DOCUSAURUS_BASE_URL}
RUN npm run build -w @cinedex/docs-site

FROM nginx:alpine AS final
COPY --from=build /app/frontend/apps/docs-site/build /usr/share/nginx/html
COPY frontend/apps/docs-site/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
