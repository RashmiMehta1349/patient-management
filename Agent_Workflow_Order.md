# Agent Workflow Order

This document defines the sequence in which agents work on each module of the Patient Management Application.

## Sequence

1. **brainstorming-agent**
   Clarifies scope and requirements for a new module or ambiguous feature. Skip if requirements are already settled in `BRD\Doc_BRD_Final.md` or `Modules\`.

2. **planning-agent**
   Produces the module's technical plan at `Planning\NN_*_Plan.md` — architecture, workflows, data model, APIs, UI, implementation tasks, test strategy, and acceptance criteria.

3. **worktree-agent** (or **implementation-agent** if no git isolation is needed)
   Builds the code per the approved plan. `worktree-agent` isolates the work in a dedicated git worktree; `implementation-agent` builds directly in the current working tree.

4. **verification-agent** — Gate
   Runs the full test suite and checks results against the plan's Acceptance Criteria and Test Strategy, plus `BRD\Doc_BRD_Final.md`. Must PASS before proceeding.

5. **gap-analysis-agent** — Gate
   Scores the implementation against the BRD and plan, requirement by requirement. Must reach ≥95% coverage before proceeding; otherwise loops back to step 3 with a gap list.

6. **code-review-agent** — Gate
   Reviews the implementation for correctness, code quality, security, and consistency with the rest of the codebase. Must have no open Critical/High findings before proceeding; otherwise loops back to step 3.

7. **finishing-agent**
   Presents explicit options to merge, create a PR, or clean up the worktree. Takes no action without the user's explicit choice.

## Notes

- Steps 4–6 are gates: each can send work back to step 3 (implementation) if it fails. Nothing advances past a failing gate.
- Verification artifacts for each agent's own behavior are tracked separately under `Verification\`.
