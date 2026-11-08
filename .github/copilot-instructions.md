# KLCN090 — Copilot context router

This file is a **compatibility/context router** for GitHub Copilot. It does not replace the repository source of truth.

## Source of truth

For every substantial task, recover context in this order:

1. Read the root `AGENTS.md`.
2. Read `SCOPE.md`, focusing on **Current State**, **Core thesis scope**, and the queue.
3. Read the assigned/ACTIVE GitHub issue.
4. Read the nearest nested `AGENTS.md` for every code area that may be changed.
5. Read only the code, tests, contracts, or runtime docs required by the audit.

If repository state conflicts with chat history or an old prompt, the latest repository state wins.

Do **not** load the whole project history by default. Do not read all closed issues/PRs, all contracts, or the entire README unless the current task requires them.

## Scope discipline

- Only one business/refinement slice may be ACTIVE at a time.
- Do not promote NEXT or PARKED work to ACTIVE without explicit user/coordinator approval.
- Do not silently expand a task into adjacent modules.
- Keep a clear scope-stop and report out-of-scope discoveries instead of implementing them.
- Never fabricate sample/fallback/business/medical data to make a feature or runtime gate appear complete.

## Workflow

For substantial implementation work:

`audit current main → lock acceptance/scope-stop → feature branch → implement smallest cohesive slice → targeted tests → affected regression → runtime/visual evidence when required → Draft PR → exact-head handoff → STOP`

The coding agent must not merge the PR, close the issue, activate the next slice, or rewrite `SCOPE.md` to start new business work unless explicitly instructed by the coordinator.

Always report exact base/head SHAs when handing off a non-trivial change.

## Project invariants

- Architecture: React customer Web + WPF employee Windows → ASP.NET Core API → SQL Server.
- API is the authority for authentication, authorization, money, inventory, clinical validation, and business mutations.
- Web/Windows must not access SQL directly.
- Do not invent medical eligibility, regimens, contraindications, clinical outcomes, VNVC internal processes, prices, permissions, or real-world operational data.
- Unknown mutation outcome must reconcile current server state before retry; never blind replay.
- Do not reset/reseed/delete the demo database merely to make tests or screenshots pass.
- Never commit secrets or real credentials.

## Agent/tooling behavior

- Check available repository skills before substantial coding/review work and follow the relevant skill instructions.
- Use Ponytail for code/refactor/review when available and appropriate.
- For UI tasks, follow the nested UI rules and available UI skill instructions.
- Build/CI is not equivalent to runtime UI verification.
- Any gate that could not be run must be reported as `UNVERIFIED`.

Keep prompts and context small. Prefer reading authoritative repository files over pasting their contents into chat.
