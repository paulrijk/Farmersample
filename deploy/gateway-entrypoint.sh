#!/bin/sh
# Starts the gateway. When SITE_PASSWORD is set, the whole site (pages, QR links and API) asks for
# a user name and password first, and search engines are told not to index it.
set -eu
rm -f /etc/caddy/auth.caddy
if [ -n "${SITE_PASSWORD:-}" ]; then
  HASH=$(caddy hash-password --plaintext "$SITE_PASSWORD")
  {
    echo "basic_auth {"
    echo "	${SITE_USER:-farmersample} $HASH"
    echo "}"
    echo 'header X-Robots-Tag "noindex, nofollow"'
  } > /etc/caddy/auth.caddy
  echo "Site password is ON (user: ${SITE_USER:-farmersample})"
fi
unset SITE_PASSWORD
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile
