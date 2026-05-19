# Release Candidate Accessibility and Stakeholder Evidence Pack - 2026-05-15

Evidence pack version: `2026-05-15.story-12.5.v1`
Owner story: `12-5-accessibility-and-stakeholder-acceptance-evidence-pack`
Release candidate identity: current Epic 12 release-certification working tree; final branch, tag, and commit must be rebound before package promotion.
Prepared date: 2026-05-15
Prepared by: GPT-5 Codex (AI-agent identifier valid as pack preparer only; not valid as `tester_or_reviewer` for any manual AT or device `completed` row)

This pack records release-readiness evidence without inferring manual results from automation. Manual assistive-technology, tablet, phone, and stakeholder gates are blocked until dated manual evidence or auditable approvals are attached.

## Machine-Readable Classification

```yaml
evidence_pack_version: "2026-05-15.story-12.5.v1"
owner_story: "12-5-accessibility-and-stakeholder-acceptance-evidence-pack"
final_classification: "blocked"
classification_reason: "Required manual AT, real-device, and stakeholder acceptance gates do not yet have dated manual evidence or auditable approvals."
release_candidate:
  branch_or_tag: "unbound-current-working-tree"
  commit: "unbound"
  freshness_rule: "Revalidate or explicitly accept stale evidence if branch, tag, commit, browser, OS, AT version, UX baseline, or responsive-tier assumptions change."
residual_gates:
  - "AT-NVDA-FIREFOX"
  - "AT-JAWS-CHROME"
  - "AT-VOICEOVER-SAFARI"
  - "DEVICE-TABLET"
  - "DEVICE-PHONE-FALLBACK"
  - "STAKEHOLDER-PRODUCT"
  - "STAKEHOLDER-QUALITY"
  - "STAKEHOLDER-RELEASE-OWNER"
  - "STAKEHOLDER-ACCESSIBILITY"
blockers:
  - "Manual screen-reader evidence missing for all required pairings."
  - "Manual tablet and phone fallback evidence missing."
  - "Product, Quality/Test, Release Owner, and Accessibility/Stakeholder sign-offs missing."
accepted_constraints: []
# post_v1_roadmap_refs is a list of `story_or_roadmap_ref` values (one per row in the
# Post-v1 Roadmap Register). The plural key reflects list cardinality; each entry
# carries the singular contract field name semantics.
post_v1_roadmap_refs:
  - "ROADMAP-CROSS-AT-EXPANSION"
  - "ROADMAP-BROAD-LOCALIZATION"
  - "ROADMAP-RTL-MATRIX"
  - "ROADMAP-ZOOM-MATRIX"
  - "ROADMAP-FORCED-COLORS-MATRIX"
  - "ROADMAP-REDUCED-MOTION-MATRIX"
sign_off_refs: []
sanitization_status: "repository-markdown-only; no screenshots, transcripts, cookies, payloads, secrets, tenant/user values, local absolute paths, full DOM dumps, or unbounded logs added"
```

## Pre-Edit Evidence Inventory

| Evidence area | Current source | Evidence type | Current status | Gap before Story 12.5 edits |
| --- | --- | --- | --- | --- |
| Manual log requirements | `docs/accessibility-verification/README.md`, `docs/accessibility-verification/manual-log-template.md` | Template | Representative template existed | Missing stable gate ids, status contract, release impact, approvals, redaction status, final classification fields |
| Automated specimen routes | `tests/e2e/specimens/frontcomposer-specimen-manifest.json` | Automated manifest | Completed for `/__frontcomposer/specimens/type` and `/__frontcomposer/specimens/data-formatting` | Does not prove manual screen-reader, real-device, or stakeholder acceptance |
| Automated accessibility and visual baseline | Story `10-2-accessibility-ci-gates-and-visual-specimen-verification` | Automated Playwright/axe/keyboard/focus/media/visual evidence | Completed for committed specimen surfaces | Does not cover manual AT announcement quality or stakeholder sign-off |
| Representative Shell/dev-mode/localization evidence | Story `11-6-shell-ux-accessibility-and-sample-coverage-follow-ups` and `11-6-row-evidence-matrix.md` | Representative fixes, accepted risks, split rows | Completed for Story 11.6 scope | Broad cross-AT, localization, RTL, and additional media matrices remain split or roadmap scope |
| UX responsive/accessibility requirements | `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md` | Product/UX specification | Defines required gates | Manual NVDA, JAWS, VoiceOver, tablet, and phone evidence not yet captured for this release candidate |
| Stakeholder acceptance | No repository-visible Story 12.5 sign-off artifact before this pack | Manual approval | Missing | Product, Quality/Test, Release Owner, and Accessibility/Stakeholder approvals are not auditable |

## Required Release Gates

The required gates for this pack are: NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari, tablet, phone fallback, cross-AT, localization, RTL, zoom, forced-colors, reduced-motion, Product acceptance, Quality/Test acceptance, Release Owner acceptance, and Accessibility/Stakeholder acceptance.

## Manual Screen-Reader Matrix

| Gate id | Task ids | AC ids | Pairing | UX baseline or responsive tier | Status | Owner | Release impact | Blocker ref | Decision needed | Reopen event |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AT-NVDA-FIREFOX | T3 | AC3, AC4, AC5, AC21, AC22, AC23, AC32 | NVDA + Firefox | Desktop baseline; Comfortable density per UX responsive/accessibility spec | blocked | Product/Quality owner | v1 cannot be classified `ready` until dated manual NVDA + Firefox evidence is captured or the gate receives approved accepted-constraint treatment | No Quality team manual-AT session is scheduled for the release candidate; FB-MANUAL-AT-MISSING in Open Feedback Register tracks the blocker | Execute manual check on bound release candidate or collect Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval for an accepted v1 constraint | Release branch/tag/commit, NVDA, Firefox, OS, route/flow, or UX baseline changes |
| AT-JAWS-CHROME | T3 | AC3, AC4, AC5, AC21, AC22, AC23, AC32 | JAWS + Chrome | Desktop baseline; Comfortable density per UX responsive/accessibility spec | blocked | Product/Quality owner | v1 cannot be classified `ready` until dated manual JAWS + Chrome evidence is captured or the gate receives approved accepted-constraint treatment | No Quality team manual-AT session is scheduled for the release candidate; FB-MANUAL-AT-MISSING in Open Feedback Register tracks the blocker | Execute manual check on bound release candidate or collect Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval for an accepted v1 constraint | Release branch/tag/commit, JAWS, Chrome, OS, route/flow, or UX baseline changes |
| AT-VOICEOVER-SAFARI | T3 | AC3, AC4, AC5, AC21, AC22, AC23, AC32 | VoiceOver + Safari | Desktop baseline; Comfortable density per UX responsive/accessibility spec | blocked | Product/Quality owner | v1 cannot be classified `ready` until dated manual VoiceOver + Safari evidence is captured or the gate receives approved accepted-constraint treatment | No Quality team manual-AT session is scheduled for the release candidate; FB-MANUAL-AT-MISSING in Open Feedback Register tracks the blocker | Execute manual check on bound release candidate or collect Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval for an accepted v1 constraint | Release branch/tag/commit, VoiceOver, Safari, macOS, route/flow, or UX baseline changes |

No screen-reader pairing is marked completed in this pack.

## Real-Device Matrix

| Gate id | Task ids | AC ids | Device tier | UX commitment | Status | Owner | Release impact | Blocker ref | Decision needed | Reopen event |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DEVICE-TABLET | T3 | AC7, AC21, AC22, AC23, AC32 | Tablet, 768px-1023px | Touch-adapted experience, all features functional, 44px touch targets | blocked | Product/Quality owner | v1 cannot be classified `ready` until dated tablet evidence is captured or accepted as a constraint | No Quality team manual-device session is scheduled for the release candidate; FB-DEVICE-MISSING in Open Feedback Register tracks the blocker | Execute tablet check on bound release candidate or collect Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval for an accepted v1 constraint | Release branch/tag/commit, browser, OS/device, route/flow, or responsive-tier assumptions change |
| DEVICE-PHONE-FALLBACK | T3 | AC7, AC8, AC21, AC22, AC23, AC32 | Phone, <768px | Functional fallback; usable but not optimized and not a v1 daily-use target | blocked | Product/Quality owner | v1 cannot be classified `ready` until dated phone fallback evidence is captured or accepted as a constraint | No Quality team manual-device session is scheduled for the release candidate; FB-DEVICE-MISSING in Open Feedback Register tracks the blocker | Execute phone fallback check on bound release candidate or collect Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval against the documented fallback commitment | Release branch/tag/commit, browser, OS/device, route/flow, or responsive-tier assumptions change |

No real-device gate is marked completed in this pack.

## Broader Accessibility Classification

| Gate id | Task ids | AC ids | Scope | Status | Owner | Evidence ref | Release impact | Reopen event |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ROADMAP-CROSS-AT-EXPANSION | T4 | AC9, AC17, AC25, AC27 | Cross-AT coverage beyond required NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari pairings | post-v1 roadmap | Product/UX accessibility roadmap owner | Story 11.6 defers full cross-assistive-technology matrices; Story 10.2 automation is not manual AT evidence | Non-blocking only after the required AT pairings are completed or approved as constraints | New supported AT/browser policy, adopter request, or release-owner decision |
| ROADMAP-BROAD-LOCALIZATION | T4 | AC9, AC17, AC25, AC27 | Broad localized layout and screen-reader quality beyond representative EN/FR evidence | post-v1 roadmap | Product/UX localization roadmap owner | Story 11.6 completed representative localization hardening and deferred broad localization matrices | Non-blocking for v1 unless Product marks broad localization as release-blocking | New v1 locale commitment, adopter locale requirement, or localization defect |
| ROADMAP-RTL-MATRIX | T4 | AC9, AC17, AC25, AC27 | Full RTL visual, keyboard, and screen-reader matrix | post-v1 roadmap | Product/UX accessibility roadmap owner | Story 10.2 and Story 11.6 identify RTL/broader visual matrices as deferred beyond deterministic specimen scope | Non-blocking for v1 unless Product marks RTL as a release target | New RTL adopter requirement, release policy change, or RTL defect |
| ROADMAP-ZOOM-MATRIX | T4 | AC9, AC16, AC17, AC25, AC27 | Browser zoom and text-resize coverage beyond current deterministic specimen automation | post-v1 roadmap | Product/Quality accessibility roadmap owner | Story 10.2 automated specimen zoom/reflow evidence covers current v1 specimen gates; broader zoom matrix is outside this evidence pack | Non-blocking for v1 specimen scope; cannot be used to claim broad zoom coverage | New zoom policy, baseline route change, or defect in automated zoom evidence |
| ROADMAP-FORCED-COLORS-MATRIX | T4 | AC9, AC16, AC17, AC25, AC27 | Forced-colors / high-contrast mode coverage beyond current deterministic specimen automation | post-v1 roadmap | Product/Quality accessibility roadmap owner | Story 10.2 automated specimen forced-colors evidence covers current v1 specimen gates; broader forced-colors matrix is outside this evidence pack | Non-blocking for v1 specimen scope; cannot be used to claim broad forced-colors coverage | New forced-colors policy, baseline route change, or defect in automated forced-colors evidence |
| ROADMAP-REDUCED-MOTION-MATRIX | T4 | AC9, AC16, AC17, AC25, AC27 | Reduced-motion / prefers-reduced-motion coverage beyond current deterministic specimen automation | post-v1 roadmap | Product/Quality accessibility roadmap owner | Story 10.2 automated specimen reduced-motion evidence covers current v1 specimen gates; broader reduced-motion matrix is outside this evidence pack | Non-blocking for v1 specimen scope; cannot be used to claim broad reduced-motion coverage | New reduced-motion policy, baseline route change, or defect in automated reduced-motion evidence |

The completed automated baseline remains supporting evidence only: Story 10.2 owns axe, keyboard, focus, forced-colors, reduced-motion, zoom/reflow, visual baseline, and specimen-manifest checks for committed specimen surfaces. This pack does not inflate that evidence into broad manual accessibility validation.

### Per-row `post-v1 roadmap` rule

Per-row `post-v1 roadmap` status is provisional. AC23 is satisfied at pack level because the overall `final_classification` is `blocked`. When a future pack proposes `ready` or `ready-with-accepted-constraints`, each ROADMAP-* row must obtain Product, Accessibility/Stakeholder, and Release Owner approval references (recorded in the Post-v1 Roadmap Register) before being claimed as roadmap rather than `blocked`.

## Accepted Constraints Register

| Constraint id | Gate id | Status | Owner | Likelihood | Release impact | Downstream consumer impact | Adopter communication need | Evidence ref | Expiry or revalidation trigger | Approval refs (Product, Quality/Test, Accessibility/Stakeholder, Release Owner) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

_The register has no rows. No accepted v1 constraints are recorded in this pack; the YAML `accepted_constraints: []` is canonical._

Any future accepted v1 constraint must link Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approvals (four refs) before this pack can classify as `ready-with-accepted-constraints`.

## Post-v1 Roadmap Register

| Roadmap id | Gate id | Owner | Story or roadmap ref | Target release or rationale | Release impact | Reopen event | Approval refs (Product, Accessibility/Stakeholder, Release Owner) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ROADMAP-CROSS-AT-EXPANSION | ROADMAP-CROSS-AT-EXPANSION | Product/UX accessibility roadmap owner | Product/UX accessibility roadmap after Story 12.5 | Outside v1 unless Product expands supported AT matrix beyond required pairings | Non-blocking only after required manual AT pairings are resolved | New AT/browser support policy, adopter request, or release-owner decision | Pending; required only when pack classification advances beyond `blocked` |
| ROADMAP-BROAD-LOCALIZATION | ROADMAP-BROAD-LOCALIZATION | Product/UX localization roadmap owner | Product/UX localization roadmap after Story 12.5 | Outside v1 broad release evidence; representative EN/FR evidence exists from Story 11.6 | Non-blocking unless Product makes broad localization a v1 promise | New locale commitment, adopter locale requirement, or localization defect | Pending; required only when pack classification advances beyond `blocked` |
| ROADMAP-RTL-MATRIX | ROADMAP-RTL-MATRIX | Product/UX accessibility roadmap owner | Product/UX RTL roadmap after Story 12.5 | Outside v1 broad release evidence; deterministic specimen scope remains the baseline | Non-blocking unless RTL support becomes a v1 release target | New RTL adopter requirement, release policy change, or RTL defect | Pending; required only when pack classification advances beyond `blocked` |
| ROADMAP-ZOOM-MATRIX | ROADMAP-ZOOM-MATRIX | Product/Quality accessibility roadmap owner | Product/Quality media-matrix roadmap after Story 12.5 | Automated specimen zoom checks exist; broader zoom matrix is outside this story | Non-blocking for current specimen scope | New zoom policy, route change, or zoom defect | Pending; required only when pack classification advances beyond `blocked` |
| ROADMAP-FORCED-COLORS-MATRIX | ROADMAP-FORCED-COLORS-MATRIX | Product/Quality accessibility roadmap owner | Product/Quality media-matrix roadmap after Story 12.5 | Automated specimen forced-colors checks exist; broader forced-colors matrix is outside this story | Non-blocking for current specimen scope | New forced-colors policy, route change, or forced-colors defect | Pending; required only when pack classification advances beyond `blocked` |
| ROADMAP-REDUCED-MOTION-MATRIX | ROADMAP-REDUCED-MOTION-MATRIX | Product/Quality accessibility roadmap owner | Product/Quality media-matrix roadmap after Story 12.5 | Automated specimen reduced-motion checks exist; broader reduced-motion matrix is outside this story | Non-blocking for current specimen scope | New reduced-motion policy, route change, or reduced-motion defect | Pending; required only when pack classification advances beyond `blocked` |

## Stakeholder Acceptance

| Gate id | Task ids | AC ids | Stakeholder group | Status | Approver | Date | Scope | Evidence path | Open-feedback ref | Release condition |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| STAKEHOLDER-PRODUCT | T5 | AC12, AC13, AC20, AC23, AC24, AC28, AC31, AC34 | Product | blocked | unassigned | pending | Story 12.5 release readiness | None | FB-STAKEHOLDER-SIGNOFF-MISSING (blocking) | Product sign-off required before `ready` or `ready-with-accepted-constraints` |
| STAKEHOLDER-QUALITY | T5 | AC12, AC13, AC20, AC23, AC24, AC28, AC31, AC34 | Quality/Test | blocked | unassigned | pending | Story 12.5 release readiness | None | FB-STAKEHOLDER-SIGNOFF-MISSING (blocking) | Quality/Test sign-off required before release classification can advance |
| STAKEHOLDER-RELEASE-OWNER | T5 | AC12, AC13, AC20, AC23, AC24, AC28, AC31, AC34 | Release Owner | blocked | unassigned | pending | Story 12.5 release readiness | None | FB-STAKEHOLDER-SIGNOFF-MISSING (blocking) | Release Owner sign-off required before release classification can advance |
| STAKEHOLDER-ACCESSIBILITY | T5 | AC12, AC13, AC20, AC23, AC24, AC28, AC31, AC34 | Accessibility/Stakeholder | blocked | unassigned | pending | Story 12.5 release readiness | None | FB-STAKEHOLDER-SIGNOFF-MISSING (blocking) | Accessibility/Stakeholder sign-off required before release classification can advance |

Canonical sentinels in this table: `unassigned` indicates an approver slot that has not yet been claimed; `pending` indicates a date that is not yet set. Both are non-enum values reserved for empty-state rows and must not appear in a `completed` row.

No delegated or proxy approvals are recorded. Any future proxy approval must record delegating principal, delegate or proxy identity, delegation authority (auditable reference), scope, expiration, approving role, date, and an auditable evidence pointer per AC34/D19; the manual log template provides the field set.

## Open Feedback Register

| Feedback id | Source | Classification | Owner | Rationale | Release decision |
| --- | --- | --- | --- | --- | --- |
| FB-MANUAL-AT-MISSING | Story 12.5 evidence audit | blocking | Product/Quality owner | Required manual AT evidence is absent for all required pairings | Blocks `ready`; requires manual evidence or approved constraints |
| FB-DEVICE-MISSING | Story 12.5 evidence audit | blocking | Product/Quality owner | Required tablet and phone fallback evidence is absent | Blocks `ready`; requires manual evidence or approved constraints |
| FB-STAKEHOLDER-SIGNOFF-MISSING | Story 12.5 evidence audit | blocking | Release Owner | Required stakeholder acceptance is not repository-visible or auditable | Blocks all non-blocked release classifications |
| FB-BROAD-MATRICES | Story 11.6 / Story 12.5 evidence audit | post-v1 roadmap | Product/UX accessibility roadmap owner | Broad cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion matrices are outside the current v1 specimen/manual-gate scope | Non-blocking only after required v1 gates and sign-offs are resolved |

### Enum convention: feedback Classification vs gate Status

The Classification column in this register uses the AC13 feedback-classification enum (`blocking`, `accepted constraint`, `post-v1 roadmap`, `non-action decision`). The gate Status enum used elsewhere (`completed`, `not performed`, `blocked`, `accepted v1 constraint`, `post-v1 roadmap`) is separate by design: `blocking` (feedback) is an active-voice classification of an open issue; `blocked` (gate) is a canonical gate status. Stakeholder Acceptance rows reference Open Feedback Register IDs in the Open-feedback ref column to keep both enums traceable without merging them.

## Adopter Communication

| Communication id | Message | Owner | Channel | Target date | Evidence ref |
| --- | --- | --- | --- | --- | --- |
| ADOPTER-COMM-01 | Do not claim broad manual accessibility validation from Story 10.2 automated axe/specimen evidence | Product/Marketing release-notes owner | v1 release notes | Before v1 publish | This evidence pack; Story 10.2 retrospective |
| ADOPTER-COMM-02 | Communicate that phone is a functional fallback for v1, not a daily-use optimized target | Product release-notes owner | v1 release notes | Before v1 publish | UX responsive/accessibility spec; DEVICE-PHONE-FALLBACK row |
| ADOPTER-COMM-03 | Communicate any future accepted v1 constraint only after Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval references are recorded | Product/Release Owner | v1 release notes / adopter portal | Conditional: only when first accepted constraint is approved | Accepted Constraints Register |
| ADOPTER-COMM-04 | Communicate post-v1 roadmap items as explicit roadmap scope, not hidden release blockers | Product/UX roadmap owner | v1 release notes; product roadmap page | Before v1 publish | Post-v1 Roadmap Register |

## Evidence Manifest

| Artifact id | Artifact type | Source | Path or immutable ref | Retention owner | Checksum or immutable ref | Sanitization result | Approved exceptions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EV-PACK-2026-05-15 | Repository markdown (internal) | Story 12.5 implementation | `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md` | Release Owner | Git commit SHA (immutable post-commit) | Passed manual redaction review; repository-relative paths only | None |
| EV-README-2026-05-15 | Repository markdown (internal) | Story 12.5 implementation | `docs/accessibility-verification/README.md` | Release Owner | Git commit SHA (immutable post-commit) | Passed manual redaction review; repository-relative paths only | None |
| EV-TEMPLATE-2026-05-15 | Repository markdown (internal) | Story 12.5 implementation | `docs/accessibility-verification/manual-log-template.md` | Release Owner | Git commit SHA (immutable post-commit) | Passed manual redaction review; repository-relative paths only | None |

No screenshots, recordings, exported logs, raw screen-reader transcripts, external links, cookies, local absolute paths, secrets, tenant/user values, command payloads, full DOM dumps, or unbounded logs are added by this evidence pack.

## Duplicate Status Reconciliation

When the same gate id appears in multiple tables of this pack, the canonical row is determined by the table type:

- For `post-v1 roadmap` gates: the Broader Accessibility Classification table is canonical; the Post-v1 Roadmap Register provides supplemental ownership and approval metadata that must not contradict the canonical status.
- For all other gates: each gate id has exactly one row, in the table appropriate to its type (Manual Screen-Reader Matrix, Real-Device Matrix, Accepted Constraints Register, or Stakeholder Acceptance).

If a future evidence file adds another row for the same gate id, this pack must be updated so exactly one canonical status remains. No duplicate or contradictory gate status currently exists in this pack.

## Final Release Classification

Final classification: `blocked`

Residual gates: `AT-NVDA-FIREFOX`, `AT-JAWS-CHROME`, `AT-VOICEOVER-SAFARI`, `DEVICE-TABLET`, `DEVICE-PHONE-FALLBACK`, `STAKEHOLDER-PRODUCT`, `STAKEHOLDER-QUALITY`, `STAKEHOLDER-RELEASE-OWNER`, `STAKEHOLDER-ACCESSIBILITY`.

This pack can move to `ready` only after all required gates have dated sanitized evidence and sign-off from all four canonical roles (Product, Quality/Test, Release Owner, Accessibility/Stakeholder). It can move to `ready-with-accepted-constraints` only after every incomplete required gate is either an approved accepted v1 constraint (four-role approval) or a named post-v1 roadmap item with the per-row roadmap approval refs (Product, Accessibility/Stakeholder, Release Owner).
