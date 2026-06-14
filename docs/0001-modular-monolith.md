# ADR 0001: Modular monolith over microservices

## Status
Accepted

## Context
This is a solo project intended to (a) teach software design and distributed-systems
fundamentals and (b) serve as a portfolio piece. The domain splits naturally into
Users, Wallet, and Generation.

## Decision
Build a **modular monolith**: a single deployable, internally partitioned into modules
with compiler-enforced boundaries (each module exposes a `*.Contracts` assembly; other
modules reference only contracts, never implementations). Each module owns its own
database schema; no cross-module table access.

## Consequences
- One deployable, one CI/CD pipeline, one database — minimal operational overhead.
- Boundaries are enforced by the compiler (reference graph) + `internal` visibility,
  not by convention alone.
- Cross-module calls are in-process: synchronous interface calls for commands that
  need an answer (e.g. `Wallet.Reserve`), in-process events for facts being announced
  (e.g. `GenerationCompleted`).
- If a module ever needs independent scaling/deployment, the clean boundary makes
  extraction into a service feasible later — without paying that cost now.

## Alternatives considered
- **Microservices:** rejected — solves team-scale and independent-deployment problems
  this project does not have; adds distributed-systems overhead with no current benefit.
