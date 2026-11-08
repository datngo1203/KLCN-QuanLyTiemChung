---
applyTo: "apps/web/**"
---

# Customer Web context router

Follow the root `AGENTS.md`, `SCOPE.md`, the assigned/ACTIVE issue, and `apps/web/AGENTS.md`.

This file is a Copilot compatibility router, not an independent feature specification.

- Web is customer-facing React and uses the real API; never read SQL directly or add sample fallback to simulate completion.
- Keep customer preferences distinct from center-confirmed business state when the active contract requires it.
- Do not derive security/business permissions from UI state.
- Do not expose raw enum/provider/internal error details.
- Reuse current design/components before introducing new abstractions or dependencies.
- Run relevant component/unit checks plus lint/build.
- UI changes require browser smoke at appropriate desktop/mobile viewports. If browser/runtime verification cannot run, report `UNVERIFIED`.
