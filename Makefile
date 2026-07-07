# ImageStudio — local development.
#
# Code runs NATIVELY (hot reload); only backing services run in containers.
# Dagger is container-only and owns CI/CD instead — see .dagger/.
#
#   make dev             Postgres + backend (:5253) + frontend (:5173), live logs
#   make fulldev         dev + fake-fal (:8080) + observability stack — heavier, optional
#   make fake-fal        local SD1.5 inference stub (:8080) — heavy, optional
#   make observability   Prometheus/Grafana/Tempo/Loki/otel-collector only
#   make down            stop the backing services
#
# Ctrl-C on `make dev`/`make fulldev` tears down backend + frontend; `make down` stops everything.
#
#   make deploy-backend REGISTRY_ADDRESS=...       push the Api image to Artifact Registry
#   make deploy-otel-sidecar OTEL_REGISTRY_ADDRESS=...  push the otel-collector sidecar image
#
# Both deploy targets run `gcloud auth login` first — a fresh browser login every
# time, no long-lived service-account key involved.

API_PROJECT := backend/src/Api
FRONTEND_DIR := frontend
FAKE_FAL_DIR := tools/fake-fal
CONDA_ENV := wma
OBSERVABILITY_SERVICES := prometheus grafana tempo otel-collector loki

# Override on the command line, e.g.:
#   make deploy-backend REGISTRY_ADDRESS=us-central1-docker.pkg.dev/my-proj/imagestudio/api:$(git rev-parse --short HEAD)
REGISTRY_ADDRESS ?=
OTEL_REGISTRY_ADDRESS ?=

.PHONY: dev fulldev backend frontend db wait-db observability down install fake-fal stripe-listen deploy-backend deploy-otel-sidecar

## Run the full native dev stack (Postgres in a container, app processes native).
dev: db wait-db
	@echo "→ backend on :5253, frontend on :5173 (Ctrl-C to stop all)"
	@trap 'kill 0' EXIT INT TERM; \
		$(MAKE) --no-print-directory backend & \
		$(MAKE) --no-print-directory frontend & \
		$(MAKE) --no-print-directory stripe-listen & \
		wait

## Same as dev, plus fake-fal (:8080) and the observability stack (Grafana on :3000).
fulldev: db wait-db observability
	@echo "→ backend on :5253, frontend on :5173, fake-fal on :8080, grafana on :3000 (Ctrl-C to stop all)"
	@trap 'kill 0' EXIT INT TERM; \
		$(MAKE) --no-print-directory backend & \
		$(MAKE) --no-print-directory frontend & \
		$(MAKE) --no-print-directory fake-fal & \
		$(MAKE) --no-print-directory stripe-listen & \
		wait

## Backend API. Secrets come from the root .env, exported into the process
## environment here — the app itself no longer loads .env (that only made sense
## for local dev; in prod these vars are set by whatever publishes the container).
backend:
	set -a; . ./.env; set +a; dotnet run --project $(API_PROJECT)

## Frontend dev server (Vite). Installs deps on first run.
frontend:
	@[ -d $(FRONTEND_DIR)/node_modules ] || (cd $(FRONTEND_DIR) && npm ci)
	cd $(FRONTEND_DIR) && npm run dev

## Backing services only — mirrors the README's `docker compose up -d`.
db:
	docker compose up -d postgres

## Block until Postgres accepts connections so the backend doesn't race it.
wait-db:
	@echo "→ waiting for Postgres…"
	@until docker compose exec -T postgres pg_isready -U postgres >/dev/null 2>&1; do sleep 0.5; done
	@echo "→ Postgres ready"

## Prometheus + Grafana + Tempo + Loki + otel-collector. The backend exports
## metrics via OTLP to the collector on localhost:4317 whenever it's up —
## no app config needed, it just has nothing to send to otherwise.
observability:
	docker compose up -d $(OBSERVABILITY_SERVICES)

## Install all local dependencies up front (optional; dev does this lazily).
install:
	dotnet restore backend/ImageStudio.slnx
	cd $(FRONTEND_DIR) && npm ci

## Local fal.ai stand-in (SD1.5). First run downloads ~4GB of weights.
## Runs in the `wma` conda env (deps live there); `conda run` avoids needing
## an interactive `conda activate` inside the recipe's one-shot shell.
## Point the backend at it via .env (FAL_QUEUE_URL/FAL_JWKS_URL) — see tools/fake-fal.
fake-fal:
	cd $(FAKE_FAL_DIR) && conda run -n $(CONDA_ENV) --no-capture-output python fake_fal.py

## Forward Stripe webhooks to the local backend. The signing secret printed on
## first run must match STRIPE_WEBHOOK_SECRET in .env.
stripe-listen:
	stripe listen --forward-to localhost:5253/stripe/webhook

## Stop the backing services.
down:
	docker compose down

## Push the backend image to Artifact Registry. `gcloud auth login` runs every
## time (no cached ADC, no service-account key on disk) so the push is always
## tied to a human who just approved it in the browser.
deploy-backend:
	@test -n "$(REGISTRY_ADDRESS)" || (echo "set REGISTRY_ADDRESS=<region>-docker.pkg.dev/<project>/<repo>/api:<tag>"; exit 1)
	gcloud auth login --brief
	dagger call deploy-backend --registry-address=$(REGISTRY_ADDRESS) --gcp-token=cmd:"gcloud auth print-access-token"

## Push the otel-collector sidecar image (our config baked in) to Artifact Registry.
deploy-otel-sidecar:
	@test -n "$(OTEL_REGISTRY_ADDRESS)" || (echo "set OTEL_REGISTRY_ADDRESS=<region>-docker.pkg.dev/<project>/<repo>/otel-collector:<tag>"; exit 1)
	gcloud auth login --brief
	dagger call publish-otel-sidecar --registry-address=$(OTEL_REGISTRY_ADDRESS) --gcp-token=cmd:"gcloud auth print-access-token"
