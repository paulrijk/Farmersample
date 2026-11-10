# Test site on Azure (behind a shared password)

A private test version of FarmerSample for the EDB, Saj and invited buyers. Everything, including QR links,
asks for one shared user name and password first, and search engines are told not to index it.

## What gets created

| Resource | Size | Purpose |
|----------|------|---------|
| Container App `farmersample` | 1 vCPU / 2 GB, one replica | Gateway (password wall), API and worker in one app |
| Container Apps job `farmersample-migrator` | runs on demand | Database migrations and demo data |
| PostgreSQL Flexible Server | Burstable B1ms, 32 GB, 7-day backups | Database |
| Storage account | Standard LRS, private | Farm photos |
| Container Registry | Basic | Images, built in Azure |
| Log Analytics | 30 days | Logs |

All in one resource group (`farmersample-test`) in an EU region. Deleting the resource group removes everything.

## First-time setup (about 20 minutes, mostly waiting)

You need the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and rights to create resources
in the subscription.

```powershell
cd C:\Users\paul\farmersample
az login
az account list -o table          # find the subscription to use
./deploy/azure/setup.ps1 -SubscriptionId <subscription id> -ConnectGitHub
```

The script asks for two passwords:

- **Site password**: the shared one you give to visitors (user name `farmersample`).
- **Back-office password**: for `admin@`, `qc@` and `field@farmersample.local`. At first sign-in each account must
  set up two-factor sign-in with an authenticator app. Sign in to all three yourself straight away, or they stay
  open to whoever knows both passwords.

It then prints the site address. With `-ConnectGitHub` it also prints five values to add as repository
**variables** in GitHub; after that every push to `main` updates the test site (workflow "Deploy test site").

Options: `-Location spaincentral` (default `westeurope`), `-CustomDomain test.farmersample.com`,
`-ResourceGroup <name>`.

## Good to know

- **Changing a password**: run the script again with the new password.
- **E-mail** is off until a mail server is set (`smtpHost` etc. in `main.bicep`). Sample requests are still stored
  and visible in the back office.
- **QR labels** printed from the test site point to the test site address.
- **Real farmer data**: only farms with signed consent. Settle who is responsible for farmer data
  (architecture, open question 5) before adding real farms.
- **Not production**: one replica, database reachable from Azure services (password protected), one shared
  visitor password. Production gets its own setup (architecture §13).
- **Stripe webhooks** (M2) will need an exception to the password wall for `/api/v1/webhooks/stripe`.

## Removing it

```powershell
az group delete --name farmersample-test
```
