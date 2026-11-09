# Builds the Angular apps and serves them with Caddy, which also proxies /api to the API.
FROM node:22 AS build
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npx ng build buyer-web --configuration production \
 && npx ng build backoffice --configuration production --base-href /admin/

FROM caddy:2
COPY deploy/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /web/dist/buyer-web/browser /srv/buyer-web
COPY --from=build /web/dist/backoffice/browser /srv/backoffice
