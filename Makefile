# ImageStudio — local development.
#
# Code runs NATIVELY (hot reload); only backing services run in containers.
# Dagger is container-only and owns CI/CD instead — see .dagger/.
#
#   make dev        Postgres + backend (:5253) + frontend (:5173), live logs
#   make fake-fal   local SD1.5 inference stub (:8080) — heavy, optional
#   make down       stop the backing services
#
# Ctrl-C on `make dev` tears down backend + frontend; `make down` stops Postgres.

API_PROJECT := backend/src/Api
FRONTEND_DIR := frontend
FAKE_FAL_DIR := tools/fake-fal
CONDA_ENV := wma

.PHONY: dev backend frontend db wait-db down install fake-fal

## Run the full native dev stack (Postgres in a container, app processes native).
dev: db wait-db
	@echo "→ backend on :5253, frontend on :5173 (Ctrl-C to stop both)"
	@trap 'kill 0' EXIT INT TERM; \
		$(MAKE) --no-print-directory backend & \
		$(MAKE) --no-print-directory frontend & \
		wait

## Backend API (reads secrets from the root .env via DotNetEnv).
backend:
	dotnet run --project $(API_PROJECT)

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

## Stop the backing services.
down:
	docker compose down
