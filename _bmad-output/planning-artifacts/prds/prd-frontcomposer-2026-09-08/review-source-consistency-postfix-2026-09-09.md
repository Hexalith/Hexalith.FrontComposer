# Source / Current-Fit Postfix Review — PRD (2026-09-09)

## Scope and source snapshot

- **Reviewed:** `_bmad-output/planning-artifacts/prd.md` and `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` after the source-reconciliation fixes.
- **Compared with:** the prior `review-source-consistency-post-2026-09-09.md`; the three 2026-09-09 reconciliation extracts; canonical `architecture.md`; GOV-1 `architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`; canonical `ux-design.md`; and the latest PRD, UX, and GOV-1 validation/reconciliation reports.
- **Fixed snapshot:** PRD SHA-256 `f9013d795332f7d259d01b03a32d68129bb1ff78644f81968f887f3ff93596b8`; addendum SHA-256 `c46c0deb0f2726bece8a3297d069f8881f4b9309e8c832345a3585616d289811`; canonical architecture SHA-256 `e04567dc3cf10d338cff695a8b43b0e84c62f9f0b88b8c74d278b668ef805d7f`; canonical UX SHA-256 `23acac1fc1fe14e903e70bcf0118b81540cfb7b3193971b25471e847d089a38c`; GOV-1 spine SHA-256 `95cbe2f2754ad3c1d7c896e73822b49edb676c0f5921c4f05fded82519c6e6e8`.

## Verdict

**PASS at the Critical/High source-consistency gate, with one Medium follow-up.** All four prior High findings and three of the four prior Medium findings are resolved. The remaining Medium is a precision/evidence-mapping defect in FR-23; it does not change the product boundary or justify closing an implementation/evidence gate.

The amended PRD does not launder the adopted AD-19 architecture into delivery. It distinguishes Architecture adoption from Product/Release acceptance, canonical-architecture propagation, implementation convergence, authenticated proof, and revalidation (§0.1 G-8; D-16; OI-12–OI-15; addendum §§1–2). The current legacy caller remains explicitly nonconforming, production releases remain halted, `HEXALITH_RELEASE_PUBLISH_ENABLED=false` remains deny-only, and no current bounded risk exception is invented.

Finding counts: **Critical 0 · High 0 · Medium 1 · Low 0**.

## Prior-finding resolution check

| Prior finding | Result | Current evidence |
| --- | --- | --- |
| H1 — raw NuGet byte equality | **Resolved.** | FR-24 distinguishes byte-identical GitHub assets from repository-signed NuGet normalized-ZIP equivalence, excluding only valid root `.signature.p7s` (§5.7 FR-24, lines 465–472). NFR-12, SM-2, and addendum §1 preserve the same rule. |
| H2 — publisher-exclusive final authorization and mandatory attestation/fallback | **Resolved.** | G-8, FR-24, NFR-12, and addendum §1 assign authentication, attestation/fallback, sealing, final classification, `publish_authorized=true`, and side effects exclusively to the protected candidate-free publisher. Builder output is early denial only. |
| H3 — historical 4.x called governed | **Resolved.** | Addendum §8 states that current execution is legacy and no release is FR-24-compliant before G-8 (line 178), agreeing with PRD §0 and FR-24. |
| H4 — stale pre-AD-19 phase status | **Resolved.** | G-8, D-16, OI-12/OI-13, and addendum §§1–2 state that AD-19 is adopted while Product/Release acceptance, parent propagation, implementation/proof, and revalidation remain open. |
| M1 — deferred-handoff incident semantics | **Resolved.** | FR-24 and SM-2 make `deferred-no-ci-handoff` the sole valid deferred sentinel and a terminal, incident-bearing, permanent disposition; malformed, duplicate, missing, or unauthenticated handoff is `missing-artifact`. Addendum §1 matches. |
| M2 — eight implementation bundles absent | **Resolved.** | OI-14 and addendum §2 enumerate all eight accepted closure bundles and require authenticated evidence without claiming delivery. |
| M3 — runbook path and safe re-enable criteria absent | **Resolved.** | OI-15 and addendum §1 name `docs/release-incident-response.md`, define containment/retry requirements, and require cause/remediation, rotation, external-effect inventory, independent verification, and Release Owner approval before re-enablement. |
| M4 — FR-23 authorities unnamed | **Partially resolved.** | FR-23 now names all four source/test mappings, but the component and migration mappings overstate current cross-source enforcement; see M1 below. |

## Additional live-spine reconciliation check

- **Manifest lineage is current.** FR-24, NFR-12, §11, D-6, and addendum §1 target `hexalith.release-evidence.v4` and classify v1–v3 as audit-only, agreeing with adopted spine AD-14/AD-17.
- **Envelope versions are not conflated.** PRD §11 separately names `hexalith.publication-candidate.v1`, `hexalith.release-verification-handoff.v3`, `frontcomposer.release-ledger-record.v2`, and `hexalith.dependency-graph.v1` (line 668).
- **Interim operating posture is explicit.** PRD §0, D-16, OI-12, and addendum §1 record the production halt, the deny-only emergency stop, and absence of a current exception. They do not imply that protected approval can make the legacy path conforming.
- **Canonical architecture drift remains visible, not laundered.** Canonical `architecture.md` still describes evidence v3 and the old mechanism; OI-13 explicitly keeps propagation of adopted AD-19 and the current contracts open. G-8 therefore remains open, as required.

## Critical

None.

## High

None.

## Medium

### M1 — FR-23 names the right inventory areas but overstates two source/test authority relationships

FR-23 says component documentation is “jointly authoritative” in the FC-DOC contract and component index and is checked by `FcDocComponentDocumentationContractTests` (`prd.md` §5.6 FR-23, line 451). The index currently lists six pages and describes a read-only-MVP set containing page toolbar, while the FC-DOC status map and its test hard-code only Layout, Navigation, DataGrid, and Settings (`fc-doc-component-documentation-2026-06-03.md` “Component status map”; `FcDocComponentDocumentationContractTests.ComponentStatusMapCoversTheReadOnlyMvpSetAsAPageOrTrackedGapWithOwner`). The test proves that every authored page appears in the index and that those four status-map rows are authored or owner-bound gaps; it does not prove complete parity among the status map, index inventory, and current public component surface.

FR-23 also says migration edges are jointly authoritative in `MigrationCatalog.cs` and `docs/migrations/index.md` and checked by `MigrationCommandTests.cs` (`prd.md` line 452). The executable catalog currently contains only `9.1.0 -> 9.2.0`, while the index also lists `3.1 -> 4.0` and `1.12 -> 2.0` manual/package/API guides. `MigrationCommandTests` exercises the catalog edge and its guide link but does not parse the index or assert an explicit CLI-supported-versus-manual-only classification for every indexed guide. `eng/validate-docs.ps1` validates guide structure and known producer fingerprints, not catalog/index parity.

**Required correction:** in PRD FR-23 line 451, say that the FC-DOC contract governs component-page conformance and its status obligations, while `docs/reference/components/index.md` inventories the published pages; describe `FcDocComponentDocumentationContractTests` as checking page conformance, page-to-index inclusion, and the four currently enumerated status-map areas. Either reconcile page-toolbar/page-tabs and the public component inventory into that map and extend the test to assert complete parity, or add an owner-bound open item for that missing check. In PRD line 452, make `MigrationCatalog.cs` authoritative for CLI-supported executable edges and `docs/migrations/index.md` the published guide inventory, which may also contain explicitly classified manual-only/package/API edges; describe `MigrationCommandTests` as checking executable catalog behavior and its guide link. Retain “jointly authoritative” only after adding and naming a test that reconciles every catalog edge with every indexed guide and its classification. No addendum text currently repeats these two authority claims, so no addendum change is required unless the mapping is duplicated there; if it is, mirror the corrected split rather than restating the present overclaim.

## Low

None.

## Gate recommendation

This source/current-fit lens no longer blocks the PRD's Critical/High reviewer gate. Keep G-2 and G-8 open for the accepted split contract, implementation bundles, authenticated evidence, and revalidation; keep OI-13 through OI-15 open until their named artifacts and approvals exist. Keep G-4 subject to the complete reviewer gate and the still-open OI-4/OI-10/OI-16 evidence. The Medium FR-23 wording/test gap should be corrected before downstream work treats the named inventories as mechanically reconciled.
