---
title: PRD Reconciliation Extract — GOV-1 Architecture Validation
source_date: 2026-09-09
target_prd: _bmad-output/planning-artifacts/prd.md
target_addendum: _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
intent: update-extract
status: complete
---

# Architecture Reconciliation Extract

## Verdict

**Material PRD update required; publication-safety decision is phase-blocking.** The bounded dependency-graph/catalog core remains valid, but the 2026-09-09 GOV-1 reviewer gate failed the release half of the architecture. The PRD therefore cannot continue to say that the current 4.x path is governed, that GOV-1 local work is done, that G-2 has no code work, or that only G-6/G-7 still require engineering. The current controls still require Release Owner approval, but approval is authorization, not a sandbox: candidate-controlled code executes in a job holding write scopes and publication credentials. The architecture must be amended and the implementation reconformed before a release may count as FR-24-compliant or v1.0-ready.

Sources reconciled: canonical `architecture.md`; GOV-1 `ARCHITECTURE-SPINE.md`; `validation-report-2026-09-09.md`; the rubric, current-fit, adversarial, implementation-drift, and security review files referenced by that report; the canonical PRD, addendum, and memlog.

### Later 2026-09-09 source advance

After this validation extract was prepared, the GOV-1 spine adopted AD-19 and advanced its target contracts to `hexalith.publication-candidate.v1`, `hexalith.release-evidence.v4`, `hexalith.release-verification-handoff.v3`, and `frontcomposer.release-ledger-record.v2`. It also fixed the interim posture: production releases halt, `HEXALITH_RELEASE_PUBLISH_ENABLED=false` is the deny-only emergency stop, and no current bounded risk exception is authorized. These changes close the architecture-selection and emergency-stop ambiguities identified below; they do not close Product/Release acceptance, parent-source propagation, the eight implementation bundles, authenticated proof, incident-runbook delivery, or revalidation. The canonical PRD and addendum project this later state; the finding tables below retain the validation inputs that caused it.

## Product-Level Deltas For The PRD

| Priority | Source findings | PRD mapping | Product-level delta |
| --- | --- | --- | --- |
| Phase blocker | A1 / SEC-VAL-01; I2 / SEC-VAL-03 | §0; §0.1; §1; §5.0; FR-24; NFR-12; §8.2; SM-2a; §10; D-6/D-11 plus proposed D-16 and G-8 | Stop describing the current path as a conforming governed release. Require a secretless/read-only candidate builder that produces one authenticated, run-bound artifact and a distinct protected publisher/signer that treats packages as data, executes only pinned owner-controlled code, and never checks out or executes candidate source. Candidate-controlled code must never receive publication secrets, write scopes, OIDC/attestation authority, or signing material. An Architect + Release Owner decision must select or reject this split before implementation; no flag flip to the current `governed-release` job is acceptable first. |
| High | A2 / ADV-2026-09-09-01; A4 / ADV-2026-09-09-03; I5 / CF-2026-09-09-05 | FR-24; NFR-12; SM-2; SM-C4/SM-C5; G-1 and proposed G-8 | Every publication-capable attempt, including failed, cancelled, deferred, no-releasable, and partial-publication paths, must receive one unambiguous disposition from a total governed-attempt classifier. Release-attempt identity is immutable; verification reruns append observations and can never replace or weaken an incident or relabel it green. Every started publication is inspected for partial side effects. G-1 backfill cannot be treated as closed merely by adding rows to an unresolved ledger state model. |
| High | A5 / SEC-VAL-02; I4 / CF-2026-09-09-04 | FR-24; NFR-12; SM-2/SM-2a; proposed G-8 | Post-release acceptance and incident classification must execute only an exact active-policy-authorized evaluator closure, independently authenticate the original CI artifact, remain bound to the released candidate, and never use later ambient default-branch helper code. Post-release verification is read-only and cannot authorize publication retroactively. |
| High/Medium | A3 / ADV-2026-09-09-02; RW-01; RW-02; ADV-2026-09-09-05/06 | NFR-7; NFR-12; FR-24 consequences; proposed G-8 | Require one byte-unique canonical representation for every accepted evidence value, backed by cross-language hostile/golden vectors. The exact selected quality run and all authenticated run coordinates must be carried without re-selection. Run uniqueness, conclusion projection, and historical/recovery evaluator identity must be total and deterministic so separately built producers and verifiers cannot disagree. |
| Medium operational | SEC-VAL-04 | §10; D-6 plus proposed D-16; proposed OI-incident-response / G-8 | Bind incident containment to the Release Owner: detection must halt further publication through the then-authoritative control, preserve immutable evidence, prefer unlisting over byte replacement, rotate credentials when exposure is plausible, and permit correction only under a new version. Product must set an acknowledgement/containment target, runbook location, and reopen/escalation condition; the validation report does not supply those product choices. |

### Required status and gate corrections

- In §0, §1, §8.2, and the glossary, replace assertions that each 4.x release currently uses governed controls. State instead that the checked-in caller selects the legacy reusable mode and that the existing protected-environment approval does not satisfy the candidate/credential isolation requirement.
- In §1, replace “only G-6 and G-7 still require engineering.” GOV-1 now has architecture-first work plus material implementation conformance work.
- Keep existing G-2 for the external Builds identity/acceptance seam, but remove “no code work identified.” Add a separate evidence gate, provisionally **G-8 GOV-1 publication-safety conformance**, whose pass condition is: A1–A5 are resolved in the architecture; an owner-accepted split-phase reusable is pre-authorized through the delayed-activation policy; implementation no longer has the validation report's Critical/High release findings; NC-31/NC-32 and the eventual post-release-helper row are registered/resolved; and the same deterministic plus five-lens reviewer gate passes on the amended spine and implementation.
- Map FR-24 to G-1, G-2, and G-8 in §5.0. G-1 remains ledger-history closure; G-8 becomes current/future release-safety conformance. G-1 must not overwrite historical incident states or silently project old rows into the future ledger vocabulary.
- Add a pending decision, provisionally **D-16 Publication privilege boundary**, owned by Architect + Release Owner. It must select the split-phase contract, define the operational posture until it lands (halt or an explicit bounded risk exception), and set the incident-containment SLA/runbook. D-6 should retain the REL-5 history but point to D-16/G-8 for current safety.
- Add open items for: D-16 decision; architecture A1–A5 amendment; split-phase Builds contract acceptance and delayed activation; implementation/register closure; incident runbook and response target; and gate revalidation. Allocate final OI numbers only after merging UX deltas to avoid collisions.

### Success-metric and risk corrections

- **SM-2** should measure a complete append-only attempt ledger, not assert that every published tag has `publish_authorized=true`. A compliant publication requires that value and exact-byte evidence; historical non-compliant/incident attempts remain permanently visible and cannot satisfy the metric by later relabelling.
- **SM-2a** state becomes: graph/catalog core implemented and evidenced; release trust chain and implementation conformance unmet. Its evidence must include the split-phase builder/publisher boundary, exact authorized evaluator identities, authenticated CI/quality/Release/verifier coordinates, and a passing GOV-1 revalidation.
- Add or extend a counter-metric: protected-environment approval, a green workflow, or a manifest classification alone is not proof that candidate code was isolated from publication authority.
- Add explicit risks for candidate-code credential exposure, later-branch verifier drift, ambiguous attempt/ledger classification, and incident detection without containment. The mitigations are G-8 and the owner-bound incident runbook, not the existing approval alone.

## Detail That Belongs Only In The Addendum / Architecture

Keep the following mechanism out of the main capability narrative; summarize outcomes in FR-24/NFR-7/NFR-12 and preserve this depth in the addendum with links to the amended spine:

- Exact builder/publisher/signer job topology; per-job permissions; OIDC versus API-key choice; artifact download, digest, attestation, and no-checkout rules; Builds reusable inputs and delayed two-phase authorization sequence.
- The total governed-attempt predicate over authenticated caller/inner-job topology; the full `needs` conclusion truth table; the selected completed-run uniqueness predicate; the exact `quality_run` carrier.
- Canonical JSON encoder invocation or equivalent escape table, scalar restrictions, hex/surrogate rules, and golden vectors for quote, backslash, controls, DEL, BMP, and supplementary code points.
- Closed `frontcomposer.release-ledger-record.v2` field types/enums/nullability, Release-attempt primary key, linked verification-rerun records, total input-to-disposition mapping, nested `environment_protection` shape, normative CI-copy name, and duplicate-member/name rejection.
- The complete `post_release` evaluator closure, helper blob identities, historical/recovery pin-scope rule, independently downloaded CI handoff, exact candidate binding, and API evidence collection.
- Implementation register detail: the validation report counts 27 active existing items, reserves NC-31 for duplicate asset-name acceptance, NC-32 for legacy mode selection, and requires a later row for post-release helper identity after the architecture rule is fixed. Individual NC mechanics do not belong in the PRD.
- Architecture-only hygiene: corrected Python module diagram, fixture-directory annotation, frontmatter binding, and the local/pinned .NET SDK observation. These are not new product requirements.

## Conflicts With Logged Decisions And Current PRD Text

| Conflict | Evidence | Required reconciliation |
| --- | --- | --- |
| D-6 and the memlog say exact-SHA dispatch plus production approval is the governed publication model; the PRD says every 4.x release is authorized that way. | I2 / SEC-VAL-03 shows the checked-in caller explicitly leaves `governed-release` false and omits governed inputs. A1 / SEC-VAL-01 shows both current reusable modes co-locate candidate execution with publication authority. | Preserve the historical REL-5 trigger decision, but withdraw the conformance claim. Route current safety to D-16/G-8. Do not call v4.2.0 “governed” solely because normal approvals occurred. |
| D-11, §8.2, G-2, and the memlog say GOV-1 local work is done and no code work was identified. | The 2026-09-09 gate verdict is FAIL: one Critical/four High architecture gaps; one Critical/four High implementation clusters; 27 active registered implementation items plus new gaps. | Mark GOV-1 architecture and implementation conformance open. Keep the graph/catalog core as completed evidence rather than reopening AD-1–AD-8/AD-10. |
| AD-18 claims it prevents a write-scoped job from consuming candidate code; `architecture.md` says the pinned publisher restores/authenticates bytes inside Semantic Release. | SEC-VAL-01 demonstrates the job checks out and executes candidate restore/build/shell/Semantic Release while holding write scopes and secrets. | Architecture decision first: replace the claimed invariant with an enforceable split-phase boundary, then update the parent architecture and PRD projection together. |
| D-6/addendum declare `HEXALITH_RELEASE_PUBLISH_ENABLED` retired; AD-12/AD-15 still project it, and SEC-VAL-04 recommends setting it false for containment. | PRD/addendum 2026-09-08 versus spine and security review 2026-09-09. | Do not silently resurrect the retired variable. D-16 must name the authoritative emergency-stop control; until decided, state the conflict and owner explicitly. |
| G-1/SM-2 treat ledger completion primarily as rows plus sign-off; the architecture requires an immutable attempt-keyed state machine whose incidents cannot green. | A4 / RW-03 / ADV-2026-09-09-03. | Amend G-1/SM-2 before backfill is accepted; historical non-compliance remains append-only and permanent. |
| The memlog says the 2026-09-08 PRD rewrite resolved the release model and left mostly evidence/owner records. | Later validation is newer and directly tests the claimed trust boundary against the pinned implementation. | Append a new memlog decision/change when the parent applies this extract; do not rewrite the existing historical entries. |

## Unchanged Product Decisions

No architecture-validation evidence reopens the runtime UI/API/package inventory, FC-NIP, EventStore identity, MCP, or UX decisions. AD-1–AD-8 and AD-10 remain strong evidence for the graph/catalog core. GOV-1 is still governance-only in product scope; the new delta changes release eligibility and evidence claims, not FrontComposer runtime behavior.
