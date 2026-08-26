# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

This repo is currently **docs/planning-only** — there is no application code checked in. An earlier commit scaffolded an Angular `client/` and Node/Express `server/` (Module 1: Authentication), but the most recent commit deliberately removed both. Going forward, the server is being rebuilt in **.NET C#** instead of Node/Express — when code is (re)built, expect a `client/` (Angular) + `server/` (.NET C#) split; the Node/Express/Prisma scaffold is historical reference only, not a template to follow for the server.

The repo's real content today is a structured spec pipeline: a BRD, a module breakdown, and a defined multi-agent workflow for turning each module into planned, verified, reviewed code.

## What this project is

A **single-user, single-clinic, web-based Patient Management Application** for a general physician (Phase 1 scope — see `BRD\Doc_BRD_Final.md`). Core loop: login → find/register patient → schedule appointment → run consultation (vitals/complaints/diagnosis) → prescribe → print prescription → review history. Explicitly out of scope for Phase 1: multi-user/receptionist access, billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, analytics dashboards, multi-doctor/clinic support, reminders, audit logging.

`BRD\Doc_BRD_Final.md` is the authoritative source of requirements — treat `BRD\Doc_BRD.md` as superseded. Do not plan or implement anything in the BRD's "Out of Scope" list without an explicit scope-change conversation.

## Module structure

The application is decomposed into 10 modules in `Modules\Application_Module_Breakdown.md` (and one detail file per module, `Modules\NN_*.md`), with a strict dependency order:

1. Authentication & Authorization (foundational, no dependencies)
2. Patient Management (anchor entity)
3. Appointment Management
4. Consultation & Clinical Records (EMR core)
5. Prescription & Medication Management
6. Patient History
7. Search & Navigation
8. Data Export

Modules 9 (Data Backup & Reliability) and 10 (Administration) are deferred — deprioritized per explicit product decision, not currently planned. Do not plan or build them without an explicit scope-change conversation.

Build modules in this order — each unblocks the next. Module detail files carry the authoritative scope/business rules for that module; the breakdown doc is a cached summary and loses to the individual `Modules\NN_*.md` file on conflict.

## Agent workflow (see `Agent_Workflow_Order.md`)

Each module moves through a fixed pipeline of specialized subagents defined in `.claude/agents/`:

1. **brainstorming-agent** — clarify scope for a new/ambiguous module (skip if BRD/Modules already settle it)
2. **planning-agent** — produce `Planning\NN_*_Plan.md` (architecture, data model, APIs, UI, tasks, test strategy, acceptance criteria); never writes code
3. **worktree-agent** (isolated git worktree) or **implementation-agent** (direct in working tree) — builds the code per the approved plan
4. **verification-agent** — GATE: runs the full test suite, checks results against the plan's Acceptance Criteria/Test Strategy and the BRD
5. **gap-analysis-agent** — GATE: scores implementation vs. BRD + plan requirement-by-requirement; must reach ≥95% coverage
6. **code-review-agent** — GATE: correctness/quality/security/consistency review; no open Critical/High findings allowed
7. **finishing-agent** — presents merge/PR/cleanup options; takes no action without explicit user choice

Steps 4–6 are gates: any failure sends work back to step 3 with specifics. Nothing advances past a failing gate, and nothing is reported "done" to the user without passing all three. `Planning\` and `Verification\` (agent-behavior verification, not module verification) directories are produced/used by this pipeline — `Planning\` doesn't exist yet since no module has been planned in the current repo state.

When asked to build a module, follow this pipeline via the matching subagent rather than writing plans or code directly — each agent's `.claude/agents/*.md` file defines its exact mission and boundaries.

## Technology stack

- **Client**: Angular (standalone components, no SSR), per the prior Module 1 scaffold — session token kept in `localStorage`, HTTP interceptor for Bearer auth and global 401 handling, `auth.guard` for route protection. **Styling: Tailwind CSS** (configured in `src/client/tailwind.config.js` + `.postcssrc.json`, directives in `src/client/src/styles.scss`) — new/updated components should be styled with Tailwind utility classes directly in the template rather than hand-written component `.scss`; only reach for a component-scoped `.scss` file for something Tailwind utilities can't express (complex animations, `::ng-deep` overrides, etc.).
- **Server**: **.NET C#** (ASP.NET Core Web API). This is a change from the original scaffold, which used Node/Express + Prisma — that code was removed and should not be reused or extended. Rebuild the server on .NET conventions instead: Entity Framework Core for data access, xUnit (or NUnit/MSTest) for tests, standard ASP.NET Core middleware pipeline for concerns like HTTPS enforcement and auth. Follow `.gitignore`'s existing `.NET` section (`bin/`, `obj/`, `*.user`, `*.suo`) — it's already in place for this stack.
- Planning docs (`Planning\NN_*_Plan.md`, once produced by planning-agent) should target this Angular + .NET C# stack; if an existing plan still assumes Node/Express, flag the drift rather than silently implementing against it.

## Working conventions carried over from the prior scaffold

The removed Module 1 implementation is recoverable via `git log`/`git show` on commits prior to `90e9910` for reference on UI/UX flow and API shape only (e.g., login/forgot/reset-password screens, auth guard/interceptor pattern) — not for server-side code, since that was Node/Express and the server is now .NET C#.
