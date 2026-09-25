---
title: 'Story 12.2: Select a Substitute Adopter'
type: 'feature'
created: '2026-09-24'
status: 'draft'
baseline_commit: '1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 12.1 supplies the adopter proof kit, but the independent Tenants proof required for G-6 is absent. A missing result does not establish that Tenants cannot supply it or authorize a substitute. If the D-7 fallback is invoked, the Product decision needs a durable record that identifies who owes the unchanged external proof.

**Approach:** Record the Product Owner's dated choice to select Hexalith.Parties or hold the readiness milestone, with the reason Tenants cannot provide proof and the accountable adopter maintainer. Link the decision to the approved kit and milestone sources, then align the current ownership and tracking text with that choice.

## Boundaries & Constraints

**Always:** Tenants remains obligated until a dated Product decision invokes D-7. A Parties selection transfers EXT-ADOPTER-1 to its named maintainer with the same candidate-bound three-call, generated-projection, generated-command, date, identity, and redaction requirements. G-6 and SM-1 remain open until independent adopter evidence is reviewed. A hold records why the milestone remains open.

**Never:** Infer that Tenants cannot deliver from the missing proof alone; fabricate Product approval, an accountable maintainer, or an execution result; treat selection or an internal fixture pass as G-6 acceptance; add an evidence schema, test lane, or wrapper report.

**Current direction (reaffirmed 2026-09-24):** Keep Tenants as the obligated adopter. Its maintainer confirmed kit feasibility and passed the clean-consumer fixture. Pursue dated proof of Tenants-generated projection and command surfaces. D-7 is not invoked; Story 12.2 stays in backlog, and G-6, SM-1, and EXT-ADOPTER-1 stay open until independent proof is reviewed. This direction is not a D-7 fallback decision or G-6 evidence.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md` -- 12.2, G-6, SM-1, D-7, OI-5; require a dated Product choice and unchanged external proof.
- `_bmad-output/implementation-artifacts/spec-12-1-i-adopter-proof-kit.md` and `docs/how-to/adopter-bootstrap-proof.md` -- completed kit and proof instructions.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- 12.2 backlog, ADOPT-APP-1 open, EXT-ADOPTER-1 external; change only after a dated choice.
- `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-adopter-kit-run-2026-09-24.md` -- kit fixture passed; pinned Tenants lacks `tests/tenants-bootstrap-acceptance.md` proof.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Composition/` -- `TenantSummaryProjection` and `CreateTenantCommand` in Tenants `origin/main` commit `ecec9586c7dece7f092011a3737d89b2d7009ef3` adds generated surfaces; proof remains outstanding.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/contracts/adopt-app-1-adopter-fallback-decision.md` -- if D-7 is invoked, file the dated choice, Tenants inability reason, accountable maintainer, unchanged proof obligation, and links to Story 12.1, guide, PRD gates, and EXT-ADOPTER-1.
- [ ] `docs/how-to/adopter-bootstrap-proof.md` -- identify the selected maintainer and evidence repository; retain the proof procedure and open gate.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- reflect the decision; leave EXT-ADOPTER-1 external.
- [ ] `_bmad-output/planning-artifacts/prd.md` -- link the decision at D-7/G-6 and identify the owner; retain G-6/SM-1 as open.

**Acceptance Criteria:**
- Given Tenants cannot supply named-module proof and Product invokes D-7, when the record is filed, then it dates and explains Parties selection or a hold and names the accountable maintainer.
- Given Parties is selected, when ownership is reconciled, then its maintainer owes the same candidate-bound three-call, generated-projection, and generated-command proof; G-6 and EXT-ADOPTER-1 stay open.
- Given a hold, when readiness is checked, then the reason and owner are visible and G-6 stays open.
- Given no dated Product decision, when readiness is checked, then Tenants remains obligated and no fallback is inferred.

## Implementation Notes

- 2026-09-24: The user selected the no-fallback path. Tenants remains obligated, Story 12.2 stays in backlog, and no D-7 decision artifact or external proof was created.
- 2026-09-24: Requested a dated Tenants maintainer feasibility answer in `Hexalith/Hexalith.Tenants#48` (`https://github.com/Hexalith/Hexalith.Tenants/issues/48`). The issue is a request, not proof or Product approval.
- 2026-09-24: User identified as `jpiquot` and confirmed Tenants can run the kit with a target run date of 2026-09-24. Posted the dated feasibility answer at `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817694948`, proposing candidate `1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4` only if its packages verify. The actual adopter result remains outstanding.
- 2026-09-24: The candidate-bound clean-consumer kit passed all four assertions; see `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-adopter-kit-run-2026-09-24.md` and the issue update `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817818115`. It renders the kit's `Proof` domain, not Tenants-generated surfaces, so G-6 and EXT-ADOPTER-1 remain open and D-7 stays uninvoked.

- 2026-09-24: Kept Tenants; no D-7. Source preflight 8/8; named proof requested in Tenants #48. G-6 open.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `git diff --check` -- expected: no whitespace errors.

**Manual checks:**
- Confirm the decision, guide, PRD, and sprint status agree on selected owner and proof obligation, and none claims external proof exists.
