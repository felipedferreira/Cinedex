# Built from the `frontend/` workspace root, not this directory — the lockfile
# is hoisted there and the app depends on the `@cinedex/components` workspace package.
# compose.yaml sets `context: ./frontend` and points `dockerfile` at this file.
FROM node:22-alpine AS build
WORKDIR /app
# Copy every workspace manifest before the sources so the `npm ci` layer stays
# cached until a dependency actually changes. All of them are required even
# though the install is scoped below: `npm ci` validates the lockfile against
# the whole workspace, so a missing manifest fails the build.
#
# `.npmrc` carries `strict-allow-scripts=true`, so it has to land before `npm ci`
# rather than with the `COPY . .` below — otherwise the image would install with
# lifecycle scripts unguarded. It holds config only; never put a token in it.
COPY package.json package-lock.json .npmrc ./
COPY apps/cinadex-app/package.json apps/cinadex-app/
COPY apps/storybook/package.json apps/storybook/
COPY packages/components/package.json packages/components/
# Scoped to the app's workspace so the image build skips the Storybook and
# test toolchains that only @cinedex/components needs. The workspace link to
# @cinedex/components is still created, and its source is all the app build consumes.
RUN npm ci --workspace cinadex-app --include-workspace-root
COPY . .
RUN npm run build -w cinadex-app

FROM nginx:alpine AS final
RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/certs \
    && openssl req -x509 -nodes -newkey rsa:2048 -days 825 \
        -keyout /etc/nginx/certs/localhost.key \
        -out /etc/nginx/certs/localhost.crt \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
COPY --from=build /app/apps/cinadex-app/dist /usr/share/nginx/html
COPY apps/cinadex-app/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 443
