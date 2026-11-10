# FarmerSample.com

Sample showroom (and later auction marketplace) for graded Sri Lankan spices.
**Verified at origin. Traded worldwide.**

- Architecture: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- Business plan (v0.1): [`docs/BUSINESS-PLAN-v0.1.md`](docs/BUSINESS-PLAN-v0.1.md)
- Brand guidelines: [`docs/brand/BRAND-GUIDELINES.md`](docs/brand/BRAND-GUIDELINES.md)
- Private test site on Azure: [`docs/DEPLOY-TEST.md`](docs/DEPLOY-TEST.md)

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

**M0 (skeleton)** and **M1 (showcase)** are done:

- Farms, farmer consent (signed form as evidence), publish/unpublish, public producer pages
- Sample batches per farm visit, versioned hub grading (corrections need a reason)
- Lots: one product and grade from one or more farms; combined lots get a volume-weighted composite sample that must be graded before publishing
- Public lot pages with the hub grading marked as verified and volume/harvest marked as producer-supplied
- QR codes: `/q/CODE` forwards to the lot or farm page; printable A4 label sheets (3 × 8) in the back office
- Farm photos: originals private; buyers only see a metadata-free WebP copy after a buyer-safe review and with photo consent
- Sample requests per lot or farm, stored and e-mailed to the team

Next (M2, from Feb 2027): buyer accounts and invites, sealed pouches and stock (Sri Lanka hub → EU stock), paid sample orders via Stripe (VSR Partners SL), offline field app.
