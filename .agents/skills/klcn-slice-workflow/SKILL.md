---
name: klcn-slice-workflow
description: "Use in the KLCN-QuanLyTiemChung repository when implementing, fixing, or handing off exactly one ACTIVE feature slice with Codex. Minimize repeated repository reads and test loops while preserving AGENTS.md, SCOPE.md, medical accuracy, authorization and money/inventory/clinical regression gates. Do not use for unrelated requests or coordinator-only PR reviews."
---

# KLCN — One active slice, minimal context

This is a lightweight workflow, **not** a new authority or a test-skip shortcut. If any rule conflicts, follow `AGENTS.md`, `SCOPE.md`, the active issue, and the approved source material in that order. Codex implements; the ChatGPT coordinator reviews and merges. Do not spawn planning/review subagents just to run this skill.

## 1. Resume without re-auditing

1. Run `git status --short --branch` and `git fetch origin main`; preserve uncommitted work. Never reset, clean, or switch branches over user work.
2. Read **only** `AGENTS.md`, the relevant part of `SCOPE.md`, and the one ACTIVE issue. If resuming a PR, compare its exact remote HEAD and diff; continue there instead of creating another branch.
3. Read `backend/contracts/README.md` only if the slice touches contracts. Read the thesis/source only for business facts actually relevant to the acceptance criteria.
4. State in at most five brief lines: ACTIVE issue, acceptance, affected files, forbidden scope, and validation. If already unambiguous, proceed directly.

## 2. Keep work narrow

- Use path-restricted `rg`, file reads, and diffs. Avoid listing the entire repository, unrelated PR histories, and global research loops.
- Implement the smallest **complete vertical slice**. Don't split a coherent feature into endless prompts, and don't implement the next slice before coordinator closeout.
- Preserve existing contracts and completed modules. Do not alter medical/vaccine facts without trustworthy source evidence; mark unsupported facts `CẦN BỔ SUNG`.
- For UI work follow the existing AGENTS skill gate; use `baoyu-design` if available and relevant. Use Ponytail if available and relevant. Do not auto-install or activate unrelated skills.
- Do not create another status file, architecture document, matrix or review artifact when the existing issue/PR/SCOPE can carry the state.

## 3. Usage-aware tests, never weaker gates

- While editing, run the **narrowest relevant tests**. Do not rerun the entire solution after every one-line edit.
- After targeted tests pass, run full affected-area regression and required builds **once on the final candidate**; rerun only the failing subset until fixed, then verify the final candidate and allow CI to cover the full workflow.
- Cross-DB/API/Web/WPF changes require contract and client regression; auth/ownership, money, stock, and clinical actions retain all relevant negative, race, rollback and validation tests. Don't trade away correctness for lower usage.
- For visual changes, perform the applicable real UI/browser checks if the environment allows it. Mark any user-authorized deferred manual smoke as **UNVERIFIED** with the exact scope; never report deferred/skipped tests as PASS.
- Do not perform repeated overlapping comprehensive code reviews: the coordinator reviews the exact GitHub HEAD after handoff.

## 4. Draft PR + short handoff

- Work on one feature branch based on the appropriate latest main; avoid modifying another active session's files.
- Push a Draft PR, don't merge or close issues. Give one concise handoff:
  - issue, base/branch, exact remote HEAD, PR URL;
  - implementation outcome and any schema/contract changes;
  - targeted/full tests, skips, builds, visual/manual gate, CI run/status;
  - blockers, worktree status; **STOP for coordinator review**.
- If context is exhausted or work stops early: save a brief comment to the ACTIVE issue/PR with exact head, DONE, NEXT, tests and blockers, then stop. Do not repeat a broad audit next session.

## Invocation

Use this skill explicitly as `$klcn-slice-workflow` for the next new KLCN Codex session. It does not change the model/effort setting automatically. Select model/effort in the Codex UI according to the risk; keep High for unresolved auth/clinical/money/inventory logic and prefer Medium for straightforward implementation and fixtures.
