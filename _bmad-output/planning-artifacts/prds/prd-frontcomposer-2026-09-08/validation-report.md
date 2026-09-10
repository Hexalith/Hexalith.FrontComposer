# Validation Report — Hexalith.FrontComposer Product Requirements Document

- **PRD:** `/home/administrator/projects/hexalith/frontcomposer/_bmad-output/planning-artifacts/prd.md`
- **Rubric:** `/home/administrator/projects/hexalith/frontcomposer/.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-09T08:59:12+02:00
- **Grade:** Poor

## Overall verdict

This is a substantively strong brownfield PRD: it has a specific product thesis, unusually concrete functional consequences, explicit delivered-versus-gated status, and a serious decision/open-item register. It is not yet safe as the milestone-closure instrument it claims to be, however, because three evidence-gate pass conditions permit closure without evidence or approval that other sections say is required; for this high-stakes chain-top use, the rubric verdict is **fair pending gate repair**.

The source/current-fit review materially lowers the consolidated grade to **Poor**. It found that the PRD's release-control model conflicts with the workflow that currently executes releases, the current GOV-1 validation has 30 open implementation nonconformances despite the PRD saying no code work is identified, the exact Builds provenance has drifted, and the security residual has contradictory dispositions. Until those contradictions are reconciled, the PRD is not decision-safe for product re-approval or milestone closure.

## Dimension verdicts

- Decision-readiness — thin
- Substance over theater — strong
- Strategic coherence — strong
- Done-ness clarity — adequate
- Scope honesty — strong
- Downstream usability — adequate
- Shape fit — strong

## Findings by severity

### Critical (2)

**[Source/current-fit]** — The claimed sole publication control is not the control the current release caller executes (§0 G-1/G-2, §5.7 FR-24, §12 D-6)

The PRD says `HEXALITH_RELEASE_PUBLISH_ENABLED` is retired and that protected-production approval plus the pinned exact-SHA workflow is the sole publication path. The current caller leaves `governed-release` unset/false and identifies FrontComposer as remaining on the legacy path; the pinned reusable workflow consequently selects its legacy job and still requires that repository variable to be exactly `true`.

Fix: Rewrite G-1/G-2, FR-24, NFR-12, D-6, and the addendum to describe the active legacy caller and deny-by-default variable gate, or first migrate the caller to the governed path and capture evidence that the variable was retired.

**[Source/current-fit]** — GOV-1 is not merely awaiting sign-off and run IDs (§0.1 G-2, §9 SM-2a, §12 D-11)

The PRD says no code work is identified and local proof is done. The current GOV-1 validation instead records failed implementation conformance and 30 open nonconformances, including fallback digest rebinding, authorization soft-defer, post-release authorization mismatch, handoff/ledger omissions, and missing workflow selection.

Fix: Make closure of the current implementation nonconformance register part of G-2, remove the “no code work” and “local proof done” claims, and require a passing revalidation before external proof and sign-off can close the gate.

### High (5)

**[Decision-readiness]** — G-2 allows absence of its end-to-end proof to satisfy the pass-condition branch (§0.1 G-2)

The authoritative row requires the v4.2.0 run IDs to be cited “or the proof is declared not yet produced,” while its state says the gate remains open on proof citation and §9 SM-2a says that citation is open. Declaring required proof absent cannot both satisfy and leave open the evidence gate.

Fix: Require authenticated run IDs and proof, or define a separately owned, dated waiver approval with explicit residual risk; do not make “not produced” an evidence-pass alternative.

**[Decision-readiness]** — G-6's fallback can close the item without proving a substitute adopter (§0.1 G-6; §12.2 OI-5)

The main path requires dated, candidate-bound Tenants evidence, but the fallback only says Product chooses Parties or holds the milestone, and OI-5 can unblock when the fallback is merely decided. Choosing Parties therefore carries no explicit equivalent bootstrap-evidence obligation.

Fix: Require the same dated, candidate-bound projection-and-command proof from Parties; only a decision to hold should close the decision without closing G-6.

**[Decision-readiness]** — G-7 omits the security acceptance the PRD says is mandatory (§0.1 G-7; §12 D-14; §12.2 OI-2; Addendum §6)

G-7's pass condition is limited to an audit test class, but D-14/OI-2 also require dated sign-off by a security reviewer distinct from the author for the disclosed catalog and credential/resource oracles. The addendum labels the gated-list alternative rejected while OI-2 still describes it as live.

Fix: Add the dated OI-2 sign-off—or the completed gated-list alternative—to G-7's pass condition and evidence, and keep the alternative pending in the addendum until that decision is signed.

**[Source/current-fit]** — Exact Builds provenance is stale, so G-3 is not approval-only (§0.1 G-2/G-3, §7, §12 D-11–D-13)

The PRD and addendum identify `references/Hexalith.Builds` at `35c3d1e5…`; current root `HEAD` records `a32cb422…`. The runtime-identity contract and captured Pact evidence still bind the old Builds identity. EventStore remains `059f6a89…` and the catalog still selects `3.103.0`, but the asserted exact catalog provenance changed.

Fix: Recapture or extend evidence at `a32cb422…` and update the identity contract/PRD, or define an explicit semantic-compatibility rule permitting this catalog-only advance; until then G-3 requires evidence work as well as approval.

**[Source/current-fit]** — The security residual is both accepted and explicitly not accepted (§5.5 FR-19, §12 D-14, §12.2 OI-2)

FR-19 and the addendum call descriptor/credential disclosure accepted, while D-14 and OI-2 say those oracles are not accepted until a dated security sign-off exists.

Fix: Use one disposition everywhere—“observed residual pending OI-2; not accepted”—or record the dated approval and close OI-2 with its evidence.

### Medium (5)

**[Decision-readiness]** — D-13 is simultaneously accepted, proposed, and non-gating (§12 D-13; §12.1; §12.2 OI-4)

D-13 accepts RC pins for the milestone, §12.1 calls the answer proposed rather than decided, and OI-4 is not mapped to a gate.

Fix: Record the Product decision before calling D-13 accepted, or explicitly assign the accepted time-bounded risk to Architecture and remove Product confirmation as an open decision; if Product approval is required, add it to §0.1.

**[Done-ness clarity]** — FR-3 does not define done for most of the vocabulary it claims (§5.1 FR-3)

The requirement names roughly seventeen attribute/metadata concepts, but its consequences specify behavior only for invalid usage, editable-field suppression, and accessible badge/status metadata.

Fix: Add a compact vocabulary-to-observable-consequence table, or a stable contract/test reference for each concept, while retaining FR-3 as the umbrella.

**[Done-ness clarity]** — FR-23's synchronization claim has no content-level completion rule (§5.6 FR-23)

DocFX success and frontmatter/snippet checks can pass while documentation is semantically stale; the requirement itself records a known stale contingency document.

Fix: Name authoritative inventories and drift checks for component, diagnostic, migration-edge, and skill-corpus coverage; require zero known stale public-doc items or classify each exception outside FR-23's done baseline.

**[Downstream usability]** — Closed decision records remain imperative FRs that story generators can treat as work (§5.0 Table C; §5.7 FR-27 and FR-28)

Table C calls FR-27/FR-28 closed decision records, but their canonical headings and consequence lists still read as active requirements.

Fix: Preserve the frozen IDs but label each heading/body `retired/closed — no implementation work`; move enduring invariants into active FRs and history into §12 or the addendum.

**[Source/current-fit]** — The public diagnostic inventory omits stable public IDs and misstates the owned bands (§5.1 FR-3; §11)

The PRD lists only selected build-time, migration, runtime, and tooling bands. The authoritative registry also allocates MCP and Aspire bands and contains public `HFC4001` plus approved cross-package exception `HFC1601`.

Fix: Reference the registry as executable authority and cover all owned bands and approved exceptions rather than presenting the partial list as the complete public surface.

### Low (2)

**[Scope honesty]** — A4 does not round-trip through the promised inline assumption mechanism (§0; §13 A4)

A4 exists only in the index while its cited reliance points state the publication interpretation as fact.

Fix: Add `[ASSUMPTION A4: …]` at the first relied-on claim, or recast A4 as evidence uncertainty owned by G-1/OI-8 and remove it from the Assumptions Index.

**[Downstream usability]** — Evidence references are not consistently self-resolving when sections are extracted (§9 SM-2, SM-7, SM-8, SM-10)

Several metrics use basename-only or partial evidence paths while gate rows use canonical paths.

Fix: Use repository-root-relative paths consistently for every evidence artifact in §9.

## Mechanical notes

- FR headings are unique and cover FR-1 through FR-30; FR-19a and FR-29.1–FR-29.7 are intentional nested/frozen extensions.
- UJ-1–UJ-6, G-1–G-7, D-1–D-15, and OI-1–OI-11 are present and unique in their defining sections.
- SM identifiers are unique but intentionally ordered by priority rather than number; §9 explains the frozen SM-5/SM-6 placement and retired SM-C1/SM-C3 identifiers.
- A3 round-trips between its inline tag and §13. A4 is index-only; A1/A2 are explicitly retired historical assumptions.
- Every user journey has a named protagonist carrying role context inline. No material glossary synonym drift was found.

## Reviewer files

- `review-rubric-2026-09-09.md`
- `review-source-consistency-2026-09-09.md`
