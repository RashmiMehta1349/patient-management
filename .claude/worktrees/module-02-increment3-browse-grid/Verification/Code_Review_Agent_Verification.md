# Code Review Agent Verification

**Document reviewed:** `.claude\agents\code-review-agent.md`
**Cross-checked against:** `BRD\Doc_BRD_Final.md` (authoritative source), `.claude\agents\brainstorming-agent.md`, `.claude\agents\planning-agent.md`, `.claude\agents\implementation-agent.md`, `.claude\agents\worktree-agent.md`, `.claude\agents\verification-agent.md`, `.claude\agents\gap-analysis-agent.md` (full six-agent pipeline), and the actual state of this workspace
**Reviewed by:** Verification pass (implementation-phase agent-definition review)
**Review date:** 2026-08-21
**Purpose:** Confirm the Code Review Agent's definition is internally consistent, faithfully grounded in the BRD, correctly positioned as the final gate *after* `gap-analysis-agent` without duplicating either upstream gate, implements a coherent fix-and-re-review loop, and would be executable in this workspace as written. This document does not modify `code-review-agent.md`; it is a standalone verification record.

---

## 1. What Was Reviewed

The full `code-review-agent.md` definition: frontmatter (name, description, tools, model), source-of-truth list, Mission, "Non-Negotiable Rule: Feedback Gets Addressed, Not Just Filed," Strict Boundaries, "What You Do," Working Style, and Output/Session Format.

This was checked against:
- `BRD\Doc_BRD_Final.md` — Scope, Out of Scope, Non-Functional Requirements, Success Criteria — to confirm the review criteria trace back to the BRD, and the agent doesn't hold the code to a bar the BRD doesn't ask for.
- `.claude\agents\brainstorming-agent.md` and `.claude\agents\planning-agent.md` — to confirm the six-stage pipeline (discovery → planning → build → verify → score → review) remains consistently described, with no contradictory claims about role boundaries.
- `.claude\agents\implementation-agent.md` and `.claude\agents\worktree-agent.md` — to confirm the loop-back target for findings is correctly identified, and that this agent claims no building authority itself.
- `.claude\agents\verification-agent.md` and `.claude\agents\gap-analysis-agent.md` — to confirm this agent's precondition ("both upstream PASSes must already exist") is stated consistently, and that code review genuinely adds information beyond "tests pass" and "requirements covered" rather than re-deriving either.
- The actual current state of this workspace (presence/absence of `Planning\`, presence/absence of any implemented module code or prior verification/gap-analysis reports) — to confirm the agent, if invoked right now, would correctly refuse to review rather than fabricate a result.

---

## 2. Checks Performed

- Frontmatter conformance to the established sibling pattern (six other agents in `.claude\agents\`).
- Tool grant appropriateness (no missing tool the agent's instructions rely on; no unnecessary/unused tool; specifically, absence of `Edit`/`EnterWorktree`/`ExitWorktree` checked as a deliberate boundary, matching the precedent set by `verification-agent.md` and `gap-analysis-agent.md`).
- Out-of-Scope leakage scan against BRD Out-of-Scope terms, including the "flag, don't treat as a quality gap" handling already established by the two upstream gate agents.
- Role-separation check against `verification-agent.md` and `gap-analysis-agent.md`: does "review quality/correctness/consistency, not tests or coverage" hold up as a genuinely independent, non-duplicative third gate?
- Severity-and-loop mechanics check: is the Critical/High/Medium/Low scheme well-defined, is the blocking threshold unambiguous, and does the fix-then-re-review loop avoid both "review filed but never re-checked" and "re-review re-litigates the whole module from scratch"?
- "Bar calibrated to this app, not generic best practice" check — does the file actually constrain review severity to what BRD § Non-Functional Requirements and Scope call for, given this is explicitly a single-physician, single-clinic, moderate-volume app?
- Pipeline-consistency and environment-readiness check: whether the preconditions the agent assumes (a module that already cleared both upstream gates) currently hold in this workspace.
- Internal self-consistency: whether Non-Negotiable Rule, Strict Boundaries, and Output/Session Format agree with each other (no section permits something another section forbids, e.g. fixing findings directly, or letting the module finish with an open Critical/High).
- BRD Success Criteria and Non-Functional Requirements carry-forward check into the review criteria (performance, security, usability).

---

## 3. Findings

### 3.1 Frontmatter & Structural Conformance — Pass

`code-review-agent.md` mirrors the established shape of its six siblings: YAML frontmatter (`name`, `description`, `tools`, `model: inherit`), a role-establishing opening paragraph naming its place at the end of the pipeline (explicitly listing all five preceding stages by name), a source-of-truth list, `## Mission`, a Non-Negotiable Rule section, Strict Boundaries, a "What You Do" numbered list, `## Working Style`, and a defined Output/Session Format. Template consistency is maintained across all seven agents now present in `.claude\agents\`.

### 3.2 Tool Grant — Pass

`tools: Read, Grep, Glob, Bash, Write, AskUserQuestion` — identical to `verification-agent.md`'s and `gap-analysis-agent.md`'s tool grants. Every tool listed is used by the agent's own instructions: `Read/Grep/Glob` for reading the BRD/plan/existing codebase conventions and the module's actual code, `Bash` for scoping the review to the actual diff/changed files (§ Working Style: "prefer reading the actual diff/changed files... over the whole codebase" implies at least `git diff`-style inspection), `Write` for producing the review report, `AskUserQuestion` for genuinely debatable severity/defect calls. Deliberately and correctly **absent**: `Edit` (Strict Boundaries explicitly forbid fixing/patching findings directly — "review and remediation are separate roles, exactly as verification/remediation and scoring/remediation are kept separate," directly and correctly citing the identical precedent in the two upstream gate agents) and `EnterWorktree`/`ExitWorktree` (no branch/worktree lifecycle role, consistent with both upstream siblings). No orphaned tool, no missing tool.

### 3.3 Out-of-Scope Leakage Scan — Pass, Consistent Treatment with Upstream Gates

Strict Boundaries enumerate the identical BRD Out-of-Scope list (billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/multi-clinic, receptionist/multi-user access, follow-up alerts/reminders) already verified term-for-term in `Implementation_Verification.md` §3.3, `Worktree_Verification.md` §3.3, `Verification_Agent_Verification.md` §3.3, and `Gap_Analysis_Agent_Verification.md` §3.3. The instruction — "if you find such functionality *implemented*, flag it as an Out-of-Scope violation, not a quality note" — correctly keeps this check as a distinct, non-severity-ranked flag (its own "Out-of-Scope Flags" Output/Session Format section, separate from "Findings"), consistent with how both `verification-agent.md` and `gap-analysis-agent.md` isolate the identical check from their respective primary gate mechanisms (test verdict, coverage score). No drift in this recurring cross-agent check across all four post-planning agents.

### 3.4 Role Separation from verification-agent and gap-analysis-agent — Pass, Genuinely Independent (Not Duplicative)

The file states its distinguishing question explicitly and correctly in its opening paragraph: "A module can pass every test and cover every requirement and still be badly built — inconsistent, insecure, hard to maintain, or quietly duplicating logic that already exists elsewhere. That's what you check for." This is a real, non-circular third axis, distinct from both prior gates:
- `verification-agent.md` — does the code do what its own tests claim (execution-correctness, evidenced by running tests)?
- `gap-analysis-agent.md` — does the code cover everything the BRD/plan required (completeness, evidenced by requirement-by-requirement inspection)?
- `code-review-agent.md` — is the code that already passes and already covers requirements actually *good* (correctness beyond what tests happen to exercise, security, consistency, maintainability)?

No overlap-as-redundancy found: Strict Boundaries explicitly forbid re-running/re-judging tests and re-scoring coverage ("those are verification-agent's and gap-analysis-agent's responsibilities... If either is missing or FAILed for this module, say so and request that gate first"), and § What You Do step 1 requires confirming both upstream PASSes before starting — a correctly one-directional dependency chain (verify → score → review) with no circular re-checking.

**Precondition consistency, checked both directions:** exactly the same pattern already validated in `Gap_Analysis_Agent_Verification.md` §3.4 for the verify→score link holds here for the score→review link — this file states the downstream dependency correctly and completely (§ What You Do step 1), while `gap-analysis-agent.md` itself makes no forward claim about `code-review-agent.md` (expected and consistent with the user's earlier explicit instruction not to retrofit cross-references into prior sibling files).

### 3.5 Severity Scheme & Fix/Re-Review Loop Mechanics — Pass

The Critical/High/Medium/Low scheme (§ What You Do step 7) is well-defined with concrete criteria per tier ("bug/security hole that will cause incorrect behavior or a vulnerability in real use" for Critical; "real correctness/security/consistency risk, not yet observed failing but likely to" for High; etc.), and the blocking rule is stated unambiguously and consistently in three independent places — the Non-Negotiable Rule ("Never let a module finish with an open Critical or High finding unaddressed"), Strict Boundaries ("Do NOT let the module finish... while a Critical or High finding is open"), and the Output/Session Format's "Verdict" section ("PASS (no open Critical/High) or FAIL (open Critical/High findings)") — no section contradicts this threshold or leaves Medium/Low ambiguously blocking.

The fix-then-re-review loop directly answers the user's own framing ("Review feedback is addressed before finishing") and avoids both failure modes a review gate could fall into:
- **"Filed but never re-checked"** — closed by the Non-Negotiable Rule's first bullet ("A review is not complete when findings are written down; it's complete when every finding at or above the blocking severity has been resolved and you've re-checked the resolution yourself") and by § What You Do step 9 ("Hand back, then re-review... confirm closure before clearing the gate").
- **"Re-review re-litigates the whole module from scratch"** — closed explicitly by the Non-Negotiable Rule's third bullet ("re-review targets the specific findings that were open, confirms they're closed, and only flags something new if it was introduced by the fix itself") and by the source-of-truth list's inclusion of "any prior code-review-agent report for this same module... so you can confirm specifically what was fixed, not re-review the whole module from zero each pass," and by the Output/Session Format's "Scope Reviewed" section requiring the agent to state "whether this is an initial review or a re-review (and of which prior findings)."

This is a coherent, self-consistent loop design — no gap where a Critical/High finding could be silently dropped between review and re-review.

### 3.6 Bar Calibration to This App, Not Generic Best Practice — Pass

Strict Boundaries explicitly instruct: "Do NOT hold a module to a general 'best practices' bar disconnected from what BRD\Doc_BRD_Final.md actually needs — this is a single-physician, single-clinic app with moderate volume; don't demand enterprise-scale patterns (e.g., distributed caching, multi-tenant isolation) the BRD explicitly scopes out." This is directly and correctly grounded in `BRD\Doc_BRD_Final.md § Scalability` ("Designed for a single clinic with moderate patient volume") and § Users and Stakeholders ("Primary Users: General Physician (Single User)... Secondary Users: None"). This is a meaningful, non-generic addition — a review agent template that didn't include this instruction would risk generating findings the product doesn't actually need (e.g., flagging the absence of horizontal-scaling patterns), which would dilute genuine findings and contradict the BRD's own explicit scope boundary. Working Style's closing instruction ("Don't pad the review with restated verification/gap-analysis output") reinforces the same anti-noise discipline from a different angle.

### 3.7 Pipeline / Upstream-Dependency Consistency — Pass, Same Unmet Precondition as All Post-Planning Siblings, Most Fully Named

**Workspace check:** no `Planning\` directory, no implemented module code, and no `verification-agent`/`gap-analysis-agent` reports currently exist in this workspace (confirmed at review time). This means `code-review-agent.md` currently has nothing to review — consistent with, and one step further downstream than, the identical gap already documented in `Implementation_Verification.md` §3.5, `Worktree_Verification.md` §3.5, `Verification_Agent_Verification.md` §3.5, and `Gap_Analysis_Agent_Verification.md` §3.7. This agent's § What You Do step 1 ("Confirm upstream gates cleared... If either is missing or failing, stop and say code review cannot proceed until both pass") names its precondition-failure case explicitly, at the same level of clarity `gap-analysis-agent.md` set as the strongest precedent among the four post-planning agents (per `Gap_Analysis_Agent_Verification.md` §3.7) — no regression in self-check clarity at the end of the chain.

**Action implied (unchanged from prior verification passes):** run `planning-agent` per module, then `implementation-agent`/`worktree-agent`, then `verification-agent`, then `gap-analysis-agent`, before `code-review-agent` has anything real to review. This remains the single blocking precondition across all five post-planning agents.

### 3.8 BRD Success Criteria & Non-Functional Requirements Carry-Forward — Pass

Working Style requires BRD Success Criteria around speed and usability (consultation in 2–3 minutes, search/history in 2–5 seconds, page loads < 2 seconds, high usability with minimal training) to be treated as "legitimate code-quality concerns... not out of this agent's lane," with a concrete worked example ("an unindexed query on the search path, or a UI flow with unnecessary extra steps, is a real finding"). § What You Do step 4 explicitly requires checking the plan's Security Considerations were actually applied in the code — parameterized queries, input validation, auth guards, password hashing, session handling, encryption at rest/in transit — directly matching `BRD\Doc_BRD_Final.md § Non-Functional Requirements → Security`. This is a correct and non-redundant carry-forward relative to the other three post-planning agents: `verification-agent.md` checks for performance *risk* at test time, `gap-analysis-agent.md` scores whether the *requirement* for these criteria is met at all, and this agent is the one that inspects the actual code for the concrete quality defects (e.g., a missing index, an over-complicated flow) that would cause the criteria to be violated in practice.

### 3.9 Internal Self-Consistency — Pass

- Non-Negotiable Rule ("Never let a module finish with an open Critical or High finding unaddressed... A Low or informational finding may be explicitly accepted/deferred by the user") is consistent with § What You Do step 8 ("Critical/High findings block the module... Medium/Low findings are reported but don't block by default — note them and let the user decide") and with the Output/Session Format's "Verdict" and "Blocking Findings" sections, which only enumerate Critical/High as gating — no section contradicts which severities block.
- Strict Boundaries ("Do NOT fix, implement, or patch anything yourself") is consistent with § What You Do step 9 ("send Critical/High findings to the responsible building agent... re-review specifically those findings") and with the Output/Session Format's "Blocking Findings" section, which asks for "a precise action list for the building agent... what 'resolved' looks like," not a diff or patch — remediation never leaks into this agent's own output, mirroring the identical discipline verified in `Verification_Agent_Verification.md` §3.7 and `Gap_Analysis_Agent_Verification.md` §3.9 for its two upstream siblings.
- Strict Boundaries ("Do NOT flag or block on anything in the BRD's Out of Scope list as if it were a missing feature") is consistent with the Mission's framing of this agent as reviewing what's already built, not what's absent-by-design, and with the separate "Out-of-Scope Flags" Output/Session Format section (§3.3 above) — no path conflates an intentional scope exclusion with a quality defect.
- The Output/Session Format's "Scope Reviewed" section (stating initial-vs-re-review status) directly operationalizes the Non-Negotiable Rule's re-review-targeting requirement (§3.5 above) — no section allows a re-review to proceed without declaring what it's re-checking.
- No section grants an exception that another section forbids; no contradictory claims found.

---

## 4. Summary

| Check | Result |
|---|---|
| Frontmatter & structural conformance | Pass |
| Tool grant appropriateness | Pass |
| Out-of-Scope leakage scan | Pass |
| Role separation from verification-agent / gap-analysis-agent | Pass — independent third gate, not duplicative |
| Severity scheme & fix/re-review loop mechanics | Pass |
| Bar calibration to this app (not generic best practice) | Pass |
| Pipeline / upstream-dependency consistency | Pass (environment precondition unmet, explicitly self-handled) |
| BRD Success Criteria / NFR carry-forward | Pass |
| Internal self-consistency | Pass |

**Overall:** `code-review-agent.md` is internally consistent, fully traceable to `BRD\Doc_BRD_Final.md`, and correctly closes the six-agent pipeline as a genuinely independent third gate — checking code quality, correctness, security, and consistency that neither test execution (`verification-agent`) nor requirement-coverage scoring (`gap-analysis-agent`) would catch. Its severity scheme is unambiguous, its fix-then-re-review loop avoids both "filed but never re-checked" and "re-review from scratch" failure modes, and its review bar is explicitly calibrated to this app's actual scale rather than generic best practice. No changes to the agent definition are required.

**Outstanding blocker (workspace-level, not agent-level, unchanged from prior verification passes):** no `Planning\` directory, no implemented module code, and no `verification-agent`/`gap-analysis-agent` reports exist yet. Run `planning-agent`, then `implementation-agent`/`worktree-agent`, then `verification-agent`, then `gap-analysis-agent`, before `code-review-agent` has real work to review.
