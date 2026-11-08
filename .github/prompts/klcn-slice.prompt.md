---
agent: 'agent'
description: 'Recover KLCN context progressively and work on one assigned slice without reloading the whole project'
---

Work on this KLCN task:

**Task / issue:** ${input:task:Enter the GitHub issue number or task objective}
**Mode:** ${input:mode:Choose audit, implement, or review}

Use the repository as the source of truth. Do not rely on old chat memory.

Recover context progressively:

1. Read root `AGENTS.md`.
2. Read `SCOPE.md` — Current State, Core thesis scope, and queue only.
3. Read the assigned issue/task source.
4. Read the nearest nested `AGENTS.md` for every area the task may touch.
5. Audit the relevant current code/tests/contracts. Read README, broader contracts, closed issues/PRs, or old history only when the audit proves they are needed.

Before changing code, report:
- exact `origin/main` SHA;
- current ACTIVE/NEXT status relevant to this task;
- acceptance you inferred from the issue/repo;
- scope-stop;
- relevant skills available/unavailable;
- files/areas likely affected.

If the task is only NEXT/PARKED and has not been explicitly activated, stop after the audit and report that implementation requires coordinator/user approval.

For implementation mode:
- create/use a feature branch from current main;
- make the smallest cohesive change;
- do not expand adjacent slices;
- run targeted tests, affected regression, and required runtime/visual gates;
- never fabricate data to make a gate pass;
- open a Draft PR;
- return exact base SHA, implementation HEAD, diff summary, tests/build/runtime evidence, UNVERIFIED items, PR URL, and blockers;
- do not merge, close the issue, update SCOPE to start the next slice, or begin another feature.

For review mode:
- review the exact remote PR/head, not a stale local assumption;
- verify diff scope, tests/CI for that exact SHA, and runtime evidence required by the nested rules;
- classify findings as blocker / non-blocking / out-of-scope;
- do not silently implement broad fixes.

STOP after the requested task/handoff.
