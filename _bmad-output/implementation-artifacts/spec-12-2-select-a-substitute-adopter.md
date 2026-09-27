---
title: 'Story 12.2: Select a Substitute Adopter'
type: 'chore'
created: '2026-09-24'
status: 'done'
baseline_commit: '1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 12.2 (ADOPT-APP-1) applies only when Hexalith.Tenants cannot supply the G-6 adopter proof and Product invokes the D-7 fallback. On 2026-09-26 the Product Owner accepted the Tenants proof for candidate `05d122005d328ec8f15ecd276058be29730288c2`, closing G-6, meeting SM-1, and recording EXT-ADOPTER-1 as done, all without invoking D-7. The trigger never fired, yet the story stays open in the backlog and the current planning text still describes it as pending.

**Approach:** Record the 2026-09-27 decision, made by the user acting as Product Owner, to close Story 12.2 / ADOPT-APP-1 as not required. D-7 is not invoked, Parties is not selected, no hold is recorded, and no fallback decision artifact is created. Align the current-state text in the sprint status, epics, and PRD with this closure, and leave dated historical records unchanged. D-7 remains PRD policy: if a later readiness candidate needs fresh adopter proof that Tenants cannot supply, that requires a new dated Product decision and a new story. This closure does not reopen or weaken G-6, SM-1, or EXT-ADOPTER-1, and it grants no D-9 re-approval, release authorization, or readiness milestone.

**Renegotiated 2026-09-27:** This decision replaces the 2026-09-24 and 2026-09-25 direction to keep Story 12.2 conditional in the backlog. That direction stays in the Implementation Notes as history.

</frozen-after-approval>

## Implementation Notes

- 2026-09-24: The user selected the no-fallback path. Tenants remains obligated, Story 12.2 stays in backlog, and no D-7 decision artifact or external proof was created.
- 2026-09-24: Requested a dated Tenants maintainer feasibility answer in `Hexalith/Hexalith.Tenants#48` (`https://github.com/Hexalith/Hexalith.Tenants/issues/48`). The issue is a request, not proof or Product approval.
- 2026-09-24: User identified as `jpiquot` and confirmed Tenants can run the kit with a target run date of 2026-09-24. Posted the dated feasibility answer at `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817694948`, proposing candidate `1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4` only if its packages verify. The actual adopter result remains outstanding.
- 2026-09-24: The candidate-bound clean-consumer kit passed all four assertions; see `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-adopter-kit-run-2026-09-24.md` and the issue update `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817818115`. It renders the kit's `Proof` domain, not Tenants-generated surfaces, so G-6 and EXT-ADOPTER-1 remain open and D-7 stays uninvoked.

- 2026-09-24: Kept Tenants; no D-7. Source preflight 8/8; named proof requested in Tenants #48. G-6 open.
- 2026-09-25: User kept Tenants. Proposed 2026-09-29 for proof or blocker/ETA in `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5828423445`. No D-7.
- 2026-09-26: Inspected the pinned Tenants acceptance Markdown and JSON. They report a passing 2026-09-25 independent run against FrontComposer `05d122005d328ec8f15ecd276058be29730288c2`, with verified package identity and generated Tenants projection/command, invalid-bootstrap, and empty-registry assertions. This was evidence receipt, not itself Product acceptance.
- 2026-09-26: Explicit Product Owner review accepted the Tenants proof for G-6 and SM-1 in `_bmad-output/implementation-artifacts/tests/tenants-g6-product-decision-2026-09-26.md`; OI-5 and EXT-ADOPTER-1 closed. D-7 was not invoked and Story 12.2 / ADOPT-APP-1 remains conditional in the backlog.
- 2026-09-27: The build resumed from `ready-for-dev`. Every task in that spec was conditional on a D-7 invocation, and its Never rule forbade fabricating a Product decision. The user was asked how to disposition the story and chose to close it as not required, which replaced the previous `dispatch` plan with this one-shot disposition. The dated historical records were not edited: `tests/tenants-g6-product-decision-2026-09-26.md`, `tests/mcp-g7-security-review-2026-09-26.md`, `planning-artifacts/sprint-change-proposal-2026-09-22.md`, the historical approved-change table in `epics.md`, and the `prds/prd-frontcomposer-2026-09-08/` reports. `baseline_commit` keeps its original 2026-09-24 value; this disposition starts from HEAD `932e5485f6294a87baa156e858aea88bebd9ff08`.
- 2026-09-27: `sprint-status.yaml` now records development status `12-2` as `done`, with a dated reason comment, and ADOPT-APP-1 as `not-required`. `not-required` is a new status added to the Approved Change Backlog definitions; nothing else parses that block. `epic-12` stays `in-progress` until the epic is closed manually or its optional retrospective runs.
- 2026-09-27: `epics.md` now has the closure in the Epic 12 implementation notes, the epic work table, and the Story 12.2 section, which also gains a disposition paragraph. `prd.md` has it in the D-7 and OI-5 rows, and its `updated` date is now 2026-09-27. The cached `epic-12-context.md` was later patched directly (see the review patches note).
- 2026-09-27: Decision owner: Product Owner, acting as the user of this build session (git author Jérôme Piquot, identified as `jpiquot` in the 2026-09-24 note). The decision was captured through the build's disposition question, and the user chose "Close as not required". The worktree already had an unrelated `references/Hexalith.EventStore` gitlink modification. It was left unstaged and is not part of this change, because that gitlink is G-3 runtime identity.
- 2026-09-27: Verification results. `git diff --check` was clean. `sprint-status.yaml` parses as YAML. `python3 eng/validate-story-artifacts.py --skip-sentinel` reports only `E11R-AI-1 implementation_story must be 11.25, got '(missing)'`, and a HEAD snapshot reports the same row, so it predates this change. The commit message passed `node_modules/.bin/commitlint --edit` (`@commitlint/cli@21.2.2`, exit 0).
- 2026-09-27: Review patches. The Approved Change Backlog legend now defines `done`. The Story 12.2 disposition in `epics.md` says the acceptance criteria were not exercised. The EXT-ADOPTER-1 paragraph now points to that disposition instead of repeating it. `epics.md` has `updated: 2026-09-27`. The PRD D-7 row now matches the spec's "new dated Product decision and a new story". The cached `epic-12-context.md` cross-story line reflects the closure, because it is tracked in git and its mtime check cannot detect staleness after a checkout.

## Review Triage Log

- Sprint status 12-2 set to `done` ahead of the one-shot `review` sync: false. The user explicitly chose `done`, and this one-shot Blind Hunter pass is the review gate. The `review` sync correctly stops at a later status.
- `done` hides that 12.2 was not delivered: low, rejected. `SPRINT_LIFECYCLE_STATUSES` in `eng/validate-story-artifacts.py:197` admits only `backlog/blocked/done/in-progress/optional/ready-for-dev/review`, so a new story status would fail the validator. The dated comment and this spec carry the disposition.
- Backlog legend lacks `done`: low, patched. The claim that `external` is unused is false, because `EXT-BUILDS-1` is `external`.
- Story 12.2 acceptance criteria are not dispositioned, and the closure is stated twice in `epics.md`: low, patched. The heading is unchanged because the sprint key derives from it.
- Nothing defines when D-7 can fire again: false for this change, because the D-7 row stays in PRD section 12 as policy. The spec-versus-PRD wording mismatch was low and was patched.
- The closure has no named approver and the record is `in-progress`: low, patched with the decision-owner note, and the spec is now `done`.
- Unrelated EventStore gitlink: medium if committed, patched by staging exact paths only.
- `epics.md` `updated:` not bumped: low, patched.
- PRD section 8.2 not restated from sprint status: low, deferred. The gap predates this change, since the 2026-09-26 update did not restate it either, and a full restatement covers more than Epic 12.
- Epic 12 closure has no owner or trigger: low, deferred. Moving `epic-12` to `done` or running the optional retrospective is a separate user choice.
- Cached `epic-12-context.md` is stale: low, patched.
- No verification recorded: low, patched with the verification note above.
