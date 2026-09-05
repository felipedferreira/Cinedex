# Built from the `frontend/` workspace root, not this directory — the lockfile is hoisted there and
# this app depends on the `@cinedex/frames`, `@cinedex/shots`, `@cinedex/scenes` and
# `@cinedex/theme` workspace packages. compose.yaml sets `context: ./frontend` and points
# `dockerfile` at this file.
FROM node:22-alpine AS build
WORKDIR /app
# Copy EVERY workspace manifest before the sources, so the `npm ci` layer is invalidated exactly
# when some package's dependencies change and stays cached otherwise.
#
# This is a cache-correctness list, not a build-correctness one: `npm ci` here does NOT fail on a
# missing workspace manifest (verified — the image builds with any single one of these lines
# removed, because `COPY . .` below fills the directory in afterwards and the workspace symlink
# then resolves). What a missing line costs is a stale install layer: change that package's
# dependencies and the cached `npm ci` is reused, so the new dependency never lands in the image.
# Keep the list complete — including `apps/docs-site`, which nothing in this image consumes.
#
# `.npmrc` carries `strict-allow-scripts=true`, so it has to land before `npm ci` rather than with
# the `COPY . .` below — otherwise the image would install with lifecycle scripts unguarded. It
# holds config only; never put a token in it.
COPY package.json package-lock.json .npmrc ./
COPY apps/cinedex-app/package.json apps/cinedex-app/
COPY apps/docs-site/package.json apps/docs-site/
COPY apps/storybook/package.json apps/storybook/
COPY packages/frames/package.json packages/frames/
COPY packages/shots/package.json packages/shots/
COPY packages/scenes/package.json packages/scenes/
COPY packages/theme/package.json packages/theme/
RUN npm ci --workspace @cinedex/storybook --include-workspace-root
COPY . .
RUN npm run build -w @cinedex/storybook

FROM nginx:alpine AS final
# Plain HTTP: unlike the SPA image, this container is not the stack's reverse proxy and Storybook
# makes no API calls, so there is nothing to terminate TLS for.
COPY --from=build /app/apps/storybook/storybook-static /usr/share/nginx/html
COPY apps/storybook/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
