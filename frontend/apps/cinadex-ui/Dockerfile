# Built from the `frontend/` workspace root, not this directory — the lockfile
# is hoisted there and the app depends on the `@cinedex/ui` workspace package.
# compose.yaml sets `context: ./frontend` and points `dockerfile` at this file.
FROM node:22-alpine AS build
WORKDIR /app
# Copy every workspace manifest before the sources so the `npm ci` layer stays
# cached until a dependency actually changes.
COPY package.json package-lock.json ./
COPY apps/cinadex-ui/package.json apps/cinadex-ui/
COPY packages/ui/package.json packages/ui/
# Scoped to the app's workspace so the image build skips the Storybook and
# test toolchains that only @cinedex/ui needs. The workspace link to
# @cinedex/ui is still created, and its source is all the app build consumes.
RUN npm ci --workspace cinadex-ui --include-workspace-root
COPY . .
RUN npm run build -w cinadex-ui

FROM nginx:alpine AS final
RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/certs \
    && openssl req -x509 -nodes -newkey rsa:2048 -days 825 \
        -keyout /etc/nginx/certs/localhost.key \
        -out /etc/nginx/certs/localhost.crt \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
COPY --from=build /app/apps/cinadex-ui/dist /usr/share/nginx/html
COPY apps/cinadex-ui/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 443
