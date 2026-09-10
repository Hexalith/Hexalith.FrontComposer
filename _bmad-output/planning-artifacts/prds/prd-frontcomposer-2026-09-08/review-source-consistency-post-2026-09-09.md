# Source / Current-Fit Review — PRD Post-Reconciliation (2026-09-09)

## Scope

- **Reviewed:** `_bmad-output/planning-artifacts/prd.md` and `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`.
- **Reconciliation inputs:** `reconcile-prd-validation-2026-09-09.md`, `reconcile-architecture-2026-09-09.md`, and `reconcile-ux-2026-09-09.md`.
- **Canonical/current sources:** `architecture.md`, the latest draft `ARCHITECTURE-SPINE.md` with AD-19, `ux-design.md`, the 2026-09-09 PRD/UX/GOV-1 validation reports, and the architecture update reconciliation reports current at review time.
- **Final source snapshot for this review:** PRD SHA-256 `932984fe9de4f0b35a4644611595d1fe146677d129f3cbfa1522041d16c84aff`; addendum SHA-256 `5f4bec93edf1f2b3cd01d0be63de72d4083f6829ccdc59e1822471739ad197c2`; GOV-1 spine SHA-256 `c72c43c728df6aba5760c37a8bb3f2bc703dfecfaaaeb8c22b9009609af4f53c` (AD-19 adopted in a still-draft spine).

## Verdict

**FAIL for Product re-approval until four High source-consistency findings are corrected.** The update correctly preserves the major unresolved gates: it no longer calls the active legacy caller governed, keeps GOV-1 implementation conformance and authenticated proof open, preserves old-versus-current Builds provenance, requires equivalent Parties adoption evidence, gates the MCP audit plus independent security disposition, and carries all four High UX behavior deltas without claiming their evidence exists. No unresolved evidence was laundered into a passed gate.

The remaining defects are concentrated in the newly amended release architecture. The PRD still conflates NuGet package payload equality with raw archive equality, omits the candidate-free publisher's exclusive final authorization and mandatory attestation/fallback outcome, leaves a direct addendum claim that the already-published 4.x line was governed, and describes the pre-update architecture state after the owner-ratified AD-19 amendment. These are load-bearing enough that downstream release stories could implement or classify the wrong trust boundary.

Finding counts: **Critical 0 · High 4 · Medium 4 · Low 0**.

## Confirmed Correct Applications

- Validation VR-1 through VR-7 are materially represented in §0/§0.1, FR-19, FR-24, D-6/D-11 through D-14, and G-1 through G-8: the active legacy caller and `HEXALITH_RELEASE_PUBLISH_ENABLED` gate are current truth; proof cannot be waived by absence; G-6 requires equivalent adopter evidence; G-7 requires both the audit and the independent OI-2 disposition; the Builds gitlink/evidence-provenance split is preserved; and MCP disclosure remains observed but not accepted.
- UX reconciliation Highs are present in FR-8, FR-10 through FR-16, FR-22, NFR-3/NFR-11, SM-6/SM-10, and addendum §4: announcement deduplication/coalescing, accessible validation/rejection, route/tab/palette/dialog focus, and state-by-surface behavior are explicit. SM-6 remains unmet and OI-16 correctly prevents Product re-approval from treating requirements text as implementation evidence.
- FR-27 and FR-28 are explicitly retired without renumbering; FR-3 now has the requested vocabulary-to-observable-behavior mapping; the diagnostic registry/bands include MCP, Aspire, and `HFC1601`; and assumption/evidence-path cleanup is substantially complete.
- The live §3 glossary now correctly defines Governed Release as the target split privilege boundary and distinguishes REL-* delivery history from the not-yet-implemented conforming model. The §0.1 state headings have also been advanced to 2026-09-09.

## Critical

None.

## High

### H1 — FR-24 requires impossible raw-byte equality for repository-signed NuGet packages

PRD §5.7 FR-24 says “the validated bytes must be identical to the published bytes” and that manifest v3 binds “the exact published bytes” (`prd.md:462,467,469`). Canonical `architecture.md` §FR-24 instead says GitHub assets are raw-byte equal, while a NuGet.org repository-signed `.nupkg` is expected to have a different raw archive hash because NuGet adds root `.signature.p7s`; equivalence is a valid repository signature plus byte-equivalent normalized ZIP members for every other entry (`architecture.md:271-279`). Latest AD-19 repeats that split (`ARCHITECTURE-SPINE.md:724-730`).

**Required correction:** amend FR-24, NFR-12, SM-2, and addendum §1 so GitHub Release assets must match candidate files byte-for-byte, while NuGet packages must have a valid repository signature and normalized ZIP-member equivalence excluding only the root `.signature.p7s` addition. State explicitly that the expected raw NuGet archive-hash difference is not itself an incident; any other added/removed/changed member is.

### H2 — The candidate-free publisher's exclusive final authorization and mandatory attestation/fallback are missing

The latest GOV-1 decision assigns the complete final authorization to pinned owner-controlled code in the protected, candidate-free publisher: after authenticating the builder artifact it obtains and verifies the required provenance attestation (or validates the immutable approved-unsupported fallback), prepares/seals manifest v3, performs offline/live verification, classifies, and requires `publish_authorized=true` before any NuGet/GitHub publication (`ARCHITECTURE-SPINE.md` AD-9 at lines 245-253 and AD-19 at lines 710-723). The builder's verdict is early denial only. The PRD and addendum currently say the builder produces an authenticated artifact and the publisher treats it as data, but they do not assign final classification exclusively to the publisher or retain attestation/fallback as mandatory evidence (`prd.md:465-469,553`; addendum §1 lines 17-25).

This leaves a dangerous conforming reading in which candidate-controlled builder output declares itself publishable and the protected publisher merely trusts it. It also silently drops non-signing provenance attestations while correctly dropping author signing/RFC 3161.

**Required correction:** in FR-24 and NFR-12 require the candidate-free publisher, using its exact authorized closure, to recompute final manifest/seal verification and classification and require `publish_authorized=true` before publication. In FR-24/NFR-12 and addendum §1 require a verified provenance attestation or the immutable approved-unsupported fallback, bind that result into manifest/handoff evidence, and require post-release verification for `compliant-candidate`. Keep author signing and RFC 3161 explicitly retired.

### H3 — The addendum still claims the historical 4.x publication line was governed

The live PRD now correctly defines Governed Release around the target D-16/G-8 secretless-builder/protected-publisher split and says the legacy caller does not qualify (`prd.md:17,121-123,465-466`). But the normative addendum's finding map still says §0 states that “4.x publication is governed per release” (`prd-addendum-2026-09-08.md:176`). That is a direct contradiction of §0 and of AD-15/AD-19, which reserve a governed attempt for the authenticated `split-publication-v1` topology. It launders historical controls into conformance even though G-8 correctly remains open.

**Required correction:** replace addendum §8's unqualified 4.x “governed” sentence with the current truth: the published 4.x history used legacy controls and must not be called FR-24-compliant under AD-19; this PRD neither reclassifies it nor authorizes a publication. Preserve the now-correct §3 glossary wording.

### H4 — The PRD still reports the pre-update GOV-1 architecture state after AD-19 was adopted

G-8's state still quotes the earlier “one Critical/four High convergence gaps,” D-16 is wholly “pending,” OI-13 still asks Architecture to resolve A1-A5, and addendum §2 says Architecture “must resolve” those findings (`prd.md:35,687,716-717`; addendum §2 line 44). That wording accurately describes the 09:31 validation snapshot but not the current phase. The latest spine records the owner-ratified split in adopted AD-19; its validation-update reconciliation closes A1-A5 in architecture, and the subsequent delivery reconciliation corrections are now present. The spine remains `status: draft`, companion sources and the register remain unreconciled, no fresh complete five-lens gate has passed, and implementation remains FAIL, so G-8 must stay open.

**Required correction:** record the phase split accurately: Architecture has adopted AD-19 and applied A1-A5 plus the delivery-reconciliation corrections; final spine approval/revalidation, Product/Release acceptance, companion-source updates, the implementation/nonconformance bundles, authenticated run proof, and the incident runbook remain open. Narrow D-16/OI-12 to the owner acceptance and interim-operating decisions still genuinely pending, and replace OI-13's “resolve A1-A5” work with final source reconciliation and revalidation. In G-8/addendum §2 retain the original failed validation as historical evidence, then state the amended architecture result separately. Do not close G-2 or G-8.

## Medium

### M1 — Deferred CI handoff incident semantics are absent from the PRD projection

Latest AD-15 makes `deferred-no-ci-handoff` a terminal incident-bearing disposition, with incident monotonicity. PRD G-1/SM-2 and addendum §1 mention deferral only as one generic ledger outcome. A downstream ledger story could therefore treat it as a neutral no-op.

**Required correction:** state in SM-2 or FR-24, and in addendum §1, that a valid deferred sentinel is terminal and incident-bearing; it cannot become a compliant observation or be relabelled green. Preserve the distinction from a malformed/missing handoff, which is `missing-artifact`.

### M2 — OI-14/addendum §2 do not project the latest split-delivery closure bundles

The latest delivery reconciliation requires distinct closure for the accepted split reusable/delayed activation, caller switch and exact topology, removal of `production` from candidate jobs, publication-candidate producer/authentication/hostile fixture, handoff-v3/ledger migration, candidate-free final classification/publication, pinned post-release helper, and duplicate destination asset-name rejection. OI-14 is an aggregate and addendum §2 still summarizes the older validation counts/NC labels.

**Required correction:** keep OI-14 as one product-level item if desired, but list or link these eight exact closure bundles in addendum §2 and make its unblock condition require every bundle plus the reconciled register. Do not claim current implementation conformance.

### M3 — The incident-runbook open item omits the safe re-enable rule

OI-15 names containment actions but not the condition for re-enabling the publish gate (`prd.md:719`). Latest AD-15 and the spine's Deferred section name `docs/release-incident-response.md` and require acknowledgement/containment, documented cause/remediation, credential rotation where exposure is plausible, complete external-side-effect inventory, independent verification, and Release Owner approval before any retry/later dispatch (`ARCHITECTURE-SPINE.md:571-576,882-887`).

**Required correction:** name `docs/release-incident-response.md` in OI-15/addendum §1 and add the full safe re-enable conditions above. Do not mark the runbook complete until it exists and is approved/exercised as OI-15 requires.

### M4 — FR-23 calls four inventories authoritative without naming three of them

FR-23 says component, diagnostic, migration-edge, and skill-corpus inventories are authoritative and content-checked, but only the diagnostic inventory is resolvable elsewhere by exact path (`prd.md:446-454`; §11). This only partially applies VR-9.

**Required correction:** name the repository-root-relative authoritative artifact or executable check for each inventory, or add owner-bound open items for inventories that do not yet exist. Keep `docs/diagnostics/diagnostic-registry.json` as the diagnostic authority.

## Low

None.

## Gate Recommendation

Do not allow G-4 Product re-approval yet. After H1-H4 are fixed, rerun this source/current-fit lens against a stable hash of the final AD-19 spine and the updated PRD/addendum. Keep G-1, G-2, G-3, G-6, G-7, and G-8 open unless their named external evidence actually exists; this review found no basis to close any of them.
