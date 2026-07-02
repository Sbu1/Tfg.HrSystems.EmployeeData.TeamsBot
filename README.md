# Tfg.HrSystems.EmployeeData.TeamsBot

A conversational Microsoft Teams assistant that lets TFG Infotech staff see their in-office hours
progress toward the 100-hour monthly goal, view history and anonymised peer standing, log motivations
for shortfall months, and lets managers see their team's standing and who is at risk — all backed by
the existing `Tfg.HrSystems.EmployeeData` API.

## Specifications

- Product spec: [`docs/specs/base/in-office-assistant/product-spec.md`](docs/specs/base/in-office-assistant/product-spec.md)
- Technical spec: [`docs/specs/base/in-office-assistant/technical-spec.md`](docs/specs/base/in-office-assistant/technical-spec.md)

> Spec-driven build: the signed-off product spec is the source of truth for scope and requirements;
> the technical spec covers architecture and implementation. No application code is written until the
> technical spec is signed off.

Work-item tracker: [`docs/specs/base/in-office-assistant/work-breakdown.md`](docs/specs/base/in-office-assistant/work-breakdown.md)

## Build & test

```bash
dotnet build Tfg.HrSystems.EmployeeData.TeamsBot.sln
dotnet test test/EmployeeData.TeamsBot.Tests
```

## Run locally

The bot talks to the **real** Employee Data API (test environment - the default `EmployeeApi:BaseUrl` in
`appsettings.json`) and Azure OpenAI. Put your dev values in
`src/EmployeeData.TeamsBot.Presentation.Bot/appsettings.Development.json` (git-ignored) and run:

```bash
dotnet run --project src/EmployeeData.TeamsBot.Presentation.Bot
```

```jsonc
// appsettings.Development.json
{
  "AzureOpenAI": { "ApiKey": "<your dev key>" },
  "Graph": { "DevEmployeeNumber": 123456 }   // dev-only: resolves the caller without Graph identity
}
```

`Graph:DevEmployeeNumber` is a dev/demo shortcut - set it to a real employee number in the test data so a caller
resolves without Microsoft Graph (which needs app credentials + TOQ-04). Then connect the **Bot Framework
Emulator** to `http://localhost:5234/api/messages` (blank App ID / Password). Health: `GET /healthz`.

Optional local Redis (used once F3-S4 state lands): `docker compose -f src/docker-compose.yml up -d`.

## Container image (CI / deploy)

`build/docker/Dockerfile` runs a published output (runtime-only image, mirroring the Transaction Backend convention):

```bash
dotnet publish src/EmployeeData.TeamsBot.Presentation.Bot -c Release -o build/docker/publish
docker build -t teamsbot build/docker
```
