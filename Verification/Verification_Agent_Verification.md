# Verification Agent Verification

**Document reviewed:** `.claude\agents\verification-agent.md`
**Cross-checked against:** `BRD\Doc_BRD_Final.md` (authoritative source), `.claude\agents\brainstorming-agent.md`, `.claude\agents\planning-agent.md`, `.claude\agents\implementation-agent.md`, `.claude\agents\worktree-agent.md` (full sibling/upstream pipeline), and the actual state of this workspace
**Reviewed by:** Verification pass (implementation-phase agent-definition review)
**Review date:** 2026-08-21
**Purpose:** Confirm the Verification Agent's definition is internally consistent, faithfully grounded in the BRD, correctly gates both build-phase agents (implementation-agent and worktree-agent) without overlapping or contradicting either, and would be executable in this workspace as written. This document does not modify `verification-agent.md`; it is a standalone verification record.

---

## 1. What Was Reviewed

The full `verification-agent.md` definition: frontmatter (name, description, tools, model), source-of-truth list, Mission, "Non-Negotiable Rule: Verify, Don't Assume," Strict Boundaries, "What You Do," Working Style, and Output/Session Format.

This was checked against:
- `BRD\Doc_BRD_Final.md` — Scope, Out of Scope, Success Criteria, Non-Functional Requirements — to confirm the agent's gate criteria trace back to the BRD and it doesn't validate anything outside Phase 1 boundaries.
- `.claude\agents\brainstorming-agent.md` and `.claude\agents\planning-agent.md` — to confirm the four-stage pipeline (discovery → planning → build → verify) is described consistently, with no contradictory claims about role boundaries.
- `.claude\agents\implementation-agent.md` and `.claude\agents\worktree-agent.md` — to confirm this agent gates *both* build-phase agents evenhandedly, references the correct upstream artifacts, and doesn't duplicate or contradict either agent's own testing/acceptance-criteria responsibilities.
- The actual current state of this workspace (presence/absence of `Planning\`, presence/absence of `.git`, presence/absence of any implemented module code) — to confirm the agent, if invoked right now, would behave correctly against real conditions rather than only in the abstract.

---

## 2. Checks Performed

- Frontmatter conformance to the established sibling pattern (four other agents in `.claude\agents\`).
- Tool grant appropriateness (no missing tool the agent's instructions rely on; no unnecessary/unused tool; specifically, absence of `Edit`/`EnterWorktree`/`ExitWorktree` checked as a deliberate boundary, not an oversight).
- Out-of-Scope leakage scan against BRD Out-of-Scope terms.
- Role-separation check against `implementation-agent.md` and `worktree-agent.md`: does "verify, don't fix" hold up without gap or overlap against what those two agents already claim to do (they both self-test during the build loop; does verification-agent duplicate or genuinely add an independent gate)?
- Pipeline-consistency check: whether `verification-agent.md`'s description of its upstream dependency (`Planning\NN_*_Plan.md` Test Strategy/Acceptance Criteria) matches what `planning-agent.md` actually promises to produce, and whether that artifact currently exists.
- Environment-readiness check: whether the preconditions the agent assumes (an implemented module, a runnable test suite) currently hold in this workspace.
- Internal self-consistency: whether Non-Negotiable Rule, Strict Boundaries, and Output/Session Format agree with each other (no section permits something another section forbids, e.g. fixing code).
- BRD Success Criteria and Non-Functional Requirements carry-forward check (performance, security, reliability) into verification-time obligations.

---

## 3. Findings

### 3.1 Frontmatter & Structural Conformance — Pass

`verification-agent.md` mirrors the established shape of its four siblings: YAML frontmatter (`name`, `description`, `tools`, `model: inherit`), a role-establishing opening paragraph naming its place at the end of the pipeline, a source-of-truth list, `## Mission`, a Non-Negotiable Rule section (matching the pattern established by `worktree-agent.md`'s "Non-Negotiable Rule: Always Work in a Worktree"), Strict Boundaries, a "What You Do" numbered list, `## Working Style`, and a defined Output/Session Format. Template consistency is maintained across all five agents now present.

### 3.2 Tool Grant — Pass

`tools: Read, Grep, Glob, Bash, Write, AskUserQuestion`. Every tool listed is used by the agent's own instructions: `Read/Grep/Glob` for reading BRD/Modules/Planning docs and locating test files, `Bash` for actually running the test suite (the core of this agent's job), `Write` for producing verification reports, `AskUserQuestion` for genuinely ambiguous/subjective acceptance criteria. Deliberately and correctly **absent**: `Edit` (this agent's Strict Boundaries explicitly forbid fixing code — granting `Edit` would contradict its own "verify, don't remediate" mandate) and `EnterWorktree`/`ExitWorktree` (this agent inspects whatever working tree or worktree it's pointed at; it doesn't create or tear down isolation, which remains worktree-agent's exclusive responsibility per `Worktree_Verification.md` §3.5). This is a meaningfully tighter tool grant than any sibling agent, and the tightening is directly traceable to boundaries stated in the same file — no orphaned tool, no missing tool.

### 3.3 Out-of-Scope Leakage Scan — Pass

Strict Boundaries explicitly enumerate the BRD's Out-of-Scope list (billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/multi-clinic, receptionist/multi-user access, follow-up alerts/reminders) and instruct the agent to flag any such functionality found implemented as a violation, rather than testing it as if it were in scope. Cross-checked term-by-term against `BRD\Doc_BRD_Final.md § Out of Scope` — all 10 items present, verbatim in substance, matching the identical list already verified in `Implementation_Verification.md` §3.3 and `Worktree_Verification.md` §3.3. This is a novel and correct addition relative to the two build-phase agents: those agents refuse to *build* Out-of-Scope items; this agent additionally checks that nothing Out-of-Scope slipped through *despite* that refusal — a genuine independent check, not a redundant restatement.

### 3.4 Role Separation from implementation-agent / worktree-agent — Pass, Correctly Independent (Not Redundant)

Both `implementation-agent.md` (§ What You Do, step 4d: "Run the relevant tests; fix failures before moving on") and `worktree-agent.md` (§ What You Do, step 6: "Test as you go... Run the test suite before considering a task complete") already run tests during the build loop. This raises a fair question: does `verification-agent.md` merely duplicate that step?

Checked against the actual text — it does not:
- Both build agents run tests **during** the build, as a self-check by the same party doing the building — a standard but inherently non-independent check (the builder decides for itself when it's satisfied).
- `verification-agent.md`'s Non-Negotiable Rule explicitly targets exactly this gap: "Never accept 'tests pass' or 'done' from an implementation agent's report at face value. Run the tests yourself, in this session, and read the actual output." This reframes testing from a self-report into an independent, adversarial re-check by a party with no stake in the outcome looking good — a materially different guarantee than "the builder said it tested fine."
- The gate semantics are new and not claimed by either build agent: neither `implementation-agent.md` nor `worktree-agent.md` states that its own self-testing blocks the *next* module or a merge — `worktree-agent.md` §"Next Step" only prompts keep/remove/proceed-to-review as a question to the user, without an enforced gate. `verification-agent.md` is the first (and only) agent in the pipeline that renders an explicit PASS/FAIL verdict and states plainly that nothing may proceed on FAIL.

No contradiction found: `verification-agent.md` § Mission ("You do not... write feature code") and Strict Boundaries ("Do NOT fix failing code, tests, or configuration yourself... hand it back") are consistent with, and correctly complementary to, both build agents' own step 4d/step 6 self-testing — one is build-time self-check, the other is an independent gate. Neither build-agent file claims exclusive ownership of "testing" in a way that would conflict with this agent existing.

### 3.5 Pipeline / Upstream-Dependency Consistency — Pass, with the Same Currently-Unmet Precondition as Its Siblings

`verification-agent.md` states its primary checklist is `Planning\NN_*_Plan.md`'s Test Strategy and Acceptance Criteria sections, matching `planning-agent.md`'s stated output and the same dependency already validated for `implementation-agent.md` and `worktree-agent.md` in `Implementation_Verification.md` §3.5 and `Worktree_Verification.md` §3.5.

**Workspace check:** no `Planning\` directory and no implemented module code currently exist in this workspace (confirmed at review time — only `BRD\`, `Modules\`, `Verification\`, and `.claude\agents\` are present). This means `verification-agent.md` currently has nothing to verify: no plan to check Acceptance Criteria against, no built code, no test suite to run. The agent's own Non-Negotiable Rule ("If tests cannot be run at all... that is itself a verification failure — report it as blocking, don't skip verification and let the module through by default") correctly anticipates a version of this situation (a broken/missing test runner for an otherwise-implemented module) but does not explicitly address the more basic upstream case of "no module has been implemented yet to verify." This is a minor definitional gap, not a defect that would cause incorrect behavior — invoking this agent with nothing built would still legitimately resolve to a stated FAIL/blocked-can't-verify outcome under the existing rule's spirit, it's just not spelled out as its own named case the way `implementation-agent.md` explicitly spells out "if no plan exists, say so and suggest planning-agent first."

**Action implied (unchanged from prior verification passes):** run `planning-agent` per module, then `implementation-agent` or `worktree-agent`, before `verification-agent` has anything real to gate. This remains the single blocking precondition across all three post-planning agents.

**Minor recommendation (non-blocking):** consider adding an explicit instruction for the "nothing implemented yet" case — e.g., "If no code exists yet for the claimed module/step, say so and stop rather than reporting a vacuous PASS or an uninformative generic FAIL" — mirroring the explicit self-check already present in `implementation-agent.md` for its own missing-plan case. Not required for correctness today (the existing rule already implies a FAIL/blocked outcome), but would make the failure mode as legible as its sibling agents'.

### 3.6 BRD Success Criteria & Non-Functional Requirements Carry-Forward — Pass

Working Style correctly restates the BRD's Success Criteria (consultation in 2–3 minutes, search/history in 2–5 seconds, page loads < 2 seconds) as things to actively check for plausibility even absent an automated performance test ("no obviously unindexed full-table scan on the search path... call out risk rather than staying silent because 'no test covers it'"). This is a stronger and more independent carry-forward than either build agent's — those agents write code "positioned to meet" the criteria; this agent is the one instructed to actively look for evidence the criteria are actually being violated. § What You Do step 6 additionally requires cross-checking Security and Reliability NFRs (BRD § Non-Functional Requirements) at verification time, not just Performance. No BRD Success Criterion or Security/Reliability NFR is omitted from the agent's stated verification obligations.

### 3.7 Internal Self-Consistency — Pass

- Non-Negotiable Rule ("Never let partial or flaky results round up to a pass") is consistent with Strict Boundaries ("Do NOT skip straight to 'looks fine' without running something") and with the Output/Session Format's "Verdict" section requiring an unambiguous top-level PASS/FAIL — a flaky or partial result cannot be laundered into a silent pass anywhere in the document.
- Strict Boundaries ("Do NOT fix failing code... hand it back") is consistent with § What You Do step 9 ("on FAIL, direct the failure report to the agent/user responsible for the fix... rather than attempting the fix yourself") and with the Output/Session Format's "Blocking Issues" section, which asks for a reproducible report rather than a diff or patch — remediation never leaks into this agent's own output format.
- Strict Boundaries ("Do NOT merge branches, force-push, amend commits, delete worktrees, or skip commit hooks") is consistent with the general git-safety posture already established across all four sibling agents, and correctly scoped down further than `worktree-agent.md`'s equivalent boundary (this agent has no branch-management role at all, not even the "ask before removing" version worktree-agent has).
- The Output/Session Format's "Next Step" section correctly mirrors the "Gate explicitly" instruction in § What You Do step 8 — both require an unambiguous proceed/do-not-proceed statement, not a hedge.
- No section grants an exception that another section forbids; no contradictory claims found.

### 3.8 Minor Observations (Not Defects)

- This agent's frontmatter description already states "PROACTIVELY... before any further module is started, any worktree is merged, or any work is reported to the user as complete," which correctly positions it as mandatory, not optional, immediately following either build agent — consistent with the user's own stated intent for this agent ("Nothing proceeds until verification passes").
- Unlike `worktree-agent.md`, this agent has no branch/worktree lifecycle responsibility at all (§3.2 above) — correctly reflected by the complete absence of `EnterWorktree`/`ExitWorktree` from its tool grant, and by Strict Boundaries explicitly disclaiming any worktree-deletion authority.
- The agent's insistence on running tests "yourself, in this session" (rather than trusting a prior session's report) is a good practical safeguard consistent with its adversarial-gate purpose, and correctly closes the one gap identified in §3.4 that would otherwise make this agent redundant with its two siblings' self-testing.

---

## 4. Summary

| Check | Result |
|---|---|
| Frontmatter & structural conformance | Pass |
| Tool grant appropriateness | Pass |
| Out-of-Scope leakage scan | Pass |
| Role separation from implementation-agent / worktree-agent | Pass — independent gate, not redundant |
| Pipeline / upstream-dependency consistency | Pass (environment precondition unmet, mostly self-handled; minor recommendation below) |
| BRD Success Criteria / NFR carry-forward | Pass |
| Internal self-consistency | Pass |

**Overall:** `verification-agent.md` is internally consistent, fully traceable to `BRD\Doc_BRD_Final.md`, and provides a genuinely independent gate against both build-phase agents rather than duplicating their self-testing. Its tool grant is correctly and deliberately tighter than its siblings' (no `Edit`, no worktree tools), directly enforcing its own "verify, don't fix" mandate. No changes to the agent definition are required.

**Non-blocking recommendation:** add an explicit "nothing implemented yet to verify" case to § Non-Negotiable Rule or § What You Do, mirroring `implementation-agent.md`'s explicit missing-plan self-check, so this failure mode is named rather than only implied.

**Outstanding blocker (workspace-level, not agent-level, unchanged from prior verification passes):** no `Planning\` directory and no implemented module code exist yet. Run `planning-agent`, then `implementation-agent`/`worktree-agent`, before `verification-agent` has real work to gate.
