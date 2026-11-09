# FarmerSample.com

Sample showroom (and later auction marketplace) for graded Sri Lankan spices.
**Verified at origin. Traded worldwide.**

- Architecture: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- Business plan (v0.1): [`docs/BUSINESS-PLAN-v0.1.md`](docs/BUSINESS-PLAN-v0.1.md)
- Brand guidelines: [`docs/brand/BRAND-GUIDELINES.md`](docs/brand/BRAND-GUIDELINES.md)

## Run it on your laptop (demo mode — needs only Docker Desktop)

```bash
git clone https://github.com/paulrijk/farmersample.git
cd farmersample
docker compose -f deploy/docker-compose.yml --profile app up --build -d
```

The first build takes a few minutes. Then open:

| What | Where |
|------|-------|
| Public site | http://localhost:8080 |
| Back office | http://localhost:8080/admin |
| E-mail inbox (Mailpit) | http://localhost:8025 |
| Telemetry dashboard | http://localhost:18888 |

Demo logins (fictional, local only): `admin@farmersample.local` / `FarmerSample-local-1`
(also `qc@…` and `field@…` with the same password).

Stop: `docker compose -f deploy/docker-compose.yml --profile app down` · Wipe all data: add `-v`.

All farms in the demo data are **fictional**. Never commit real farmer data.

## Developer mode (hot reload)

Needs .NET 10 SDK and Node.js 22 LTS as well.

```bash
npm run setup     # once
npm run infra     # Postgres, Azurite, Mailpit, telemetry dashboard
npm run db        # migrations + demo data (prints logins)
npm run api       # API on http://localhost:5080
npm run worker    # e-mails (outbox)
npm run web       # public site http://localhost:4200, back office http://localhost:4201
npm test          # unit, architecture and integration tests (Docker must be running)
```

Two-factor sign-in for staff is required everywhere except local development (`Auth:RequireStaffMfa`).

## Showing QR pages on a phone (EDB demo)

QR codes point to `PUBLIC_BASE_URL`. For a phone to reach your laptop, start a tunnel:

```bash
cloudflared tunnel --url http://localhost:8080
```

and set `PUBLIC_BASE_URL` in `.env` to the printed https URL, then `npm run demo` again.

## Layout

```
src/FarmerSample.Api         ASP.NET Core API (public, auth, staff endpoints)
src/FarmerSample.Worker      outbox processor (e-mails)
src/FarmerSample.DbMigrator  migrations + seed
src/FarmerSample.Core        modules (Identity, Admin, SupplyQuality, Catalogue), persistence, shared kernel
tests/                       unit, architecture (module boundaries), integration (Testcontainers + privacy canary)
web/                         Angular: buyer-web (public site), backoffice (/admin), shared brand styles
deploy/                      docker-compose, Dockerfiles, Caddy gateway
```

## Status

Milestone **M0 (skeleton)** is done plus the start of **M1**: farms, consent, publishing, public producer pages with QR URLs,
sample requests with staff e-mail. Next in M1: sample batches and gradings, lots, photos with buyer-safe approval.
