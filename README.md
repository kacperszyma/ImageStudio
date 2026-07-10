# ImageStudio

A prompt-to-image generation platform built as a **modular monolith** in C# / .NET 10.
Users buy credits, submit prompts, and choose from several inference models; generations
run through an asynchronous job pipeline with provider-agnostic adapters and real-time
status updates over WebSockets.

> Built in public as a study in software design (deep modules, clean boundaries) and
> distributed-systems fundamentals (async job processing, idempotency, at-least-once
> delivery, transactional correctness, the saga/outbox pattern).

## Status

🚧 Early development. Following a ~15%-upfront design approach
(*A Philosophy of Software Design*, Ousterhout).

## Architecture at a glance

A single deployable, partitioned into modules with compiler-enforced boundaries.
Each module owns its own database schema and exposes a narrow public contract via a
`*.Contracts` assembly; other modules depend only on those contracts, never on
internals.

| Module | Responsibility | Key interface |
|---|---|---|
| **Users** | Identity, app-side user record | `IUsers` |
| **Wallet** | Credit balance, Stripe purchases, reserve/settle/release | `IWallet` |
| **Generation** | Async job pipeline, provider adapters, blob storage | `submit → jobId`, `getStatus` |

The deep module is **Generation**: a narrow interface hiding a queue, workers, retries,
idempotency, provider translation, and storage — callers never see any of that
complexity.

### Key design decisions

See [`docs/`](./docs) for architecture decision records (ADRs), including:
- Why a modular monolith over microservices
- Credit **reserve-and-settle** (not charge-on-success) and why it forces idempotency
- Why the job queue starts as a Postgres table before a real broker
- The provider/capability adapter boundary (fal.ai today, self-hosted PyTorch later)
- The saga/outbox pattern used to keep generation state and side effects consistent
  across partial failures

## Tech stack

- **Backend:** C# / .NET 10, ASP.NET Core, EF Core (one `DbContext` per module),
  SignalR for real-time job status
- **Database:** PostgreSQL (OLTP, system of record)
- **Payments:** Stripe (checkout + webhooks)
- **Frontend:** React 19 + Vite, TypeScript, TanStack Query, Tailwind CSS, shadcn/ui
- **Auth:** Auth0 / Entra External ID (JWT bearer)
- **Inference:** fal.ai adapters, with a local mock provider for offline development
- **Observability:** OpenTelemetry → Prometheus, Loki, Tempo, visualized in Grafana
- **CI/CD:** Dagger (Python SDK) — identical pipeline locally and in CI, builds and
  publishes container images to Google Artifact Registry for deployment to Cloud Run
- **Testing:** xUnit, FluentAssertions, Testcontainers (real Postgres for integration
  tests), `WebApplicationFactory` for end-to-end API tests

## Repository layout

```
backend/      C# solution — Api host + module projects + tests
frontend/     React app
.dagger/      CI/CD pipeline (build, test, publish, deploy) as code
docs/         Architecture decision records
observability/ Prometheus, Loki, Tempo, otel-collector config
```

## Local development

Code runs **natively**; only backing services (Postgres, observability stack) run in
containers.

```bash
make dev          # Postgres + backend (:5253) + frontend (:5173), live logs
make fulldev       # dev + local fal.ai mock + observability stack (Grafana on :3000)
make down          # stop backing services
```

See the [Makefile](./Makefile) for the full list of targets, including CI/CD image
publishing via Dagger.

## License

MIT — see [LICENSE](./LICENSE).
