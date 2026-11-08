---
applyTo: "apps/windows/**,apps/windows.tests/**,apps/windows.ui.tests/**"
---

# Windows/WPF context router

Follow the root `AGENTS.md`, `SCOPE.md`, the assigned/ACTIVE issue, and `apps/windows/AGENTS.md`.

This file exists so Copilot receives the Windows routing rules even on clients where nested `AGENTS.md` discovery may vary. Do not treat it as a second source of truth.

- WPF is the employee client and uses the real API; never read SQL directly.
- Prefer existing `ViewsV2/`, `Themes/`, and `CE.*` components/tokens; avoid unrelated shell redesign.
- Do not expose raw enum/object/provider/protocol error details to users.
- Preserve reconcile-before-retry behavior for unknown mutations.
- Run affected Windows tests, UI/render tests, and Release build.
- UI/flow changes require native/full-shell verification for the affected states and keyboard/resize behavior. If native runtime is unavailable, report `UNVERIFIED`; do not infer PASS from build/CI.
