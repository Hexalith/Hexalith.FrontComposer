# Validation Report — Hexalith.FrontComposer Product Requirements Document

- **PRD:** `/home/administrator/projects/hexalith/frontcomposer/_bmad-output/planning-artifacts/prd.md`
- **Rubric:** `/home/administrator/projects/hexalith/frontcomposer/.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-08T08:04:52Z
- **Grade:** Poor

## Overall verdict
This is a high-substance brownfield capability spec: the product bet is specific, failed evidence is named rather than smoothed, baseline FRs are mostly testable, and counter-metrics exist. It is not yet a v1.0 build contract. The work that still gates publication — FC-NIP composition, projection realtime recovery, architecture-review defects, analyzer activation — is tracked as story status and epic numbers instead of PRD-carried done-ness, and the success metrics validate bootstrap and package seals rather than those remaining bets. Downstream UX, architecture, and story workflows can extract IDs and contract paths, but they cannot source-extract what “done” looks like for the requirements that actually remain open.

Adversarial and source/current-fit reviewers materially shift that picture. Adversarial review would kill the document as a ship contract: `approved-for-v1-readiness` launders unauthorized publication, REL-AI-1 is circular, and MCP fail-closed language is weaker than the cited security contract. Source reconciliation shows the PRD is also stale against delivery: Stories 9.3–9.8 and 11.17–11.23 are recorded done, architecture moved FR-24 to unsigned `hexalith.release-evidence.v3`, and Builds issue 17 was reopened with an accepted revision on 2026-08-08 — four days before this PRD’s `updated: 2026-08-12` stamp. Those two extra reviews disagree on *why* 11.20–11.23 fail (overdue backlog vs already landed); they agree the PRD cannot be used as the current v1.0 source of record until it is rewritten against delivery truth and given one named publication-blocker list.

## Dimension verdicts
- Decision-readiness — adequate
- Substance over theater — strong
- Strategic coherence — adequate
- Done-ness clarity — thin
- Scope honesty — adequate
- Downstream usability — adequate
- Shape fit — adequate

## Findings by severity

### Critical (6)
**[Adversarial]** — Status launders an unfinished release as v1.0-ready (§ frontmatter, §0, §12 D-9 / D-6 / D-10 / D-11)
YAML `status: approved-for-v1-readiness` plus D-9 “Blocks: None” is document-approval theater while D-6 still blocks the next NuGet/GitHub publication, D-10 still gates 11.23, and FR-24 says publication remains unauthorized. The PRD’s own §10 names the trap: “Workflow success is confused with release readiness.”
Fix: Split PRD-approval from product-ship authorization. Put a single v1.0 publication blocker list above the FR table. D-9 must not read “None” while any ship gate remains open.

**[Adversarial]** — Publication gates are circular; REL-AI-1 cannot close without the release it forbids (§5.7 FR-24, §12 D-6)
FR-24 requires `REL-AI-1` done only after a real release records `classification=ready` and `publish_authorized=true`, while the same requirement says publication remains unauthorized. That is a bootstrap paradox.
Fix: Define a named non-customer gated evidence release that is not v1.0, or drop REL-AI-1 as a pre-v1.0 completion criterion. Pick one and date it.

**[Adversarial]** — MCP fail-closed claims omit oracles, skill-gate bypass, allow-all, and additive-schema side effects (§2.4 UJ-4, §5.5 FR-17–FR-19, §6 NFR-5, §9 SM-4)
UJ-4/FR-19 claim failures “do not become existence oracles,” but the cited security contract makes `tools/list` a successful empty list, lets skill resources bypass the visibility gate, documents sample allow-all gates, and allows CompatibleAdditive/Warning side effects. SM-4 measures test coverage, not “no leak in any public MCP response.”
Fix: Promote opaque public tokens into FR-19 with a request-class → public-shape table. Ban allow-all in production. Name CompatibleAdditive/Warning as approved or forbidden. Replace SM-4 with an independent leak/oracle suite.

**[Adversarial]** — Stories 11.20–11.23 are a v1.0 publication gate on a schedule that has already failed (§5.0 FR-29, §8.2, §12 D-10)
§8.2 still dates 11.20–11.23 as sequential backlog due 2026-07-24 through 2026-09-11. Read as written, three dates are past and 11.23 remains a publication gate. Source reconciliation (below) finds those stories landed; this finding is the adversarial reading of the frozen PRD text.
Fix: Either re-date with an honest critical path and remove 11.23 from the publication gate, or freeze v1.0 until a replacement decision is signed — and first restated against sprint-status truth.

**[Source reconciliation]** — Epic 9 / FR-13 / FR-26 / D-4 still treat Stories 9.4–9.8 as open gates (PRD §5.0, FR-13, FR-26, D-4 vs sprint-status and specs 9.3–9.8)
The PRD still says Stories 9.4–9.8 must pass before FR-13/FR-26 completion. Implementation records every child done, with Story 9.8 live acceptance on 2026-08-27. Sprint-status still has `epic-9: in-progress` and E9-AI-* open, so the PRD agrees with orchestrator bookkeeping more than with story files.
Fix: Restate FR-13/FR-26 as complete / release-verification; close or residual-ize D-4 against the 9.8 proof packet; list remaining DW rows as residuals, not epic-open gates.

**[Source reconciliation]** — FR-24 / NFR-12 / SM-2 still require signed `hexalith.release-evidence.v2` publication (PRD §5.7, §6, §9, D-6 vs architecture.md)
PRD FR-24 still requires signed, timestamped v2 evidence. Architecture (updated 2026-08-16) checksums unsigned packages, emits `hexalith.release-evidence.v3`, and uses `workflow_dispatch`. REL-5 is done: author signing and RFC 3161 timestamp are deliberately not release requirements. Ledger rows for v4.1.x are valid v3 / `fallback-approved`, and later specs treat v4.2.0/v4.3.0 as published.
Fix: Replace FR-24/NFR-12/SM-2/D-6 with the unsigned-author / repository-signature / v3 / dispatch model; distinguish “packages have been published” from “REL-AI-1 is closed.”

### High (15)
**[Decision-readiness]** — Open-question closure hides live publication tensions (§12.1, §12 D-6, §5.7 FR-24)
§12.1 claims the 2026-07-05 question set is “now resolved,” while D-6 still states publication remains unauthorized and FR-24 says REL-4 is not yet operational proof. Zero `[NOTE FOR PM]` callouts remain.
Fix: Restore a short open-item list for live gates with owner and unblock condition; keep resolved decisions in the register.

**[Strategic coherence]** — Success metrics do not test the remaining v1.0 product bets (§9 vs §1, FR-12/FR-13, FR-26/FR-29)
No SM names composed FC-NIP behavior, self-healing projection realtime, or architecture-review defect closure. SM-1/SM-2 can pass while operator freshness and recovery remain unresolved in the PRD text.
Fix: Add SMs (or explicit mappings) for composed/live FC-NIP, reconnect-and-recover projection freshness, and remaining H-series closures.

**[Done-ness clarity]** — FR-29 is a program envelope, not a requirement (§5.7 FR-29)
Consequences govern Epic 11 administration. Architecture-review findings H1–H12 / M-series are not listed, so v1.0-blocking runtime/security outcomes are not source-extractable.
Fix: Replace FR-29 (or add FR-29.x rows) with one testable outcome per remaining defect class, mapped to H/M ID and story.

**[Done-ness clarity]** — Projection reliability has no PRD-carried bounds (§5.3 FR-12, §6 NFR-8/NFR-9/NFR-11)
FR-12 does not say what recovery means or how long fallback polling may last. NFR-8 defers to “configured budgets” that FR-15 defines only for commands.
Fix: Inline reconnect/retry/fallback numbers (or cite a named contract), name benchmark IDs/caps, and list v1.0 mandatory test lanes.

**[Scope honesty]** — Story 11.24 / EventStore runtime identity is invisible here (§8.2, §8.3, §12 vs Epic 11)
Epic 11 includes an identity-adoption story the PRD does not list as in-scope, non-goal, or deferred.
Fix: Add an explicit in-scope / `[NON-GOAL for MVP]` / deferred row with activation condition and whether publication may proceed while it stays backlog.

**[Adversarial]** — FR-24 / FR-26–FR-29 / REL-* / GOV-1 bundling hides what actually blocks v1.0 (§5.0, §5.7)
The status map mixes baseline verification, open blocking remediations, and closed decision records. A builder cannot answer “what is the minimum ship set?” without a second ledger.
Fix: Split the map into v1.0 ship blockers, v1.0 regression baselines, and closed decision records. Promote remaining gates to first-class IDs with one pass/fail sentence each.

**[Adversarial]** — No consumer-visible public-surface inventory (§4, §5.7 FR-24, §11, §12 D-5)
§11 is a category list, not package IDs, PublicAPI files, MCP tool names, or diagnostic bands. `eng/release-package-inventory.json` names eight packable IDs; the UI row says container image while §4 says the product does not ship FrontComposer-owned containers.
Fix: Add a single table: package ID, packable yes/no, public-API baseline, TFM, breaking-change policy. Resolve UI/AppHost.

**[Adversarial]** — Operator jobs are the vision; developer/CI activity is the scoreboard (§1, §2, §9)
UJ-2/UJ-3 land on row trust and command confirmation. Primary SMs measure bootstrap, package seals, drift, and test coverage. There is no operator outcome metric.
Fix: Restate v1.0 as a developer-framework / package-safety release and move operator freshness out of the ship thesis, or add operator SMs that fail the release if Marc cannot trust freshness.

**[Adversarial]** — D-4 “Resolved” while Stories 9.4–9.8 still block the operator freshness feature (§5.0 FR-13, §12 D-4)
Decision-resolved / product-unshippable smoothing. Source reconciliation finds 9.4–9.8 actually done; the PRD still sells both readings.
Fix: Mark D-4 as decision recorded, composition accepted or not against the 9.8 packet. Rewrite UJ-2 if v1.0 ships without automatic row marking.

**[Adversarial]** — Assumptions A1/A2 are treated as approved product law (§2.4, §4, §12 D-9, §13)
D-9 blesses accepted assumption dispositions as the reason the PRD is `approved-for-v1-readiness`. A2’s “domain-module adoption” has no obligated consumer (“preferably Tenants”).
Fix: Promote A1 into FR-22 acceptance or demote it from v1.0. Replace A2/SM-1 “preferably” with a named module and dated evidence artifact.

**[Source reconciliation]** — D-1 BMad run copy path is archived (PRD §0, D-1 vs archive tree)
The claimed live path `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md` does not exist; files live under `archive/prds/`.
Fix: Point D-1 at the archive as read-only historical, or drop the run-copy claim.

**[Source reconciliation]** — D-6 / D-11 still say Builds issue 17 closed; G2 recorded reopen + SHA on 2026-08-08 (PRD D-6, D-11 vs G2 request)
`g2-hexalith-builds-inline-pre-publish-gate-request.md` records issue 17 reopened 2026-08-08 with accepted immutable revision `a8a50859…`, four days before the PRD `updated` stamp. Shared-catalog contract frontmatter still says revision pending.
Fix: Rewrite D-6/D-11 to the 2026-08-08 accepted revision plus remaining FrontComposer integration/e2e proof.

**[Source reconciliation]** — FR-29 / §8.2 Epic 11 program status is frozen on the 2026-07-17 workstream table (PRD §5.0, §8.2 vs sprint-status, Story 11.23, Story 11.24)
Sprint-status marks 11-17 through 11-24 done. PRD §8.2 still says 11.17b–d / 11.18b–c / 11.19a–d are in review and 11.20–11.23 are future phases. Story 11.24 is absent from FR-29.
Fix: Replace §5.0/§8.2/FR-29 with sprint-status truth; record 11.23 as landed; add 11.24 as a named child.

**[Source reconciliation]** — D-6 REL-4 freeze model contradicts the 2026-08-09 REL-4 story (PRD FR-24 / D-6 vs rel-4 story, architecture.md)
PRD still claims the caller-side freeze guard in `release.yml` is in review. REL-4 story: after exact-source dispatch replaced auto `workflow_run`, the caller freeze-guard is gone. Standing freeze is Builds-hosted `HEXALITH_RELEASE_PUBLISH_ENABLED`.
Fix: Describe freeze as Builds-hosted + operator dispatch; separate default-frozen publisher from already-published 4.1.x+.

**[Source reconciliation]** — EventStore runtime identity and Pact reconciliation expanded NFR-11 without a PRD requirement (PRD §6 NFR-11 vs Story 11.24, Pact specs, identity contract)
NFR-11 lists “Pact checks” but never names an EventStore runtime identity. Story 11.24 is done; `frontcomposer-eventstore-approved-runtime-identity-v1.json` now records `3.103.0` with `migrationApprovalClaimed: false`. `epics.md` still calls 11.24 blocked backlog.
Fix: Add an FR or D-row for owner-approved vs current-compatibility EventStore identity; treat live Pact provider evidence as a distinct v1 gate.

### Medium (22)
**[Decision-readiness]** — Status approval does not cover later load-bearing rows (§12 D-9 vs D-4/D-6/D-10/D-11)
**[Decision-readiness]** — HFCM9002 has no decision-register row (§5.7 FR-27, §12)
**[Strategic coherence]** — Feature prioritization is being replaced by story-status narration (§5.0, §8.2, FR-24/FR-29)
**[Done-ness clarity]** — FR-13/FR-26 done-ness is a story checklist (§5.3 FR-13, §5.7 FR-26, D-4)
**[Done-ness clarity]** — Catalog FRs defer the testable part to “documented” (§5.1 FR-1/FR-3, §5.2 FR-9, §5.3 FR-11)
**[Scope honesty]** — Open-item and assumption density is too clean for the stakes (§12.1, §13, §8.2)
**[Downstream usability]** — Operator IA nouns are not identical across UJ, glossary, and routes (§2.4 UJ-2, §3, FR-10, D-3)
**[Downstream usability]** — Canonical copy vs run copy is stale (§0, §12 D-1)
**[Shape fit]** — Product-contract shape is overloaded with release mechanism and underloaded with operator UX (§4, FR-24, D-6/D-8/D-11)
**[Adversarial]** — Dual “canonical” PRD copies, one path already gone (§0, D-1)
**[Adversarial]** — FR-19 is labeled baseline while its own text says v1.0-blocking (§5.0, FR-19)
**[Adversarial]** — §12.1 claims every open question is resolved while publication, analyzers, and GOV-1 are not
**[Adversarial]** — SM-4 / SM-6 / NFR-11 measure activity and CI greenness, not adopter safety
**[Adversarial]** — Fluent UI Blazor RC is pinned as a v1.0 runtime constraint (§7)
**[Adversarial]** — Shell / operator tenant isolation is not a requirement; only MCP gates are
**[Source reconciliation]** — Success metrics lack the evidence the PRD treats as resolved (§9 / D-7 vs SM-1/SM-2 artifacts)
**[Source reconciliation]** — FR structure dropped load-bearing IA and qualitative UX rules (FR-10 vs ux-design.md / FC-IA-1)
**[Source reconciliation]** — Cited `api-contracts.md` is a 2026-06-02 brownfield scan that no longer matches §11
**[Source reconciliation]** — Successor FC-NIP diagnostic ID was never allocated; HFC1070 remains Trim/AOT
**[Source reconciliation]** — Fluent UI pin in §7 is stale against the Builds catalog (rc.4 vs rc.5)
**[Source reconciliation]** — Architecture quality-review H1–H12 rationale is stale versus 11.6 / 11.8 / 11.11–11.14 delivery
**[Source reconciliation]** — plus related medium notes in `review-source-reconciliation.md`

Full notes and suggested fixes for the 22 medium findings are in the reviewer files listed below.

### Low (8)
**[Scope honesty]** — Addendum depth was dropped (no `addendum.md` beside the canonical PRD)
**[Downstream usability]** — Command-lifecycle vocabulary does not roundtrip (§3 vs FR-15 vs UJ-3)
**[Adversarial]** — Generated-output path declared a public contract with no compatibility window
**[Adversarial]** — “Sensitive cases” and “internals” are undefined in FR-19 / UJ-4
**[Source reconciliation]** — LEGACY-FR-* can still confuse a naive grep (2026-07-05 numbering split is otherwise cleared)
**[Source reconciliation]** — No addendum for qualitative/mechanism overflow
**[Source reconciliation]** — Sprint-status Epic 9 action items remain open after story `done`
**[Source reconciliation]** — Logging governance continued after the PRD froze 11.18 as “in review”

## Mechanical notes
- IDs: UJ-1–6, FR-1–29, NFR-1–13, D-1–11, A1–A2 are unique and contiguous. SM-2a is the only numbering wart.
- Assumptions roundtrip: A1 and A2 are indexed; wording drifts across UJ-6 / FR-22 / SM-5.
- Glossary drift: operator “bounded context” vs Module; Command Lifecycle incomplete versus FR-15; missing entries for EventStore, Level-2/3/4, REL-*/GOV-1, `hexalith.release-evidence.v2`.
- Cross-refs: Story 9.4–9.8, 11.17–11.23, and contract paths resolve only with `epics.md` and `_bmad-output/contracts/`. Story 11.24 is absent. D-1’s 2026-07-05 run copy is archived.
- No `addendum.md` beside the canonical PRD.
- Required sections for this shape are present. A v1.0 blocker table is the missing scan surface.

## Reviewer files
- `review-rubric.md`
- `review-adversarial.md`
- `review-source-reconciliation.md`
