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

**Current direction (2026-09-24):** Keep Tenants as the obligated adopter. D-7 is not invoked. Request a dated feasibility answer from a Tenants maintainer before reconsidering the fallback. Story 12.2 remains conditional and in backlog; this direction is not a D-7 Product decision or G-6 evidence.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md` Story 12.2 -- source acceptance criteria; the fallback is conditional.
- `_bmad-output/planning-artifacts/prd.md` G-6, SM-1, D-7, OI-5 -- milestone authority and unchanged proof obligation; reference rather than copy evidence.
- `_bmad-output/implementation-artifacts/epic-12-context.md` -- epic boundaries and cross-story dependency.
- `_bmad-output/implementation-artifacts/spec-12-1-i-adopter-proof-kit.md` and `docs/how-to/adopter-bootstrap-proof.md` -- completed kit and its selected-maintainer instructions; reuse, do not reimplement or rerun external proof.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- 12.2 is backlog, ADOPT-APP-1 open, and EXT-ADOPTER-1 external; update only after a dated decision.
- `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md` -- expected external evidence path; absent locally. Parties equivalent is also absent. Neither is written by FrontComposer.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/contracts/adopt-app-1-adopter-fallback-decision.md` -- create the dated Product decision with condition, choice, rationale, accountable maintainer, unchanged proof requirement, and links to Story 12.1, the kit guide, PRD G-6/SM-1/D-7/OI-5, and EXT-ADOPTER-1.
- [ ] `docs/how-to/adopter-bootstrap-proof.md` -- make the selected maintainer and evidence repository unambiguous while preserving the existing proof procedure and keeping the gate open.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- mark 12.2 and ADOPT-APP-1 according to the recorded choice; keep EXT-ADOPTER-1 external and G-6 open.
- [ ] `_bmad-output/planning-artifacts/prd.md` -- link the decision from D-7/G-6 and identify the selected owner without rewriting the unchanged proof requirement or claiming acceptance.

**Acceptance Criteria:**
- Given Tenants cannot supply the proof and Product invokes D-7, when the decision is filed, then it dates and explains a Parties selection or milestone hold and names the accountable adopter maintainer.
- Given Parties is selected, when ownership is reconciled, then Parties owes the same dated, candidate-bound three-call projection and command proof and neither G-6 nor EXT-ADOPTER-1 is marked complete.
- Given a hold is selected, when readiness is checked, then the reason and owner are visible and G-6 remains open.
- Given no dated Product decision, when readiness is checked, then Tenants remains obligated and no fallback is inferred.

## Implementation Notes

- 2026-09-24: The user selected the no-fallback path. Tenants remains obligated, Story 12.2 stays in backlog, and no D-7 decision artifact or external proof was created.
- 2026-09-24: Requested a dated Tenants maintainer feasibility answer in `Hexalith/Hexalith.Tenants#48` (`https://github.com/Hexalith/Hexalith.Tenants/issues/48`). The issue is a request, not proof or Product approval.
- 2026-09-24: User identified as `jpiquot` and confirmed Tenants can run the kit with a target run date of 2026-09-24. Posted the dated feasibility answer at `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817694948`, proposing candidate `1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4` only if its packages verify. The actual adopter result remains outstanding.
- 2026-09-24: The candidate-bound clean-consumer kit passed all four assertions; see `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-adopter-kit-run-2026-09-24.md` and the issue update `https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817818115`. It renders the kit's `Proof` domain, not Tenants-generated surfaces, so G-6 and EXT-ADOPTER-1 remain open and D-7 stays uninvoked.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `git diff --check` -- expected: no whitespace errors.

**Manual checks:**
- Confirm the decision, guide, PRD, and sprint status agree on selected owner and proof obligation, and none claims external proof exists.
