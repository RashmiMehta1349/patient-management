# Gap Analysis Agent Verification

**Document reviewed:** `.claude\agents\gap-analysis-agent.md`
**Cross-checked against:** `BRD\Doc_BRD_Final.md` (authoritative source), `.claude\agents\brainstorming-agent.md`, `.claude\agents\planning-agent.md`, `.claude\agents\implementation-agent.md`, `.claude\agents\worktree-agent.md`, `.claude\agents\verification-agent.md` (full pipeline), and the actual state of this workspace
**Reviewed by:** Verification pass (implementation-phase agent-definition review)
**Review date:** 2026-08-21
**Purpose:** Confirm the Gap Analysis Agent's definition is internally consistent, faithfully grounded in the BRD, correctly positioned as an independent gate *after* `verification-agent` without duplicating it, implements its stated 95% loop-back threshold coherently, and would be executable in this workspace as written. This document does not modify `gap-analysis-agent.md`; it is a standalone verification record.

---

## 1. What Was Reviewed

The full `gap-analysis-agent.md` definition: frontmatter (name, description, tools, model), source-of-truth list, Mission, "Non-Negotiable Rule: Score Requirements, Not Tests," Strict Boundaries, "What You Do," Working Style, and Output/Session Format.

This was checked against:
- `BRD\Doc_BRD_Final.md` — Scope, Out of Scope, Functional/Non-Functional Requirements, Success Criteria — to confirm the scored requirement set traces back to the BRD and the agent doesn't score anything outside Phase 1 boundaries.
- `.claude\agents\brainstorming-agent.md` and `.claude\agents\planning-agent.md` — to confirm the five-stage pipeline (discovery → planning → build → verify → score) remains consistently described, with no contradictory claims about role boundaries.
- `.claude\agents\implementation-agent.md` and `.claude\agents\worktree-agent.md` — to confirm the loop-back target (whichever agent built the module) is correctly identified and that this agent doesn't claim any building authority itself.
- `.claude\agents\verification-agent.md` — to confirm this agent's precondition ("a verification PASS must already exist") is stated consistently on both sides, and that scoring genuinely adds information beyond a passing test suite rather than re-deriving the same result.
- The actual current state of this workspace (presence/absence of `Planning\`, presence/absence of any implemented module code or prior verification reports) — to confirm the agent, if invoked right now, would correctly refuse to score rather than fabricate a result.

---

## 2. Checks Performed

- Frontmatter conformance to the established sibling pattern (five other agents in `.claude\agents\`).
- Tool grant appropriateness (no missing tool the agent's instructions rely on; no unnecessary/unused tool; specifically, absence of `Edit`/`EnterWorktree`/`ExitWorktree` checked as a deliberate boundary, matching `verification-agent.md`'s precedent).
- Out-of-Scope leakage scan against BRD Out-of-Scope terms, including the score-denominator-specific handling (Out-of-Scope items excluded from the score entirely, not counted as gaps).
- Role-separation check against `verification-agent.md`: does "score coverage, not tests" hold up as a genuinely independent, non-duplicative gate, and is the precondition ("verification PASS required first") stated compatibly on both sides?
- Threshold-mechanics check: is the 95% formula well-defined, auditable, and free of a rounding/edge-case ambiguity (e.g., what happens exactly at 95.0%)?
- Loop-back-target check: does the agent correctly route FAIL results to the actual building agent, and handle the case where it's ambiguous which one built the module?
- Pipeline-consistency and environment-readiness check: whether the preconditions the agent assumes (a completed module, a verification-agent report) currently hold in this workspace.
- Internal self-consistency: whether Non-Negotiable Rule, Strict Boundaries, and Output/Session Format agree with each other (no section permits something another section forbids, e.g. fixing gaps directly, or inflating the score).
- BRD Success Criteria and Non-Functional Requirements carry-forward check into the scored requirement set.

---

## 3. Findings

### 3.1 Frontmatter & Structural Conformance — Pass

`gap-analysis-agent.md` mirrors the established shape of its five siblings: YAML frontmatter (`name`, `description`, `tools`, `model: inherit`), a role-establishing opening paragraph naming its place at the end of the pipeline (explicitly listing all five preceding stages), a source-of-truth list, `## Mission`, a Non-Negotiable Rule section, Strict Boundaries, a "What You Do" numbered list, `## Working Style`, and a defined Output/Session Format. Template consistency is maintained across all six agents now present in `.claude\agents\`.

### 3.2 Tool Grant — Pass

`tools: Read, Grep, Glob, Bash, Write, AskUserQuestion` — identical to `verification-agent.md`'s tool grant. Every tool listed is used by the agent's own instructions: `Read/Grep/Glob` for reading BRD/Modules/Planning docs and inspecting the actual implementation, `Bash` for the "quick functional check" step 3 allows, `Write` for producing the coverage report, `AskUserQuestion` for pinning down ambiguous "met" bars. Deliberately and correctly **absent**: `Edit` (Strict Boundaries explicitly forbid fixing/patching gaps directly — granting `Edit` would contradict the agent's own "score, don't remediate" mandate, mirroring `verification-agent.md` §3.2's identical reasoning) and `EnterWorktree`/`ExitWorktree` (this agent has no branch/worktree lifecycle role, consistent with its upstream sibling). No orphaned tool, no missing tool.

### 3.3 Out-of-Scope Leakage Scan — Pass, with a Correctly Distinct Treatment

Strict Boundaries enumerate the identical BRD Out-of-Scope list (billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/multi-clinic, receptionist/multi-user access, follow-up alerts/reminders) already verified term-for-term in `Implementation_Verification.md` §3.3, `Worktree_Verification.md` §3.3, and `Verification_Agent_Verification.md` §3.3. What's new and correctly handled here: this agent explicitly states Out-of-Scope items must be excluded from the score's denominator entirely — "it is neither present-and-good nor a gap" — and reported as a separate flag. This is the right call mechanically: were an Out-of-Scope item folded into the denominator as a "Missing" requirement, it would improperly and permanently cap every module's achievable score below 100%; were it folded in as "Met" because it happens to exist, it would reward scope creep. The agent avoids both failure modes and keeps the Out-of-Scope check as a separate, non-score-affecting flag in its own Output/Session Format section (§ "Out-of-Scope Flags"), consistent with how `verification-agent.md` treats the same concern in its own "Out-of-Scope / BRD Cross-Check" section.

### 3.4 Role Separation from verification-agent — Pass, Genuinely Independent (Not Duplicative)

`verification-agent.md` answers "does the code do what it claims, per its own tests and Acceptance Criteria?" `gap-analysis-agent.md` answers a different question: "does the code cover everything the BRD/plan actually required, whether or not a test happens to exist for it?" The file states this distinction explicitly and correctly in its opening paragraph: "A green test suite tells you the code does what it claims to do; it does not tell you the code covers everything it was supposed to do." This is a real, non-circular difference — a module could pass every written test (verification PASS) while still omitting an entire BRD requirement nobody wrote a test for (a gap-analysis FAIL), which is exactly the scenario this agent exists to catch. No overlap-as-redundancy found: this agent's Non-Negotiable Rule explicitly forbids re-running/re-judging the test suite itself ("Do NOT re-run or re-judge the test suite itself — that's verification-agent's responsibility"), so the two agents' checks don't collide or produce a contradictory verdict on the same evidence.

**Precondition consistency, checked both directions:** `gap-analysis-agent.md` § What You Do step 1 requires "confirm a verification PASS exists... If none exists or it's a FAIL, stop." `verification-agent.md` itself makes no forward claim about gap-analysis-agent (reasonable — it was written before this agent existed, and per user instruction earlier in this session, sibling files were deliberately not updated to cross-reference this new agent). The dependency is therefore stated correctly and completely on the downstream side (this file), with no contradiction on the upstream side (silence, not a conflicting claim). This mirrors the same one-directional-reference pattern already accepted for `implementation-agent.md`/`worktree-agent.md` toward `planning-agent.md`.

### 3.5 Threshold Mechanics — Pass, One Edge Case Worth Naming (Non-Blocking)

The formula `(Met + 0.5 × Partial) / Total × 100` is well-defined and auditable: every input (Met count, Partial count, Total count) must appear in the Output/Session Format's "Score" section alongside the computed percentage, so the number is never asserted without its derivation — directly satisfying the Non-Negotiable Rule's "shown as work, not asserted" requirement.

The 95% threshold is stated as a hard boundary, and the file explicitly pre-empts the obvious failure mode of the rule going soft in practice: "94.9% loops back exactly like 60% does — state the verdict plainly, don't soften a near-miss." This directly matches the user's own framing of the request ("If the score is below 95%, the workflow loops back") and leaves no ambiguity about which side of the line 95.0% itself falls on (§ What You Do step 5: "if score ≥ 95%: PASS" — inclusive of exactly 95%, correctly matching the user's "below 95%" phrasing for the FAIL side).

**Minor observation (non-blocking):** the 0.5 weight for "Partial" is a reasonable, industry-standard convention for coverage scoring, but the file does not state *why* 0.5 specifically (as opposed to, say, 0.25 for a barely-started item vs. 0.75 for a nearly-complete one). This is not a defect — a fixed, consistently-applied weight is defensible and keeps scoring simple and auditable — but a future refinement could let "Partial" carry a stated fractional estimate per item (e.g., "80% complete") rather than a flat 0.5 for every partial item, if finer-grained scoring is ever wanted. Not required for correctness today.

### 3.6 Loop-Back Target & Repeat-Loop Handling — Pass

§ What You Do step 6 correctly routes a FAIL to "the responsible building agent (implementation-agent or worktree-agent, whichever built the module)" — this is answerable in practice because `worktree-agent.md`'s own Output/Session Format already records "Worktree: Name/path... and the branch it's on" (per `Worktree_Verification.md` §3 context) and `implementation-agent.md`'s progress tracker records which agent is active, so "whichever built the module" is a determinable fact at gap-analysis time, not a guess.

Strict Boundaries additionally require flagging a module that fails gap analysis "more than once... as a stalled loop rather than silently repeating the same cycle," and § What You Do step 7 / Output/Session Format's "Scope Scored" and "Next Step" sections both require stating the pass number (1st, 2nd, ...). This is a well-designed safeguard against exactly the infinite-loop risk implied by a hard "loop back until 95%" gate — the user's own instruction ("the workflow loops back... before continuing") could otherwise be read as unbounded, and this agent correctly adds visibility (not a hard cap, which would contradict the user's stated requirement, but a mandatory escalation-to-user signal) rather than looping silently forever.

### 3.7 Pipeline / Upstream-Dependency Consistency — Pass, Same Unmet Precondition as All Post-Planning Siblings

**Workspace check:** no `Planning\` directory, no implemented module code, and no `verification-agent` reports currently exist in this workspace (confirmed at review time). This means `gap-analysis-agent.md` currently has nothing to score — consistent with, and one step further downstream than, the identical gap already documented in `Implementation_Verification.md` §3.5, `Worktree_Verification.md` §3.5, and `Verification_Agent_Verification.md` §3.5. Unlike `verification-agent.md` (where the "nothing to verify yet" case was only implicitly covered), this agent explicitly names its precondition failure mode in § What You Do step 1 ("If none exists or it's a FAIL, stop and say gap analysis cannot proceed until verification passes") — this is the most explicit self-check of any of the three post-planning agents reviewed so far, and directly addresses the minor recommendation raised for `verification-agent.md` in `Verification_Agent_Verification.md` §3.5/§4, applied correctly to this agent's own equivalent case.

**Action implied (unchanged from prior verification passes):** run `planning-agent` per module, then `implementation-agent`/`worktree-agent`, then `verification-agent`, before `gap-analysis-agent` has anything real to score. This remains the single blocking precondition across all four post-planning agents.

### 3.8 BRD Success Criteria & Non-Functional Requirements Carry-Forward — Pass

Working Style explicitly requires the BRD's full Success Criteria list (consultation in 2–3 minutes, search/history in 2–5 seconds, page loads < 2 seconds, 80% paper reduction, smooth prescription printing, successful CSV/PDF export, high usability with minimal training) to appear as scored line items for any module they apply to, not as background context — "don't treat them as background context only verification-agent should worry about; score them explicitly where the module implements the relevant flow." This is a correct and complete restatement of every item in `BRD\Doc_BRD_Final.md § Success Criteria` (all 6 items present), and is a meaningfully stronger carry-forward than either build agent's (which only writes code "positioned to meet" the criteria) or even `verification-agent.md`'s (which checks for plausibility/risk) — this agent is the one that puts a Met/Partial/Missing status and a percentage weight on each criterion.

### 3.9 Internal Self-Consistency — Pass

- Non-Negotiable Rule ("never pad the denominator... never shrink it") is consistent with Strict Boundaries ("Do NOT silently narrow scope to inflate the score") and with Working Style ("Be exhaustive on the requirement list before being fast") — all three converge on the same anti-gaming property from different angles (denominator can't be padded, shrunk, or rushed), with no section permitting what another forbids.
- Strict Boundaries ("Do NOT fix, implement, or patch anything yourself") is consistent with § What You Do step 6 ("direct the gap list to the responsible building agent... rather than attempting the fix yourself") and with the Output/Session Format's "Gaps" section, which asks for a description plus "which agent owns closing it," not a diff or patch — remediation never leaks into this agent's own output, mirroring the identical discipline verified in `Verification_Agent_Verification.md` §3.7 for `verification-agent.md`.
- Strict Boundaries ("Do NOT let the workflow proceed... when the score is below 95%") is consistent with the Output/Session Format's "Verdict" section requiring an unambiguous top-level PASS/FAIL and with "Next Step" requiring an explicit proceed/loop-back statement — no path exists in the document where a sub-95% score could be reported without the mandatory stop instruction attached.
- The Output/Session Format's "Requirement Coverage Table" and "Score" sections correctly require the granular per-requirement breakdown before the aggregate percentage is shown, directly satisfying the Non-Negotiable Rule's "shown as work, not asserted" instruction — no section allows a bare score without its supporting table.
- No section grants an exception that another section forbids; no contradictory claims found.

---

## 4. Summary

| Check | Result |
|---|---|
| Frontmatter & structural conformance | Pass |
| Tool grant appropriateness | Pass |
| Out-of-Scope leakage scan (denominator-exclusion handling) | Pass |
| Role separation from verification-agent | Pass — independent gate, not duplicative |
| Threshold mechanics (95%, inclusive boundary, weighting) | Pass (minor non-blocking observation on Partial weighting) |
| Loop-back target & repeat-loop visibility | Pass |
| Pipeline / upstream-dependency consistency | Pass (environment precondition unmet, most explicitly self-handled of the reviewed agents) |
| BRD Success Criteria / NFR carry-forward | Pass |
| Internal self-consistency | Pass |

**Overall:** `gap-analysis-agent.md` is internally consistent, fully traceable to `BRD\Doc_BRD_Final.md`, and implements the user's requested "score against requirements, loop back below 95%" behavior coherently and safely — with an auditable scoring formula, a correctly inclusive threshold boundary, explicit anti-gaming boundaries on the denominator, a well-defined loop-back target, and a repeat-loop visibility safeguard that prevents the gate from silently cycling forever. It is the most explicit of the four post-planning agents about naming its own "nothing to score yet" precondition failure. No changes to the agent definition are required.

**Outstanding blocker (workspace-level, not agent-level, unchanged from prior verification passes):** no `Planning\` directory, no implemented module code, and no `verification-agent` reports exist yet. Run `planning-agent`, then `implementation-agent`/`worktree-agent`, then `verification-agent`, before `gap-analysis-agent` has real work to score.
