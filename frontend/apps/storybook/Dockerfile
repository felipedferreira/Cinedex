# Built from the `frontend/` workspace root, not this directory — the lockfile is hoisted there and
# this app depends on the `@cinedex/components` workspace package. compose.yaml sets `context: ./frontend`
# and points `dockerfile` at this file.
FROM node:22-alpine AS build
WORKDIR /app
# Copy every workspace manifest before the sources so the `npm ci` layer stays cached until a
# dependency actually changes. All four are required even though the install is scoped below:
# `npm ci` validates the lockfile against the whole workspace.
#
# `.npmrc` carries `strict-allow-scripts=true`, so it has to land before `npm ci` rather than with
# the `COPY . .` below — otherwise the image would install with lifecycle scripts unguarded. It
# holds config only; never put a token in it.
COPY package.json package-lock.json .npmrc ./
COPY apps/cinadex-app/package.json apps/cinadex-app/
COPY apps/storybook/package.json apps/storybook/
COPY packages/components/package.json packages/components/
RUN npm ci --workspace @cinedex/storybook --include-workspace-root
COPY . .
RUN npm run build-storybook -w @cinedex/storybook

FROM nginx:alpine AS final
# Plain HTTP: unlike the SPA image, this container is not the stack's reverse proxy and Storybook
# makes no API calls, so there is nothing to terminate TLS for.
COPY --from=build /app/apps/storybook/storybook-static /usr/share/nginx/html
COPY apps/storybook/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
