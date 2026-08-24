# Implementation Agent Verification

**Document reviewed:** `.claude\agents\implementation-agent.md`
**Cross-checked against:** `BRD\Doc_BRD_Final.md` (authoritative source), `.claude\agents\brainstorming-agent.md`, `.claude\agents\planning-agent.md`, `.claude\agents\worktree-agent.md` (sibling/upstream pipeline stages), and the actual state of this workspace
**Reviewed by:** Verification pass (implementation-phase agent-definition review)
**Review date:** 2026-08-21
**Purpose:** Confirm the Implementation Agent's definition is internally consistent, faithfully grounded in the BRD and the upstream discovery/planning pipeline, correctly differentiated from its sibling `worktree-agent`, and would be executable in this workspace as written. This document does not modify `implementation-agent.md`; it is a standalone verification record.

---

## 1. What Was Reviewed

The full `implementation-agent.md` definition: frontmatter (name, description, tools, model), source-of-truth list, Mission, "Relationship to worktree-agent," Strict Boundaries, "What You Do," Working Style, Progress Tracking Format, and Output/Session Format.

This was checked against:
- `BRD\Doc_BRD_Final.md` — Scope, Out of Scope, Success Criteria, Non-Functional Requirements — to confirm the agent won't build anything outside Phase 1 boundaries and correctly treats the BRD's Success Criteria as implementation-time constraints.
- `.claude\agents\planning-agent.md` and `.claude\agents\brainstorming-agent.md` — to confirm the three/four-agent pipeline (discovery → planning → implementation) is described consistently, with no contradictory claims about handoff artifacts or file locations.
- `.claude\agents\worktree-agent.md` — to confirm the two implementation-phase agents are cleanly differentiated (in-place vs. isolated) and don't silently overlap or contradict each other's boundaries.
- The actual current state of this workspace (presence/absence of `Planning\`, presence/absence of `.git`) — to confirm the agent, if invoked right now, could actually execute against a real plan.

---

## 2. Checks Performed

- Frontmatter conformance to the established sibling pattern (`brainstorming-agent.md`, `planning-agent.md`, `worktree-agent.md`).
- Tool grant appropriateness (no missing tool the agent's instructions rely on; no unnecessary/unused tool).
- Out-of-Scope leakage scan against BRD Out-of-Scope terms.
- Differentiation check against `worktree-agent.md`: do the two agents' boundaries partition cleanly, or is there a gap/overlap in when each should be used?
- Pipeline-consistency check: whether `implementation-agent.md`'s description of its upstream dependency (`Planning\NN_*_Plan.md`) matches what `planning-agent.md` actually promises to produce, and whether that artifact currently exists.
- Environment-readiness check: whether the preconditions the agent assumes (an approved plan, optionally a git repo) currently hold in this workspace.
- Internal self-consistency: whether Strict Boundaries, the Progress Tracking Format, and the Output/Session Format agree with each other (no section permits something another section forbids).
- BRD Success Criteria and Non-Functional Requirements carry-forward check (performance, security, reliability) into implementation-time obligations.

---

## 3. Findings

### 3.1 Frontmatter & Structural Conformance — Pass

`implementation-agent.md` mirrors the established shape of its three siblings: YAML frontmatter (`name`, `description`, `tools`, `model: inherit`), a role-establishing opening paragraph naming its place in the pipeline, a source-of-truth list, `## Mission`, boundary sections, a "What You Do" numbered list, `## Working Style`, and a defined output format. It additionally introduces a "Relationship to worktree-agent" section and a distinct "Progress Tracking Format" — both are reasonable, purpose-specific additions rather than template drift.

### 3.2 Tool Grant — Pass

`tools: Read, Grep, Glob, Bash, Write, Edit, AskUserQuestion`. Every tool listed is used by the agent's own instructions: `Read/Grep/Glob` for reading BRD/Modules/Planning docs, `Bash` for running tests and (optionally) git commits, `Write/Edit` for implementation, `AskUserQuestion` for plan-gap decisions. This matches `worktree-agent.md`'s tool set exactly, minus `EnterWorktree`/`ExitWorktree` — which is correct, since this agent deliberately does not isolate work in a worktree. No unused tool, no missing tool.

### 3.3 Out-of-Scope Leakage Scan — Pass

Strict Boundaries explicitly enumerate the BRD's Out-of-Scope list (billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/multi-clinic, receptionist/multi-user access, follow-up alerts/reminders) as things to name and stop on. Cross-checked term-by-term against `BRD\Doc_BRD_Final.md § Out of Scope` — the list is complete and accurate (all 10 items present, verbatim in substance), and no Out-of-Scope term appears anywhere else in the file outside that refusal context.

### 3.4 Differentiation from worktree-agent — Pass

The "Relationship to worktree-agent" section states this agent works "directly in the current working tree — no `EnterWorktree` isolation," and explicitly defers to `worktree-agent` if the user says "worktree" or asks for isolation. `worktree-agent.md`'s own description states it should be used when the user is "ready for actual code to be written" and explicitly names its Non-Negotiable Rule of always isolating via `EnterWorktree`/`ExitWorktree`. The two agents' descriptions cross-reference each other correctly and by name in both directions (`implementation-agent.md` → `worktree-agent.md` and vice versa, per the `worktree-agent.md` frontmatter description reviewed in `Worktree_Verification.md` §3.1). No gap: any "build the plan into code" request is routed to exactly one of the two agents depending on the isolation preference, and neither claims the other's territory.

One asymmetry worth noting (not a defect): `implementation-agent.md` handles the "not a git repository" case explicitly (§ Relationship to worktree-agent — "a git repo is not a hard requirement for this agent... proceed"), while `worktree-agent.md` hard-requires a git repo (per `EnterWorktree`'s own precondition). This is correctly reasoned given the tool contract, not an inconsistency.

### 3.5 Pipeline / Upstream-Dependency Consistency — Pass, with a Currently-Unmet Precondition (Environment, not Definition)

`implementation-agent.md` states its primary build spec is `Planning\NN_*_Plan.md`, "produced by planning-agent," and instructs: "If no `Planning\NN_*_Plan.md` exists yet for the requested module, say so and suggest planning-agent first rather than improvising an implementation without an approved plan." This matches `planning-agent.md`'s own stated output location and the pipeline order already validated in `Verification\Planning_Verification.md` and `Verification\Worktree_Verification.md`.

**Workspace check:** no `Planning\` directory currently exists in this workspace (confirmed at review time — only `BRD\`, `Modules\`, `Verification\`, and `.claude\agents\` are present). `Verification\Planning_Verification.md` §0 previously documented the same gap for `worktree-agent.md`. This means `implementation-agent.md`'s explicit self-check ("if no plan exists, say so and suggest planning-agent") is not just correct in principle — it is the exact behavior that would currently fire if this agent were invoked today. This is a strength of the definition (it fails safe and legibly rather than improvising), not a defect in the agent file itself.

**Action implied:** run `planning-agent` per module to produce `Planning\NN_*_Plan.md` files before `implementation-agent` (or `worktree-agent`) can do real build work. This is unchanged from the prior two verification passes and remains the single blocking precondition across both implementation-phase agents.

### 3.6 BRD Success Criteria & Non-Functional Requirements Carry-Forward — Pass

Working Style correctly restates the BRD's Success Criteria (consultation in 2–3 minutes, search/history in 2–5 seconds, page loads < 2 seconds) as implementation-time functional requirements on the code produced, not planning-time targets — consistent with the same carry-forward already verified for `worktree-agent.md` in `Worktree_Verification.md` §3.4. Security Considerations are folded into the per-step execution loop (§ What You Do, step 4e: "parameterized queries, input validation, auth guards... as you write it, not retroactively"), which aligns with BRD § Non-Functional Requirements → Security (secure login, pre-provisioned account, encryption at rest/in transit). No BRD Success Criterion or Security/Reliability NFR is omitted from the agent's stated obligations.

### 3.7 Internal Self-Consistency — Pass

- Strict Boundaries ("Do NOT skip writing tests for a step... if deferred, say so explicitly and track it as an open item") is consistent with the Progress Tracking Format's `[!]` blocked/deferred-with-reason state and with the Output/Session Format's "Tests" and "Deviations From Plan" sections — a deferred test cannot silently vanish; it must surface in at least two independent places (checklist status and session report).
- Strict Boundaries ("Do NOT merge branches, force-push, amend published commits, or skip commit hooks without explicit user instruction") is consistent with the general git-safety posture established across all sibling agents and does not conflict with § What You Do step 6 ("commit at natural checkpoints... with descriptive messages"), which only describes ordinary incremental commits, not any of the forbidden destructive operations.
- The Progress Tracking Format's four states (`[x]`/`[~]`/`[ ]`/`[!]`) are used consistently in both the format definition and referenced correctly in § What You Do step 4f and step 7 ("final state of the progress checklist").
- No section grants an exception that another section forbids; no contradictory claims found.

### 3.8 Minor Observations (Not Defects)

- Unlike `worktree-agent.md`, this agent does not need an `EnterWorktree`/`ExitWorktree` compliance table (§3.5 of `Worktree_Verification.md`) since it uses neither tool — correctly reflected by their absence from its tool grant.
- The agent's `git status`-before-starting instruction ("flag any pre-existing uncommitted changes to the user before adding your own — don't mix your work into an unrelated dirty working tree without asking") is a good practical safeguard given this agent, unlike `worktree-agent`, has no isolation mechanism to fall back on if the working tree is already dirty.

---

## 4. Summary

| Check | Result |
|---|---|
| Frontmatter & structural conformance | Pass |
| Tool grant appropriateness | Pass |
| Out-of-Scope leakage scan | Pass |
| Differentiation from worktree-agent | Pass |
| Pipeline / upstream-dependency consistency | Pass (environment precondition unmet, correctly self-handled) |
| BRD Success Criteria / NFR carry-forward | Pass |
| Internal self-consistency | Pass |

**Overall:** `implementation-agent.md` is internally consistent, fully traceable to `BRD\Doc_BRD_Final.md`, cleanly differentiated from `worktree-agent.md`, and correctly designed to fail safe when its required upstream artifact (`Planning\NN_*_Plan.md`) does not yet exist — which is the current state of this workspace. No changes to the agent definition are recommended.

**Outstanding blocker (workspace-level, not agent-level, unchanged from prior verification passes):** no `Planning\` directory exists yet. Run `planning-agent` per module before either implementation-phase agent can execute real build work.
