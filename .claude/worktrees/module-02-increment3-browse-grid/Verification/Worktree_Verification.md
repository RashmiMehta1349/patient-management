# Worktree Agent Verification

**Document reviewed:** `.claude\agents\worktree-agent.md`
**Cross-checked against:** `BRD\Doc_BRD_Final.md` (authoritative source), `.claude\agents\planning-agent.md` and `.claude\agents\brainstorming-agent.md` (upstream pipeline stages), `EnterWorktree`/`ExitWorktree` tool contracts, and the actual state of this workspace
**Reviewed by:** Verification pass (implementation-phase agent-definition review)
**Review date:** 2026-08-21
**Purpose:** Confirm the Worktree Agent's definition is internally consistent, faithfully grounded in the BRD and the upstream planning pipeline, correctly uses the `EnterWorktree`/`ExitWorktree` tool contracts, and is actually executable in this workspace as written. This document does not modify `worktree-agent.md`; it is a standalone verification record.

---

## 1. What Was Reviewed

The full `worktree-agent.md` definition: frontmatter (name, description, tools, model), source-of-truth list, Mission, "Non-Negotiable Rule: Always Work in a Worktree," Strict Boundaries, "What You Do," Working Style, and Output/Session Format.

This was checked against:
- `BRD\Doc_BRD_Final.md` — Scope, Out of Scope, Success Criteria — to confirm the agent won't build anything outside Phase 1 boundaries and correctly treats the BRD's Success Criteria as implementation-time constraints, not just planning artifacts.
- `.claude\agents\planning-agent.md` and `.claude\agents\brainstorming-agent.md` — to confirm the three-agent pipeline (discovery → planning → implementation) is described consistently across all three files, with no contradictory claims about handoff points or file locations.
- The `EnterWorktree` / `ExitWorktree` tool definitions — to confirm the agent's instructions match what these tools actually require and permit (git-repository precondition, `remove` semantics, session-scoping).
- The actual current state of this workspace (`git status`, presence/absence of `.git`, presence/absence of `.claude/settings.json` hooks, presence/absence of `Planning\`) — to confirm the agent, if invoked right now, could actually execute its own Non-Negotiable Rule.

---

## 2. Checks Performed

- Frontmatter conformance to the established two-sibling pattern (`brainstorming-agent.md`, `planning-agent.md`).
- Tool grant appropriateness (no missing tool the agent's instructions rely on; no unnecessary/unused tool).
- Out-of-Scope leakage scan against BRD Out-of-Scope terms.
- Tool-contract compliance: every claim `worktree-agent.md` makes about `EnterWorktree`/`ExitWorktree` behavior checked against those tools' actual descriptions.
- Environment-readiness check: whether the preconditions `EnterWorktree` requires (git repo, or configured hooks) currently hold in this workspace.
- Pipeline-consistency check: whether `worktree-agent.md`'s description of its upstream dependency (`Planning\NN_*_Plan.md`) matches what `planning-agent.md` actually promises to produce, and whether that artifact currently exists.
- Internal self-consistency: whether the Strict Boundaries, Non-Negotiable Rule, and Output Format sections agree with each other (no section permits something another section forbids).

---

## 3. Findings

### 3.1 Frontmatter & Structural Conformance — Pass

`worktree-agent.md` mirrors the established shape of `brainstorming-agent.md` and `planning-agent.md`: YAML frontmatter (`name`, `description`, `tools`, `model: inherit`), a role-establishing opening paragraph naming its place in the pipeline ("operating strictly *after* discovery... and planning..."), a source-of-truth list, `## Mission`, boundary sections, a "What You Do" numbered list, `## Working Style`, and a defined output format. This is the third agent built to the same template, and the template has been applied consistently.

### 3.2 Tool Grant — Pass

`tools: Read, Grep, Glob, Bash, Write, Edit, AskUserQuestion, EnterWorktree, ExitWorktree`. Every tool listed is used by the agent's own instructions: `Read/Grep/Glob` for reading BRD/Modules/Planning docs, `Bash` for running tests/commits inside the worktree, `Write/Edit` for implementation, `AskUserQuestion` for plan-gap decisions, `EnterWorktree/ExitWorktree` for the core isolation behavior. Unlike its two siblings, this agent correctly adds `Bash` (needed to actually run builds/tests/git commands) — an appropriate, deliberate widening of tool scope consistent with it being the only agent of the three that writes and executes code.

### 3.3 Out-of-Scope Leakage Scan — Pass

The agent's Strict Boundaries explicitly enumerate the BRD's Out-of-Scope list (billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/multi-clinic, receptionist/multi-user access, follow-up alerts/reminders) as things to refuse and flag. Cross-checked term-by-term against `BRD\Doc_BRD_Final.md § Out of Scope` — the list is complete and accurate; no Out-of-Scope item is missing from the agent's refusal list, and no Out-of-Scope term appears anywhere else in the file outside that refusal context.

### 3.4 BRD Success Criteria Handling — Pass

Working Style correctly elevates the BRD's Success Criteria (consultation in 2–3 minutes, search/history in 2–5 seconds, page load < 2 seconds) from planning-time targets to implementation-time constraints ("write code that's positioned to meet them, and call out during implementation if something threatens to violate them"). This is a meaningful and accurate carry-forward — these criteria are otherwise easy to lose between planning and code, and `Verification\Planning_Verification.md` § 1.3–1.4 already identified that page-load performance in particular has no explicit module owner in `Modules\`, so this agent-level reinforcement is a useful backstop, not redundant.

### 3.5 Tool-Contract Compliance (`EnterWorktree` / `ExitWorktree`) — Pass, One Minor Gap

Checked claim-by-claim against the actual tool definitions:

| Agent's claim | Tool contract | Match? |
|---|---|---|
| "Before writing or editing a single file, call `EnterWorktree`" | Tool creates an isolated worktree and switches the session's working directory into it | ✅ Correct sequencing |
| "Name the worktree meaningfully" | `EnterWorktree` accepts an optional `name` param; each path segment restricted to letters/digits/dots/underscores/dashes, max 64 chars | ✅ Correct; agent's example name (`module-02-patient-management`) is valid under that constraint |
| "Never call `ExitWorktree` with `remove` unless the user confirmed it" | `ExitWorktree` with `action: "remove"` **refuses** to run if there are uncommitted files or unmerged commits, unless `discard_changes: true` is also passed | ✅ Consistent — the agent's instruction is actually stricter than the tool requires (tool would block silently; agent commits to asking first regardless) |
| Implicit: agent will call `EnterWorktree` fresh each new implementation task | Tool: "Must not already be in a worktree session when creating a new worktree (`name`)" | ⚠️ **Gap** — not addressed. If a prior `EnterWorktree` call in the same session is still active when the agent is asked to start a *second* module, a second `name`-based `EnterWorktree` call would be rejected by the tool. The agent has no instruction for this case (exit-then-re-enter vs. continue in the same worktree). |
| Not mentioned | `EnterWorktree` also accepts `path` to re-enter an *existing* worktree from a prior session | ⚠️ **Gap** — the agent never mentions this option, so a returning session resuming unfinished work on a module would default to creating a brand-new worktree rather than re-entering the one already in progress, unless the user happens to specify it. |

Neither gap is a correctness defect in what the agent currently claims (nothing it says is false), but both are missing operational guidance for realistic multi-module or resumed-session workflows.

### 3.6 Environment Readiness — **Blocking Finding**

`EnterWorktree`'s own contract states: *"Must be in a git repository, OR have WorktreeCreate/WorktreeRemove hooks configured in settings.json."*

Checked against this workspace's actual state:
- `git rev-parse --is-inside-work-tree` → **fails**: `fatal: not a git repository (or any of the parent directories): .git`
- `.claude/settings.json` → **does not exist** (only `.claude/agents/*.md` are present; no hooks configured)

**Neither precondition is currently met.** As written, if `worktree-agent` is invoked in this workspace right now, its very first mandatory action (`EnterWorktree`, per its own Non-Negotiable Rule) would fail immediately, before any implementation work could begin. This is not a flaw in the agent's *design* — the design correctly requires isolation — but it is a real, currently-blocking gap between the agent's assumptions and the workspace it's meant to operate in.

### 3.7 Pipeline Consistency — Pass, With a Known Carried-Forward Gap

`worktree-agent.md` correctly describes itself as downstream of `planning-agent.md`'s output (`Planning\NN_*_Plan.md`) and instructs itself to refuse to improvise if no plan exists for the requested module ("say so and suggest planning-agent first"). This is accurate and matches `planning-agent.md`'s own stated output format (`Planning\NN_ModuleName_Plan.md`).

However, per `Verification\Planning_Verification.md` § 0 and § 3 (Gap G6), **no `Planning\` folder currently exists in this workspace** — consistent with what this review independently reconfirmed via `ls`. This means `worktree-agent`'s self-check ("If no `Planning\NN_*_Plan.md` exists yet for the requested module, say so...") is not just a theoretical safeguard — it would trigger immediately for *every* module if the agent were invoked today. This is expected and correctly handled by the agent's own design (it degrades safely rather than improvising), so it is **not** scored as a defect, but it is worth stating plainly alongside § 3.6: today, this agent would correctly refuse to proceed twice over — no git repo for `EnterWorktree`, and no plan for it to build against.

### 3.8 Internal Self-Consistency — Pass

No contradiction found between the Non-Negotiable Rule, Strict Boundaries, "What You Do," and Output Format sections. In particular:
- The instruction to never merge/force-push/delete branches without explicit instruction (Strict Boundaries) is consistent with the Output Format's "Next Step" section, which correctly frames merge/review as "a manual, user-driven step outside this agent's scope" rather than something the agent offers to do itself.
- The instruction to commit in small, meaningful units (`What You Do` step 8) is consistent with never force-pushing or amending (same step), and matches the general git-safety posture established elsewhere in this project's agent set.
- The instruction to ask before discarding uncommitted work is stated twice (Non-Negotiable Rule and implicitly in Strict Boundaries' "half-finished changes" clause) without contradiction — both point the same direction.

---

## 4. Open Questions Raised By This Review

1. Should the agent gain explicit instructions for the "already in a worktree session" case (§ 3.5) — e.g., exit-and-re-enter for a new module vs. reuse the current worktree for a related change?
2. Should the agent mention `EnterWorktree`'s `path` parameter for resuming a previously-created worktree across sessions, so returning to unfinished module work doesn't default to spinning up a duplicate worktree?
3. Given this workspace is not currently a git repository, should the project be initialized with `git init` before `worktree-agent` is ever invoked for real, or should the agent's Mission note this precondition explicitly (e.g., "if not in a git repository, offer to `git init` first," mirroring the general project git-safety guidance already established for this session)?

---

## 5. Recommendations

- **Do not fix silently.** As with the prior planning-agent review, these are documentation/robustness improvements, not urgent defects — the agent fails safely (refuses rather than misbehaves) in both blocking scenarios found (§ 3.6, § 3.7).
- Consider adding one line to `worktree-agent.md`'s Non-Negotiable Rule section addressing the "already in a worktree" case from § 3.5, so multi-module sessions have defined behavior.
- Consider adding a one-line mention of `EnterWorktree`'s `path` option for resuming prior work, to avoid duplicate worktrees across sessions.
- Before this agent is actually invoked to build anything, this workspace needs `git init` (or equivalent hook configuration) — this is an environment-setup action for the user to decide on, not something to do automatically.
- No changes are recommended to the agent's scope, boundaries, or BRD alignment — those are sound as written.

---

## 6. Scope Impact

None. All findings are environment-readiness and operational-robustness items; no Out-of-Scope BRD item is implicated, and no change to the BRD, `Modules\`, or `planning-agent.md` is required. The one action with real weight — initializing git in this workspace — is an infrastructure precondition for using `worktree-agent` at all, not a scope change to the product being planned.

---

## 7. Sign-Off Checklist

| Item | Status |
|---|---|
| Frontmatter/tooling conforms to established agent pattern | ✅ Pass |
| No Out-of-Scope BRD functionality permitted by the agent | ✅ Pass |
| BRD Success Criteria carried forward as implementation constraints | ✅ Pass |
| Claims about `EnterWorktree`/`ExitWorktree` behavior match tool contracts | ✅ Pass (2 minor operational gaps noted, not incorrect) |
| Agent is executable in this workspace as of today | ❌ **Blocked** — no git repository, no `Planning\` documents yet |
| Internal consistency across all sections | ✅ Pass |

**Overall status: Conditional Pass.** The agent definition itself is sound, well-bounded, and consistent with the BRD and the upstream planning pipeline. It cannot actually be used in this workspace yet, for two independent and already-known reasons: this is not a git repository, and no per-module `Planning\` documents have been generated. Both are pre-existing, expected states (not new defects introduced by this agent), and the agent is designed to fail safely rather than paper over either gap.
