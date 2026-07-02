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

**Bare host (fastest):**

```bash
dotnet run --project src/EmployeeData.TeamsBot.Presentation.Bot
```

Put your dev Azure OpenAI key in `src/EmployeeData.TeamsBot.Presentation.Bot/appsettings.Development.json`
(`AzureOpenAI:ApiKey`) - this file is git-ignored. Then point the **Bot Framework Emulator** at
`http://localhost:5234/api/messages` (leave App ID / Password blank). Health: `GET /healthz`.

**Docker Compose (bot + Redis + a WireMock Employee Data API stub):**

```bash
cp .env.example .env      # then set AZURE_OPENAI_API_KEY (git-ignored)
docker compose up --build
```

Bot on `:5234`, Redis on `:6379`, Employee Data API stub on `:8080`. Note: a full conversational turn also
needs Microsoft Graph identity (`Graph:*` app credentials), which is not stubbed locally - without it the bot
starts and serves `/healthz`; end-to-end turn behaviour is covered by the `TestAdapter` tests
(`test/EmployeeData.TeamsBot.Tests/EndToEnd`).
