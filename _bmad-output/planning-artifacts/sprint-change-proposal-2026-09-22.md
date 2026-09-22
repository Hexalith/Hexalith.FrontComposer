---
title: Sprint Change Proposal — Readiness Gate Reconciliation
date: 2026-09-22
status: approved
approval: approved
approved: 2026-09-22
mode: batch
scope: major
source_gate: failed sprint-readiness review
---

# Sprint Change Proposal — Readiness Gate Reconciliation

## 1. Approved Decision

The user approved this direct-adjustment replan on 2026-09-22:

1. corrects status and traceability drift across the PRD, architecture, UX design,
   epics, and sprint status;
2. preserves completed Epic 9 and Epic 11 delivery history;
3. carries unresolved acceptance, security, adopter, release, and migration evidence
   into explicitly classified work;
4. creates independently completable backlog slices for the remaining product work;
   and
5. leaves every approval and external dependency open until its required receipt
   exists.

Approval of this proposal authorized the five-artifact reconciliation described in
Section 9 and the use of Section 7 as input to the create-epics-and-stories workflow.
It does not approve any Product, Architecture, Security, Release, or external-owner
decision listed below.

## 2. Trigger and Problem Statement

The trigger is the failed sprint-readiness gate after Story 11.32. The failure is not
a single implementation defect. It is a planning-integrity failure caused by four
forms of drift:

- completed implementation remains represented as active epic work;
- technical evidence capture is conflated with owner approval;
- newly changed repository identities invalidate evidence that was bound to an older
  exact tuple; and
- open product and governance obligations are listed as prose or action items instead
  of independently completable work with explicit closure evidence.

This is primarily a misunderstanding-of-done and artifact-synchronization issue,
with additional requirements exposed by current repository evidence. The product
goal remains achievable. No completed delivery needs to be rolled back.

### 2.1 Evidence observed on 2026-09-22

| Evidence | Observation | Planning consequence |
|---|---|---|
| Current repository HEAD | b61e51e13b9d7a39b22d6463a23532bdc696610f | The proposal is bound to this inspection point, not a future execution point. |
| Root gitlinks | Hexalith.Builds 2fba3497043fe5ffcfe4dc44c51a09eae9b950ab; Hexalith.EventStore db1e9d735b8c7186d51682725f08085cda98fe67 | The Story 11.25 and successor evidence tuples do not describe the current pair. |
| EventStore successor evidence | The 3.106 evidence reconciliation remains in review and has no migration-approval receipts | Story 11.25 cannot close G-3 or E11R-AI-1. |
| Release tags | v4.2.0, v4.3.0, v4.4.0, and v4.5.0 exist after the ledger's last covered v4.1.1 release | OI-8 must cover all four post-ledger releases, not only v4.2 through v4.4. |
| Compatibility baseline | eng/release_compatibility.py still names 4.4.0 while v4.5.0 exists | OI-11 is open again and needs a deliberate update or accepted lag decision. |
| Fluent documentation | The catalog resolves rc.5 while docs/fluent-ui-v5-contingency.md cites rc.2 | OI-10 remains open. |
| Adopter proof | tests/tenants-bootstrap-acceptance.md does not exist | G-6 and OI-5 remain open. |
| Incident readiness | docs/release-incident-response.md does not exist | OI-15 and the incident portion of G-8 remain open. |
| MCP posture | Allow-all gates remain usable and no complete non-Development prohibition, endpoint-authentication assertion, and named SM-4 audit packet was found | G-7, OI-2, and OI-3 remain open. |
| GOV-1 register | The current register still contains open closure bundles and nonconformances | G-2, G-8, and OI-14 through OI-17 remain open. |
| Story 11.32 validator | The command in Section 11 reports an empty/missing File List and unreconciled changed files | Story 11.32 remains completed history, but a new current-state integrity repair is required. |
| Epic 9 evidence | Stories 9.1 through 9.8 and the retrospective are done; the live Story 9.8 proof exists | Epic 9 delivery can be closed without claiming Product acceptance. |

## 3. Status Reconciliation Decision

| Item | Current contradiction | Proposed canonical state | Rationale |
|---|---|---|---|
| Epic 9 | All stories and its retrospective are done, but the epic and six implementation action items remain open | Mark delivery epic done. Close E9-AI-1 through E9-AI-6 against their completed stories and evidence. Keep G-5/OI-1 open as a separate Product acceptance task. | Product acceptance is not implementation work and must not keep completed delivery artificially active. |
| Story 9.8 | Live proof is complete, but Product acceptance is absent | Keep story done; leave the Product acceptance task open | Evidence completion and stakeholder acceptance are distinct events. |
| Epic 11 | Sprint status says done while PRD/architecture/epics still describe an active extension or final acceptance | Keep the bounded 11.25 through 11.32 remediation delivery done and update narrative artifacts to match | Reopening a completed epic would erase useful delivery truth and still would not supply missing receipts. |
| Story 11.25 | Status says done, while its own frozen decision says G-3 stays open until separate receipts exist | Keep done as historical technical capture only. State explicitly that it neither approves migration nor closes G-3. Do not reuse 11.25 for later repository tuples. | The technical capture was delivered; the approval condition was not. |
| E11R-AI-1 | Open but still points to completed Story 11.25 and an already stale successor tuple | Keep open; remap to EVT-ID-1 and EVT-APP-1 below | A new story must bind evidence to the then-current exact tuple, followed by a distinct owner approval task. |
| Story 11.32 | Marked done, while its current mechanical validation fails | Preserve done history; create PLAN-INT-2 for present-day integrity repair and regression coverage | Later repository drift is new work, not grounds to rewrite accepted history. |
| GOV-1 | Broad in-progress item mixes external, implementation, evidence, and approval work | Keep GOV-1 open as the parent obligation and replace its execution body with EXT-BUILDS-1 and the independently completable GOV-B through GOV-J slices below | Each slice gains a single owner, evidence boundary, and honest completion rule. |

## 4. Gate Traceability

The Classification column uses:

- I — implementable story;
- A — explicit approval or evidence task; and
- X — documented non-sprint external dependency.

No gate changes to done merely because its work is mapped.

| Gate | Required outcome | Classified work | State after artifact reconciliation |
|---|---|---|---|
| G-1 Release-ledger closure | Accepted schema/classifier, v4.1.1 owner hash/sign-off, and evidence-backed rows for every later release | REL-LEDGER-1 (A), REL-LEDGER-2 (I) | Open |
| G-2 Dependency provenance and external seam | Accepted upstream split contract, exact caller topology, authenticated handoff, and source alignment | EXT-BUILDS-1 (X), GOV-B (I), GOV-E (I), GOV-SRC-1 (I), GOV-ACCEPT-2 (A) | Open |
| G-3 EventStore identity and migration | Current exact tuple passes provider/AppHost evidence and receives the required migration receipts, or an approved ownership transfer | EVT-ID-1 (I), EVT-APP-1 (A), EVT-XFER-1 (A, conditional) | Open |
| G-4 Product/UX readiness | Fluent decision, implemented UX evidence matrix, documentation parity, and final Product approval | FLUENT-APP-1 (A), UX-A through UX-F (I), DOC-A through DOC-C (I), PRD-APP-1 (A) | Open |
| G-5 Epic 9 acceptance | Product accepts the completed Story 9.8 proof packet | E9-APP-1 (A) | Open |
| G-6 Adopter bootstrap | Named adopter produces dated, candidate-bound three-call/projection/command proof | ADOPT-KIT-1 (I), EXT-ADOPTER-1 (X), ADOPT-APP-1 (A only if D-7 selects a substitute) | Open |
| G-7 MCP leak/oracle audit | Production guardrails, negative tests, named audit, and independent security sign-off | MCP-SEC-1 (I), MCP-SEC-2 (I), MCP-APP-1 (A) | Open |
| G-8 Publication-safety conformance | Eight closure bundles, incident proof, deterministic/five-lens review, and owner approvals | EXT-BUILDS-1 (X), GOV-B through GOV-H (I), GOV-I (I), GOV-J (A), GOV-ACCEPT-1 and GOV-ACCEPT-2 (A) | Open |

## 5. Open-Item Traceability

| Open item | Classification and target | Closure rule |
|---|---|---|
| OI-1 | E9-APP-1 (A) | Dated Product acceptance cites the Story 9.8 proof. Epic 9 delivery bookkeeping is corrected separately. |
| OI-2 | MCP-APP-1 (A) | Independent security reviewer signs the complete MCP negative-evidence packet after MCP-SEC-1 and MCP-SEC-2. |
| OI-3 | MCP-SEC-1 and MCP-SEC-2 (I) | Non-Development allow-all usage fails closed; endpoint authentication, tenant/tool/resource gates, unregistered URI behavior, leak/oracle checks, and SM-4 audit evidence pass. |
| OI-4 | FLUENT-APP-1 (A) | Product and Architecture accept an exact catalog identity or record a dated alternative decision. |
| OI-5 | ADOPT-KIT-1 (I), EXT-ADOPTER-1 (X), optional ADOPT-APP-1 (A) | FrontComposer supplies a deterministic kit; the selected external adopter supplies candidate-bound proof. A D-7 substitute requires a dated Product decision. |
| OI-6 | PLAN-INT-1 (I) | PRD, architecture, epics, UX, and sprint status tell the same Epic 11/history/residual-work story and pass consistency checks. |
| OI-7 | GOV-SRC-1 (I) | FC-DEP-1, G2 request, GOV-1 source, architecture, PRD, and the current identity register agree without replacing exact provenance with labels. |
| OI-8 | REL-LEDGER-1 (A) and REL-LEDGER-2 (I) | Schema/classifier and the pending v4.1.1 owner hash/sign-off are accepted, then append-only rows cover v4.2.0 through v4.5.0 plus any intervening release discovered at execution. |
| OI-9 | REL-A3-APP-1 (A) | Release Owner records a dated A3 sample-host container decision. |
| OI-10 | DOC-C (I) or a dated Product exception (A) | Contingency documentation names the exact catalog-owned Fluent identity, or the exception has owner, expiry, and revisit trigger. |
| OI-11 | REL-BASE-1 (I) plus approval only if lag is intentional | Baseline advances to the latest valid published release, currently v4.5.0, or an owner-approved lag decision records rationale and expiry. |
| OI-12 | GOV-ACCEPT-1 (A) | Product and Release accept D-16, the production halt, the deny-only emergency stop, and the no-exception posture. |
| OI-13 | GOV-ACCEPT-2 (A) | Product and Release accept the reconciled sources without weakening the adopted publication boundary. |
| OI-14 | EXT-BUILDS-1 (X) and GOV-B through GOV-H (I) | All eight closure bundles have authenticated evidence; unresolved Critical/High findings remain blocking. |
| OI-15 | GOV-I (I) | Incident runbook exists and a dated tabletop proves acknowledgement, containment, immutable evidence, and recovery handling. |
| OI-16 | UX-A through UX-F (I) | Every canonical UX/accessibility matrix row has implementation and evidence; missing rows remain open. |
| OI-17 | GOV-J (A/evidence) | Deterministic validation and all five review lenses are rerun on one unchanged evidence candidate, with every finding disposition recorded. |
| OI-18 | EVT-XFER-1 (A, conditional) | If FrontComposer cannot own migration approval, Product/Architecture/Release explicitly transfer ownership with recipient, required receipts, due condition, and non-closure effect. |
| OI-19 | DOC-A and DOC-B (I) | Component, index, status, page-toolbar/tab, migration, and classification documentation matches the implemented contract and verification evidence. |

## 6. FR-30 and Obligation Traceability

FR-30 is present in the PRD but absent from the epics FR Coverage Map. Add this
canonical mapping:

| Requirement | Proposed owner | Required proof |
|---|---|---|
| FR-30 Tenant-scoped EventStore behavior | TEN-SCOPE-1 | End-to-end command/query/subscription/count/storage evidence proves tenant separation and fail-closed behavior when tenant identity is absent or mismatched. |

TEN-SCOPE-1 must exercise real production seams. Unit-only proof is insufficient. Its
completion closes FR-30 delivery traceability but does not close G-3 migration approval.

The G-1 through G-8 mappings in Section 4 become the canonical obligation map in the
PRD, epics, and sprint status. Architecture carries only the technical dependencies;
UX carries only G-4/G-7 behaviors that affect interaction or evidence. This prevents
the same approval from acquiring conflicting states in several documents.

## 7. Proposed Backlog for Epic/Story Creation

The identifiers below are stable aliases, not final story numbers. The downstream
create-epics-and-stories workflow should allocate numeric epic/story IDs after checking
the current sequence. Each item has one independently testable completion boundary.

### 7.1 Epic: Planning Truth and Traceability

#### PLAN-INT-1 — Reconcile canonical planning truth

- Type: implementable story
- Owner: Product Owner + Architect
- Scope: apply the approved status decisions and G/OI/FR-30 mappings to the five
  canonical artifacts.
- Acceptance: all five artifacts agree; completed history remains intact; unresolved
  approvals are open; no stale story is named as current evidence ownership.

#### PLAN-INT-2 — Restore artifact-integrity validation

- Type: implementable story
- Owner: QA automation maintainer
- Scope: reconcile the current Story 11.32 validation failure and add a check that
  detects epic/story/action contradictions and missing File Lists without rewriting
  historical acceptance.
- Acceptance: the current validator passes on its declared scope; an intentional
  stale-status fixture fails; the command and evidence are recorded.

### 7.2 Epic: Runtime Identity, Tenant Safety, and Adoption

#### EVT-ID-1 — Capture the current EventStore release tuple

- Type: implementable story
- Owner: Runtime maintainer + Architect
- Scope: freeze the exact FrontComposer HEAD, EventStore gitlink, Builds gitlink,
  package identity, provider verification, and AppHost smoke evidence at story
  execution time.
- Acceptance: one immutable evidence packet passes for one exact tuple; drift fails
  closed; no migration approval is inferred.

#### EVT-APP-1 — Approve or reject EventStore migration

- Type: approval/evidence task
- Owner: Architect + Release Owner
- Dependency: EVT-ID-1
- Acceptance: all required durable receipts cite the exact EVT-ID-1 tuple and record
  approve/reject. Absence of any receipt leaves G-3 open.

#### EVT-XFER-1 — Record conditional migration-approval ownership transfer

- Type: approval task, created only if OI-18 is invoked
- Owner: Product Owner + Architect + Release Owner
- Acceptance: named receiving owner, required evidence, trigger, due condition, and
  milestone effect are explicit. A transfer does not itself close G-3.

#### TEN-SCOPE-1 — Prove FR-30 tenant-scoped EventStore behavior

- Type: implementable story
- Owner: Runtime + QA maintainers
- Acceptance: end-to-end query, subscription, count, and storage paths separate two
  tenants; missing/mismatched tenant identity fails closed; the production adapters
  are exercised.

#### ADOPT-KIT-1 — Publish a deterministic three-call adopter proof kit

- Type: implementable story
- Owner: Framework maintainer
- Scope: versioned bootstrap steps, evidence schema, candidate-SHA binding, generated
  projection assertion, generated command assertion, and redaction guidance.
- Acceptance: a clean consumer fixture can execute the kit without repository-private
  assumptions.

#### EXT-ADOPTER-1 — Execute named adopter proof

- Type: documented non-sprint external dependency
- Owner: selected Hexalith.Tenants maintainer, or Parties maintainer after D-7
- Dependency: ADOPT-KIT-1
- Acceptance: the external repository publishes dated, candidate-bound proof. The
  FrontComposer sprint may track the dependency but may not mark it done on the
  adopter's behalf.

### 7.3 Epic: MCP Production Security

#### MCP-SEC-1 — Enforce production MCP authorization composition

- Type: implementable story
- Owner: MCP/runtime maintainer
- Acceptance: non-Development startup fails when allow-all tenant-tool or
  resource-visibility gates are selected; mapped endpoints require host
  authentication; normal registered operations still work.

#### MCP-SEC-2 — Prove MCP leak/oracle resistance

- Type: implementable story
- Owner: Security QA
- Acceptance: negative tests cover unregistered URIs, cross-tenant and cross-user
  access, unauthorized-vs-not-found oracle behavior, lifecycle-state isolation,
  redacted diagnostics, and the named SM-4 audit class.

#### MCP-APP-1 — Record independent MCP security sign-off

- Type: approval/evidence task
- Owner: reviewer independent of MCP-SEC-1 implementation
- Dependency: MCP-SEC-1 and MCP-SEC-2
- Acceptance: signed review cites the immutable evidence candidate and records all
  residual risks; an unsigned packet leaves G-7 open.

### 7.4 Epic: UX Conformance and Documentation Parity

#### UX-A — Implement shell, account, hamburger, search, and route focus behavior

- Acceptance: FR-8 and FR-10 keyboard/focus matrices pass across direct navigation,
  palette navigation, module tabs, search, and responsive shell states.

#### UX-B — Implement validation, rejection, command-dialog, and navigation-guard focus

- Acceptance: FR-14 and FR-16 errors link to summaries, focus moves deterministically,
  FC-CNC outcomes are distinguishable, and blocked/abandoned navigation is safe.

#### UX-C — Make projection and command lifecycle announcements deterministic

- Acceptance: FR-11, FR-12, and FR-15 announcement/state matrices pass, including
  coalescing, terminal ceilings, auth/scope loss, retry, and polling outcomes.

#### UX-D — Complete fresh-row visual and silent-expiry behavior

- Acceptance: FR-13 indicator behavior passes normal, forced-colors, reduced-motion,
  filtered/paged, and silent-expiry scenarios without focus theft.

#### UX-E — Produce responsive accessibility evidence

- Acceptance: NFR-3/SM-6 evidence covers 320 CSS px, 400 percent zoom, text spacing,
  target size, focus not obscured, forced colors, reduced motion, keyboard, and
  the required manual assistive-technology checks.

#### UX-F — Complete reusable UX testing helpers

- Acceptance: FR-22 helpers express the canonical interaction and accessibility
  matrices without app-specific selectors or hidden timing assumptions.

UX-A through UX-F are implementable stories owned by UX + Shell + QA maintainers.
They may be completed independently, but PRD-APP-1 waits for all required rows.

#### DOC-A — Reconcile component, index, status, toolbar, and tab documentation

- Type: implementable story
- Acceptance: public component and page-pattern docs name the implemented APIs,
  states, and evidence with no stale status claims.

#### DOC-B — Reconcile migration catalog and classification documentation

- Type: implementable story
- Acceptance: migration index, classification, compatibility, and consumer guidance
  agree with current artifacts and supported versions.

#### DOC-C — Correct the Fluent v5 contingency identity

- Type: implementable story
- Acceptance: documentation resolves the exact catalog-owned identity and contains
  a mechanical drift check; alternatively, a dated exception is recorded outside
  story completion.

#### FLUENT-APP-1 — Decide the exact Fluent v5 posture

- Type: approval task
- Owner: Release Owner
- Acceptance: dated decision cites the exact package/catalog identity, RC/GA posture,
  breaking-change rule, revalidation trigger, and accessibility evidence requirement.

#### PRD-APP-1 — Record final product-readiness approval

- Type: approval task
- Owner: Product Owner
- Dependency: every Product-owned prerequisite in G-1 through G-8
- Acceptance: dated digest names satisfied gates and explicitly lists any rejected
  or still-open gate. The PRD status changes only when this record exists.

### 7.5 Epic: Governed Split Publication

#### EXT-BUILDS-1 — Obtain owner-accepted split reusable Builds revision

- Type: documented non-sprint external dependency
- Owner: Hexalith.Builds owner
- Acceptance: an immutable upstream revision implements and accepts the split
  reusable contract. FrontComposer cannot complete or activate the caller switch
  before this dependency exists.

#### GOV-B — Switch the caller to the accepted exact two-job topology

- Type: implementable story
- Dependency: EXT-BUILDS-1
- Acceptance: the caller pins the accepted immutable revision; candidate/build and
  publisher jobs are structurally distinct; delayed activation and rollback are
  tested.

#### GOV-C — Remove publication authority from candidate/build execution

- Type: implementable story
- Acceptance: candidate/build jobs have no production environment, publication
  secret, write scope, OIDC/attestation authority, signing material, or equivalent
  ambient capability; hostile probes fail.

#### GOV-D — Produce and authenticate a run-bound publication candidate

- Type: implementable story
- Acceptance: candidate, fallback digest, policy, run/attempt, coordinates, and
  production approval are authenticated together; replay, mutation, and
  hostile-candidate fixtures fail closed.

#### GOV-E — Implement handoff v3 and a typed append-only release ledger

- Type: implementable story
- Acceptance: handoff validation is total; ledger transitions are typed and
  append-only; retry/attempt semantics are explicit; malformed or retroactive
  mutation fails.

#### GOV-F — Classify and publish using candidate-free pinned code

- Type: implementable story
- Acceptance: protected publication consumes data only, uses manifest v4 and
  owner-controlled pinned code, never checks out or executes candidate source, and
  preserves normalized-ZIP equivalence.

#### GOV-G — Pin post-release evaluation and durable incident evidence

- Type: implementable story
- Acceptance: post-release evaluation uses an immutable owner-controlled helper with
  no ambient/candidate helpers; recovery and incident artifacts survive the run and
  are independently retrievable.

#### GOV-H — Reject duplicate destination asset names

- Type: implementable story
- Acceptance: duplicate or ambiguous destination names fail before publication with
  redacted diagnostics; unique assets preserve deterministic ordering.

#### GOV-I — Publish and tabletop the release incident runbook

- Type: implementable evidence story
- Acceptance: the runbook assigns acknowledgement/containment targets, revoke/stop,
  preserve, assess, recover, and communicate actions; a dated tabletop produces an
  immutable evidence packet and tracked findings.

#### GOV-J — Revalidate GOV-1 on one unchanged candidate

- Type: approval/evidence task
- Dependency: GOV-B through GOV-I
- Acceptance: deterministic checks plus adversarial, edge-case, verification-gap,
  acceptance, and structure/prose lenses run against one immutable candidate; every
  finding has a disposition; any unresolved Critical/High item blocks closure.

#### GOV-ACCEPT-1 — Accept D-16 and the incident posture

- Type: approval task
- Owner: Product Owner + Release Owner + Architect
- Acceptance: dated decision covers AD-19, release halt, deny-only emergency stop,
  no-exception posture, and incident response target.

#### GOV-SRC-1 — Reconcile governance source projections

- Type: implementable documentation/traceability story
- Acceptance: architecture, PRD, FC-DEP-1, G2 request, GOV-1 story, identity register,
  and current gitlinks distinguish owner revision, execution pin, current gitlink,
  and evidence provenance without drift.

#### GOV-ACCEPT-2 — Accept the reconciled governance sources

- Type: approval task
- Owner: Product Owner + Release Owner
- Dependency: GOV-SRC-1
- Acceptance: dated acceptance cites exact source digests and preserves every
  publication-safety invariant.

### 7.6 Epic: Release Evidence and Decisions

#### REL-LEDGER-1 — Accept the release-ledger schema and classifier

- Type: approval/evidence task
- Owner: Release Owner + Architect
- Acceptance: schema version, classifier identity, allowed transitions, append-only
  rule, and evidence requirements are accepted.

#### REL-LEDGER-2 — Backfill all post-v4.1.1 release evidence

- Type: implementable evidence story
- Dependency: REL-LEDGER-1
- Acceptance: the existing v4.1.1 row receives its still-pending owner hash/sign-off;
  evidence-backed rows cover v4.2.0, v4.3.0, v4.4.0, and v4.5.0, plus any later
  release present at execution; missing evidence remains visibly missing and is
  never inferred or relabeled.

#### REL-BASE-1 — Reconcile the published compatibility baseline

- Type: implementable story
- Acceptance: the baseline is advanced to the latest evidence-backed published
  version, or an explicit owner decision records why it must lag, until when, and
  what blocks advancement.

#### REL-A3-APP-1 — Decide the A3 sample-host container

- Type: approval task
- Owner: Product Owner + Architect
- Acceptance: dated accept/revise/reject decision cites the exact sample-host
  boundary and downstream impact.

#### E9-APP-1 — Accept the Epic 9 live proof

- Type: approval task
- Owner: Product Owner
- Acceptance: dated acceptance cites the Story 9.8 live record. Rejection creates a
  new residual story; it does not rewrite Stories 9.1 through 9.8.

## 8. Sequencing and Dependencies

1. Planning correction: approve this proposal, then complete PLAN-INT-1 and create
   the proposed epics/stories with backlog status.
2. Decisions and external starts: initiate EXT-BUILDS-1 and EXT-ADOPTER-1; complete
   FLUENT-APP-1, REL-LEDGER-1, and the source reconciliation needed for execution.
3. Independent implementation: execute EVT-ID-1, TEN-SCOPE-1, MCP-SEC-1/2,
   UX-A through UX-F, DOC-A through DOC-C, ADOPT-KIT-1, and unblocked GOV slices.
4. Evidence convergence: complete release-ledger backfill, compatibility baseline,
   adopter evidence, incident tabletop, and the unchanged-candidate GOV review.
5. Owner decisions: obtain security, migration, governance, Epic 9, A3, and final
   Product approvals only after their evidence dependencies exist.

The work is Major in aggregate, but every implementable story is intentionally small
enough to complete and verify without declaring its parent gate closed.

## 9. Approved Artifact Amendments

Applied on 2026-09-22 to the five canonical artifacts. Proposed aliases remain
unnumbered pending create-epics-and-stories.

### 9.1 prd.md

- Replace stale Epic 9 and Epic 11 delivery-state prose with the canonical decisions
  in Section 3.
- Keep G-1 through G-8 open; add their classified work aliases from Section 4.
- Replace G-3's stale fixed tuple wording with a current-at-execution exact-tuple rule
  and retain immutable historical tuples as evidence history.
- Update OI-1 through OI-19 with the targets and closure rules in Section 5.
- Preserve the pending v4.1.1 owner hash/sign-off and extend OI-8 to every later
  release currently present, including v4.5.0.
- Keep Product approval pending until PRD-APP-1 exists.

### 9.2 architecture.md

- Describe Epic 11 as completed remediation history, with current residuals owned by
  new work rather than by Story 11.25.
- Add the exact-tuple succession rule: evidence binds one immutable FrontComposer,
  EventStore, Builds, and package tuple; later gitlink movement creates new evidence
  work and never silently migrates approval.
- Add the GOV split-story and external-dependency boundaries without marking the
  upstream Builds contract accepted.
- Add FR-30 technical traceability to TEN-SCOPE-1.

### 9.3 ux-design.md

- Preserve the current interaction contracts.
- Replace undifferentiated OI-16/OI-19 prose with traceability to UX-A through UX-F
  and DOC-A through DOC-B.
- Keep G-4, Fluent approval, manual accessibility evidence, and Product acceptance
  open until their dedicated records exist.
- Cross-reference MCP-SEC-2 only for user-visible auth/scope-loss and oracle behavior;
  do not claim the UX artifact supplies independent security approval.

### 9.4 epics.md

- Mark Epic 9 delivery done and separate E9-APP-1 from its completed stories.
- Mark Epic 11 delivery done; annotate Story 11.25 as technical capture only and
  carry E11R-AI-1 forward to EVT-ID-1/EVT-APP-1.
- Add FR-30 to the FR Coverage Map with TEN-SCOPE-1.
- Add the approved proposed-backlog handoff from Section 7 with stable aliases,
  dependencies, and traceability. Defer final numeric identifiers and complete story
  materialization to create-epics-and-stories.
- Keep GOV-1 as an open parent obligation until all applicable implementation,
  evidence, external, and approval work closes.

### 9.5 sprint-status.yaml

- Set epic-9 to done; close E9-AI-1 through E9-AI-6 with their completed story and
  evidence references; add E9-APP-1 as open.
- Keep epic-11 and Stories 11.25 through 11.32 done.
- Keep E11R-AI-1 open and remap it to EVT-ID-1/EVT-APP-1 instead of treating Story
  11.25 as unfinished.
- Add the approved aliases to a machine-readable change-backlog handoff:
  implementation work as proposed, approval/evidence tasks as open, and external
  dependencies as external. Do not encode external work as a sprint story that
  FrontComposer can complete.
- Preserve REL-AI-1 and GOV-1 as open until the replacement mapping is created and
  accepted; never close them by bookkeeping alone.

## 10. Path-Forward Evaluation

| Option | Viability | Effort | Risk | Finding |
|---|---|---|---|---|
| Direct adjustment | Viable and recommended | High aggregate, incremental per story | Medium | Preserves completed history, restores traceability, and makes remaining work executable. |
| Roll back completed stories/epics | Not viable | High | High | It destroys accurate delivery history and cannot manufacture missing approvals or external evidence. |
| Reduce or redefine the MVP | Not recommended now | Medium planning effort | High product risk | The core goal remains achievable. Removing publication, tenant safety, accessibility, or security gates would weaken the approved readiness definition. |

Recommended path: Option 1, direct adjustment. If external owners decline the Builds
or adopter dependencies, return to Product for an explicit milestone-scope decision;
do not silently reclassify those dependencies as complete.

## 11. Validation Evidence and Known Red State

The planning review used repository state and artifact content, not status labels
alone. The following command was rerun:

    python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-32-epic-11-artifact-integrity-enforcement.md

Result: failed. The validator reported that the story File List was missing or empty
and listed unreconciled story-owned changes, including sprint-status.yaml, epics.md,
the validator sources/tests, multiple Epic 11 records, and current submodule gitlinks.
PLAN-INT-2 owns this new red state. The failure does not retroactively erase the
accepted Story 11.32 delivery.

No full build or product test suite was run because this reconciliation changes planning
and tracking artifacts only.

## 12. Handoff and Success Criteria

| Role | Responsibility |
|---|---|
| Product Owner | Approve/edit this proposal; own Product decisions; never infer approval from implementation status. |
| Architect | Own exact-tuple, FR-30, Fluent, MCP, and publication-boundary consistency. |
| Product Owner / backlog workflow | Run create-epics-and-stories from Section 7 and allocate final numeric identifiers. |
| Developers and QA | Implement only accepted stories and attach the specified evidence. |
| Security reviewer | Independently decide MCP-APP-1 after the immutable negative-evidence packet exists. |
| Release Owner | Own ledger, migration, D-16, incident, GOV review, and source-acceptance decisions. |
| External Builds/adopter owners | Produce their own immutable upstream/adopter evidence; FrontComposer only records dependency state. |

This correction succeeds when:

- all five canonical artifacts agree on Epic 9, Epic 11, Story 11.25, E11R-AI-1,
  GOV-1, and release state;
- FR-30 and every G/OI obligation has exactly one visible delivery/evidence path;
- every new implementable unit can close without falsely closing an approval or
  external dependency;
- completed delivery history remains unchanged;
- current mechanical planning/artifact validation passes; and
- final readiness remains blocked until every required approval and external receipt
  actually exists.

## 13. Checklist Disposition

| Checklist area | Status | Result |
|---|---|---|
| 1. Trigger and context | Done | Failed readiness gate and repository evidence identified. |
| 2. Epic impact | Done | Epic 9/11 history preserved; new residual epics/stories proposed and sequenced. |
| 3. Artifact conflict analysis | Done | Required changes to all five canonical artifacts and secondary evidence/docs identified. |
| 4. Path forward | Done | Direct adjustment selected; rollback and MVP reduction rejected. |
| 5. Proposal components | Done | Issue, impact, path, backlog, dependencies, handoff, and success criteria included. |
| 6.1 Checklist review | Done | All applicable sections are represented in this proposal. |
| 6.2 Accuracy review | Done | Claims are bounded to inspected repository evidence; missing approval is never treated as approval. |
| 6.3 User approval | Done | User explicitly replied Continue and approve on 2026-09-22. |
| 6.4 Apply sprint changes | Done | The five canonical artifacts were reconciled; proposed aliases remain unnumbered for the downstream workflow. |
| 6.5 Confirm handoff | Done | Major-scope handoff is Product Manager/Solution Architect, followed by create-epics-and-stories. |

## 14. Approval

Current state: approved 2026-09-22.

The user authorized the five-artifact reconciliation without additional conditions.
This approval does not satisfy any Product, Architecture, Security, Release, or
external-owner approval task described by the proposal.

## 15. Workflow Execution Record

- Issue addressed: sprint-readiness artifact contradictions and unowned residual gates.
- Change scope: Major.
- Artifacts modified: prd.md, architecture.md, ux-design.md, epics.md, and
  sprint-status.yaml; this proposal was finalized as approved.
- Routed to: Product Manager and Solution Architect for the Major-scope handoff;
  create-epics-and-stories for final epic grouping, numeric story IDs, and story
  materialization.
- Structural validation: Git whitespace checks passed; sprint-status.yaml parsed
  successfully; G-1 through G-8, OI-1 through OI-19, FR-30, and Epic 9/11 status
  assertions passed.
- Known red gate: the Story 11.32 artifact validator still reports an empty/missing
  File List and unreconciled changed files. PLAN-INT-2 owns that new work; the result
  is not represented as complete.
