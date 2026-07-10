FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final

# Local Compose TLS terminates at Nginx so the backend's __Secure- refresh cookie is
# delivered over HTTPS. This self-signed certificate is for local development only;
# production deployments should mount a real certificate or terminate TLS upstream.
RUN apk add --no-cache --virtual .cert-tools openssl \
    && mkdir -p /etc/nginx/certs \
    && openssl req -x509 -nodes -newkey rsa:2048 -days 3650 \
        -keyout /etc/nginx/certs/cinadex-local.key \
        -out /etc/nginx/certs/cinadex-local.crt \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" \
    && apk del .cert-tools

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 443
