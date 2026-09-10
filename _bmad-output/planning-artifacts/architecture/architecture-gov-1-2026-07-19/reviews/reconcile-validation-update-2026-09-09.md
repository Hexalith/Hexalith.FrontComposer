# Validation-to-Update Reconciliation — GOV-1 Architecture Spine

## Scope and basis

- **Reviewed artifact:** `ARCHITECTURE-SPINE.md`, working-tree SHA-256
  `c72c43c728df6aba5760c37a8bb3f2bc703dfecfaaaeb8c22b9009609af4f53c`, `status: draft`,
  `updated: 2026-09-09`.
- **Decision authority:** the complete `.memlog.md`, including the Administrator's ratification of the
  split publication boundary and the 2026-09-09 update decisions.
- **Finding source:** `validation-report-2026-09-09.md` and its rubric, adversarial, current-fit,
  implementation-drift, and security reviewer reports.
- **Method:** reconcile each consolidated architecture finding and the relevant Medium/Low tail against
  the revised Rule text. Implementation was inspected only to separate current drift from architecture
  completeness; this review did not treat unimplemented target-state rules as architecture omissions.

## Verdict

**PASS — no unresolved Critical or High architecture finding.** The update lands the
Critical privilege split, governed-attempt classifier, canonical-byte contract, post-release evaluator
identity, authenticated quality-run carrier, conclusion projection, historical verifier-pin scope, and
all low-severity architecture/seed corrections. AD-1 through AD-18 retain their identities, AD-9's
fallback-drift guarantee is not weakened, AD-18 is strengthened, and the new split is isolated in AD-19.

AD-15's final fail-closed precedence closes the provisional disposition ambiguity: partial publication
dominates, the valid deferred sentinel is incident-bearing, required artifact/topology failures follow,
then other contract/authorization/verification failures, and only one non-incident result may match.
Zero or multiple matches become `non-compliant`. Its API conclusion vocabulary, immutable observation
identity, byte-identical duplicate rule, and incident-monotonic durable projection make A4/RW-03 fully
convergent.

The final AD-9/AD-15/AD-19 recheck also converges the attestation and failure paths. Builder output is
early-denial evidence only; final attestation/fallback validation, manifest sealing, offline/live
verification, and authorization execute in candidate-free protected owner code. The owner-controlled
`publication_started` marker is set immediately before the first NuGet/GitHub Release mutation, so a
protected pre-publication failure remains `rejected-before-publication` while any actual or mismatched
external side effect becomes `partial-publish-incident`. The flow rendering now matches that ownership.

## Critical and High architecture findings

| Validation item | Result | Reconciliation |
| --- | --- | --- |
| **A1 / SEC-VAL-01 — candidate code shared a publication-credentialed job** | **Closed** | AD-13 admits candidate execution only in a secretless builder and allows only a run-bound artifact to cross into publication. AD-18 forbids candidate code with any secret/write scope, scopes publication secrets to the protected publisher, and forbids `secrets: inherit`. AD-19 makes builder readiness diagnostic only; candidate-free pinned owner code authenticates the artifact, obtains/verifies attestation or fallback, seals/verifies/classifies the final manifest, and alone may publish. Hostile-candidate fixtures are mandatory. AD-17 binds both roles to one exact reusable coordinate without merging their privileges. |
| **A2 / ADV-2026-09-09-01 — no governed-attempt classifier** | **Closed** | AD-15 defines a governed attempt from the authenticated AD-13 caller plus the active release evaluator and exact AD-19 reusable, independent of a legacy inner job. It fixes the two API job names and rejects zero, duplicate, additional publication-capable jobs, or topology/handoff disagreement. An owner-controlled marker records the instant immediately before the first publication mutation; job start and prerequisite attestation minting do not falsely count as publication, and the marker is reconciled with job state and external APIs. |
| **A3 / ADV-2026-09-09-02 — canonical JSON escape ambiguity** | **Closed** | AD-5 binds all spine digests to the byte-for-byte Python 3.14 `json.dumps` invocation, forbids floats, selects short escapes, lowercase Unicode escapes and surrogate pairs, and requires cross-language hostile/golden vectors for all requested character classes. |
| **A4 / ADV-2026-09-09-03 / RW-03 — ledger state machine and immutable identity** | **Closed** | AD-15 closes the member set and types, defines eight disposition values, fixes fail-closed precedence, makes the valid deferred sentinel incident-bearing, maps zero/multiple matches to `non-compliant`, closes API conclusion values, keys observations by immutable Release and verification run coordinates, requires byte-identical duplicates, and makes incidents permanently dominant in REL-AI-1. |
| **A5 / SEC-VAL-02 — ambient later-default-branch post-release helper** | **Closed in architecture** | AD-12 requires a matched `post_release` authorization and restricts executable logic to the exact caller blob and pinned actions in its closure; it expressly forbids ambient/default-branch or released-candidate checkout/helper execution. AD-13 permits a later active-policy-authorized evaluator for recovery, records it independently, and forbids projecting it as Release-time provenance. AD-15 authenticates that closure before any ledger decision. The existing workflow remains implementation drift. |

## Medium and Low architecture tail

| Validation item | Result | Reconciliation |
| --- | --- | --- |
| **RW-01 / ADV-2026-09-09-04 — no authenticated `quality_run` carrier** | **Closed** | AD-15 handoff v3 carries the exact AD-13 projection, requires post-release API authentication, and forbids re-selection. |
| **RW-02 — completed-run uniqueness ambiguity** | **Closed in spine; companions deferred** | AD-13 filters to completed successful runs first, then requires exactly one; failed/cancelled attempts do not create ambiguity and duplicate successful reruns fail closed. Parent/FC-DEP-1 wording is explicitly deferred for owner reconciliation before implementation. |
| **ADV-2026-09-09-05 — no total `needs` conclusion projection** | **Closed** | AD-15 enumerates every direct dependency, admits only `success`, `failure`, `cancelled`, or `skipped`, rejects missing/unknown entries, and gives deterministic failure/cancellation/success precedence. |
| **ADV-2026-09-09-06 — historical verifier execution-SHA scope** | **Closed** | AD-13 says a historical/recovery verifier may use its later active-policy-authorized `post_release` action commits, records them independently, and never equates them with or projects them as the historical Release pin. |
| **SEC-VAL-04 — no containment owner/action/runbook trigger** | **Closed** | AD-15 binds Release Owner ownership and `docs/release-incident-response.md`, acknowledgement plus containment before retry/dispatch, gate freeze, evidence preservation, credential rotation where needed, complete external inventory, unlisting, new-version correction, independent verification, and explicit Release Owner approval before gate re-enable. Deferred requires that runbook to land before split integration; only automatic issue creation remains deferred. |
| **RW-04 — open `environment_protection` shape/time** | **Closed** | AD-18 fixes the exact nested members, field types, ordering, `main` branch constraint, and post-Release observation time. |
| **RW-05 — unnamed embedded CI copy / duplicate matches** | **Closed** | AD-15 names `dependency-release-handoff.json` and maps missing, duplicate, or unequal normative copies to `missing-artifact`. |
| **RW-06 — ambiguous Builds execution identity** | **Closed** | Consistency Conventions explicitly select the active-policy `release` row and keep the CI pin independent. |
| **RW-07 — missing inherited frontmatter binding** | **Closed** | Frontmatter now includes `parent:no-runtime-or-ux-change`. |
| **RW-08 — stale GOV-1/G2 source wording** | **Explicitly deferred** | Inherited Invariants identify the same-job source as superseded, and Deferred requires reconciliation of `architecture.md`, FC-DEP-1, the story, and G2 request before split implementation. This is companion drift, not a weakened AD. |
| **SCD-01 — inaccurate module-dependency diagram** | **Closed** | The diagram now reflects the observed `release_prepublish` and `release_evidence` relationships and removes the false imports identified by the drift review. |
| **SCD-02 — inaccurate fixture-directory seed** | **Closed** | The seed now states that the directory currently contains manifest fixtures and that policy/split fixtures land with implementation. |
| **CF-2026-09-09-08 — .NET 10.0.401 upstream movement** | **Closed** | Stack text labels 10.0.400 as the local/pinned feature-band floor and records upstream 10.0.401. |

## Invariant and AD stability check

- AD-1 through AD-11 remain substantively unchanged in their graph, compatibility, fallback,
  acquisition, and governance-only guarantees. AD-5 is narrowed to a uniquely reproducible byte
  contract rather than weakened.
- AD-12 retains delayed activation and active-row authorization, while strengthening the post-release
  trust boundary so ledger decisions cannot run ambient code.
- AD-13 retains exact-main, exact-CI-candidate authority and immutable reusable pins, while resolving the
  CI/quality selection ambiguity and making the candidate-to-publisher crossing artifact-only.
- AD-14 remains one-way and fail-closed. AD-17 remains manifest v3; its release provenance still names
  one exact reusable blob and now explicitly binds both split roles.
- AD-15 advances the cross-workflow handoff from v2 to v3. That is a versioned target-state change, not
  an in-place weakening; historical evidence remains governed by AD-14.
- AD-16 retains the accepted-lineage root and two-phase authorization model. A new owner-accepted Builds
  revision implementing AD-19 must enter that lineage before FrontComposer selects it.
- AD-18 is strengthened from scope placement to an enforceable privilege boundary. AD-19 is the next
  stable ID and records the newly ratified decision; no previous ID is renumbered or reused.
- AD-19 also preserves the full inherited FR-24 evidence obligation in the secretless builder and keeps
  exact GitHub asset bytes, tag-to-candidate identity, and NuGet repository-signature normalization
  explicit. The privilege move therefore does not weaken package/test/SBOM/symbol evidence or exact-byte
  verification.
- The inherited exact-artifact, pre-publication-only authorization, ownership, and no-runtime/API/package/
  UX invariants remain intact. The parent same-job BUILD-REL-1 mechanism is explicitly superseded by an
  owner-ratified successor and cannot silently govern implementation.

## Implementation drift and companion work

No implementation or register change was part of this spine-only update. The validation report's
implementation verdict therefore remains **FAIL**, and the updated spine must not be described as a
production-state description.

| Cluster | Current disposition after the architecture update |
| --- | --- |
| **I1 / CF-01 / ICD-01 — live fallback-digest rebinding** | Still implementation nonconformance under AD-9; NC-1/2/3/24 remain applicable. The architecture guarantee was preserved. |
| **I2 / CF-02 / SEC-VAL-03 — legacy reusable mode** | Still implementation nonconformance, but the corrective target is now the owner-accepted `split-publication-v1` reusable, not merely flipping the old `governed-release` flag. The register needs the reserved legacy-mode row and the split-reusable delivery dependency. |
| **I3 / CF-03 / ICD-04 — quality selection, trust-bearing CI checks, row closure, lineage** | Still implementation/test drift under AD-12/13/16/18 and existing NC-18/20-23/28/30. |
| **I4 / CF-04 / ICD-02 — evaluator authorization and independent CI authentication** | Still implementation/policy drift under AD-12/15. A new row must specifically target elimination of ambient post-release helper execution under the amended rule. |
| **I5 / CF-05 / ICD-03 — old handoff/ledger producer** | Still implementation drift. Existing v1/v2-oriented rows must be reconciled to handoff v3, publication-candidate v1, the split job topology, the total classifier, and one typed ledger producer. |
| **ICD-05/06 and CF-06/07** | Manifest-v2 over-acceptance, diagnostic source authority, missing canonical/list/permission fixtures, and missing lineage verification remain implementation/test work. |
| **ICD-07 / NC-31** | Duplicate asset-name acceptance remains and must be added to the register. AD-15 itself already requires name uniqueness and a hostile fixture. |

The existing register is consequently both incomplete and partially stale. The Deferred section now
enumerates eight discrete closure bundles covering the split reusable and delayed activation, caller and
job topology, candidate-job privilege removal, publication-candidate production/authentication, handoff
v3/ledger migration, candidate-free final authorization/publication, pinned post-release execution, and
duplicate destination-name rejection. It blocks
split-publication implementation until the source owners update the parent architecture, FC-DEP-1,
GOV-1 story, G2 request, and register.

## Final recheck conclusion

All five consolidated validation architecture findings A1–A5 remain closed after the attestation,
classifier, and runbook amendments. No new Critical/High contradiction was found between AD-9, AD-12,
AD-13, AD-15, AD-17, AD-18, and AD-19. Automatic incident-issue creation and the eight implementation /
source-owner bundles remain explicit deferred work; they do not weaken the architecture contract, and
release stays ineligible until the required integration rows close.
