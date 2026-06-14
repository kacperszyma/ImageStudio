# ImageStudio

A prompt-to-image generation platform built as a **modular monolith** in C# / .NET 10.
Users buy tokens, submit prompts, and pick from several inference models; generations
run through an asynchronous job pipeline with provider-agnostic adapters.

> Built in public as a study in software design (deep modules, clean boundaries) and
> distributed-systems fundamentals (async job processing, idempotency, at-least-once
> delivery, transactional correctness).

## Status

🚧 Early development. Following a ~15%-upfront design approach
(*A Philosophy of Software Design*, Ousterhout).

## Architecture at a glance

A single deployable, partitioned into modules with compiler-enforced boundaries.
Each module owns its own schema and exposes a narrow public contract; modules depend
only on each other's `*.Contracts`, never on internals.

| Module | Responsibility | Key interface |
|---|---|---|
| **Users** | Identity, app-side user record | `IUsers` |
| **Wallet** | Token balance, Stripe purchases, reserve/settle/release | `IWallet` |
| **Generation** | Async job pipeline, provider adapters, blob storage | `submit → jobId`, `getStatus` |

The deep module is **Generation**: a narrow interface hiding a queue, workers, retries,
idempotency, provider translation, and storage.

### Key design decisions
See [`docs/`](./docs) for architecture decision records (ADRs), including:
- Why a modular monolith over microservices
- Token **reserve-and-settle** (not charge-on-success) and why it forces idempotency
- Why the job queue starts as a Postgres table before a real broker
- Provider/capability adapter boundary (fal.ai today, self-hosted PyTorch later)

## Tech stack

- **Backend:** C# / .NET 10, ASP.NET Core, EF Core (one DbContext per module)
- **Database:** PostgreSQL (OLTP, system of record)
- **Frontend:** React + Vite, shadcn/ui
- **Infra (Azure):** Container Apps, PostgreSQL Flexible Server, Blob Storage,
  Entra External ID, (Service Bus later)
- **Observability:** OpenTelemetry → Application Insights
- **Testing:** xUnit, Testcontainers (real Postgres), WebApplicationFactory

## Repository layout

```
backend/      C# solution — Api host + module projects + tests
frontend/     React app
infra/        Dockerfiles, IaC, deploy config
docs/         Architecture decision records + diagrams
```

## Local development

Code runs **natively**; only backing services run in containers.

```bash
docker compose up -d        # Postgres + Azurite only
cd backend && dotnet run --project src/Api
cd frontend && npm ci && npm run dev
```

## License

MIT — see [LICENSE](./LICENSE).
