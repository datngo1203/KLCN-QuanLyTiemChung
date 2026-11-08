---
applyTo: "backend/**"
---

# Backend context router

Follow the root `AGENTS.md`, `SCOPE.md`, the assigned/ACTIVE issue, and `backend/AGENTS.md`.

This file is a Copilot compatibility router, not a replacement for backend contracts or nested rules.

- Read only the relevant contract section before changing endpoint/DTO/error semantics.
- Reuse existing entities/source-of-truth; do not create parallel tables/statuses without acceptance.
- API enforces authentication, authorization, ownership, and business invariants.
- Do not alter applied migrations; new schema work must be additive/legacy-safe where possible.
- Do not reset/reseed the demo database to make tests pass.
- Money, inventory, clinical, concurrency, and multi-row writes require transaction/fail-closed behavior appropriate to the current contract.
- Unknown mutation outcomes must remain reconcilable; never make clients rely on blind retries.
- Contract/schema/high-risk mutations require the relevant integration/regression tests.
