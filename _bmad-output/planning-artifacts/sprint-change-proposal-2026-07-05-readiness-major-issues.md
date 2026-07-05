---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: "Implementation Readiness Assessment 2026-07-05 = NEEDS_WORK; 7 issues across 4 categories, 4 major issues block v1.0 RC implementation-readiness classification."
source_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05.md
status: approved
approval: approved-by-administrator-2026-07-05
scope: Moderate
owner: Product Owner + Developer
issue4_decision: "Add an IA decision gate (FC-IA-1); record provisional recommended answers, final sign-off stays with Product/UX."
applied: "2026-07-05: applied Proposals 2A-2D, 3A, 4A-4D to epics.md, ux-experience-2026-07-05.md, sprint-status.yaml; plus optional REL-1 story surfacing in sprint-status."
---

# Sprint Change Proposal — 2026-07-05 Readiness Major Issues

## Section 1 — Issue Summary

The 2026-07-05 implementation-readiness assessment
(`implementation-readiness-report-2026-07-05.md`) returned **NEEDS_WORK**. No critical violations
remain, but four **major** issues must be navigated before the planning set can be classified
implementation-ready for the v1.0 release candidate:

1. **REL-AI-1 / REL-1 release-evidence gate is open** (PRD FR-24 / Release Governance Gate RG-1).
2. **Epic 11 decomposition parents 11.17, 11.18, 11.19 are not implementation-ready** — they are
   explicitly marked "Split-before-dev" and each bundles multiple independently reviewable
   workstreams.
3. **PRD FR-14, FR-15, FR-16 subclauses are only partially explicit in story AC** — the command
   epics (Epic 3, Epic 4) are `done` and the behaviors are implemented, but several PRD subclauses
   are not pinned in acceptance criteria or cited to tests.
4. **Module-tab route encoding and projection-flyout IA remain open** — the UX experience spine has
   four unresolved open questions that must be decided before Story 11.7 (and any navigation-route
   story) starts development.

This proposal is a **Direct Adjustment** (backlog reorganization) across `epics.md`, the UX
experience artifact, and `sprint-status.yaml`. It reopens **no completed product epic** and changes
**no v1 scope**. Two setup decisions were confirmed with the Product Owner before drafting:
review mode = **Batch**; Issue 4 handling = **add an IA decision gate** (record provisional
recommended answers; final Product/UX sign-off preserved).

## Section 2 — Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering event | [x] | Trigger is the 2026-07-05 readiness report NEEDS_WORK verdict, not a single failed story. |
| 1.2 Core problem | [x] | Four planning-artifact gaps block RC-readiness classification; each is scoped below. |
| 1.3 Evidence | [x] | `implementation-readiness-report-2026-07-05.md`, `prd.md`, `epics.md`, `ux-experience-2026-07-05.md`, `sprint-status.yaml`, `sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md`, source symbols (`ILifecycleStateService`, `AuthorizingCommandServiceDecorator`, `CommandDispatchAuthorizationGate`) and test files. |
| 2.1 Current epic impact | [x] | No `done` epic is invalidated. Epic 11 (backlog) gains child stories; Epics 3/4 (done) gain a trace addendum only. |
| 2.2 Epic-level changes | [!] | Epic 11 order table row 10 expands into child stories; a UX IA gate is added to the Epic 11 decision-gates block and Story 11.7 DoD. |
| 2.3 Future epic impact | [x] | Epic 11 route/navigation work depends on the IA gate; package-boundary stories 11.11–11.14 depend on REL-1 package-consumer evidence but are not blocked from creation. |
| 2.4 New/remove epics | [x] | No new epic. Child stories decompose existing Epic 11 parents; REL-1 already exists as a release-governance story. |
| 2.5 Priority/order | [!] | Issue 1 residual is implementation-only; Issues 2–4 are pre-dev planning corrections that must land before Epic 11 route/remediation dev kickoff. |
| 3.1 PRD conflicts | [x] | PRD FR-14/15/16 wording is correct; the gap is epic/story traceability, addressed by a trace addendum. FR-24 wording was already tightened by the REL-AI-1 proposal. |
| 3.2 Architecture conflicts | [x] | No structural architecture change. Route-path IA aligns with the existing Story 11.0 `/commands/{BoundedContext}/{CommandTypeName}` path contract. |
| 3.3 UX conflicts | [!] | UX experience open questions 1–4 are routed to the FC-IA-1 gate with provisional recommended answers; Epic 8 projection-flyout reconciled as strictly-secondary-into-tabs. |
| 3.4 Other artifacts | [!] | `epics.md`, `ux-experience-2026-07-05.md`, and `sprint-status.yaml` need updates. Release workflow/tests are owned by REL-1 (out of this proposal's scope). |
| 4.1 Direct adjustment | Viable | Recommended: backlog reorganization + trace + decision gate. |
| 4.2 Rollback | Not viable | No completed product work should be rolled back. |
| 4.3 MVP review | Not viable | v1 scope is unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment, Moderate scope. |
| 5.1–5.5 Proposal components | [x] | Captured in Section 4. |
| 6.1–6.2 Final review | [x] | Actionable, scoped, with OLD→NEW edits. |
| 6.3 Approval | [!] | Pending explicit Administrator approval before applying edits. |
| 6.4 Sprint status update | [!] | Add FC-IA-1 action item; confirm REL-AI-1 remains open. |
| 6.5 Handoff | [x] | Moderate: Product Owner, Developer, Architect, UX, Test Architect, Release Owner. |

### Epic Impact

- **Epic 11 (backlog):** decomposition-parents 11.17/11.18/11.19 become index containers with child
  stories; the implementation-order table and decision-gate block are updated. No implemented work.
- **Epic 3 & Epic 4 (done):** receive a **non-invasive trace addendum** only. Existing story AC and
  implementation are untouched — done stories are **not reopened**.
- **Release Governance Gate RG-1:** already correctly modeled; no change here beyond confirming
  REL-AI-1 stays open until REL-1 implementation records evidence.

### Artifact Conflicts

- `epics.md`: Epic 11 order table + parent-story decomposition + Story 11.7 DoD + a command-FR trace
  addendum after Epic 4.
- `ux-experience-2026-07-05.md`: Open Questions routed to a new FC-IA-1 IA Decision Gate section.
- `sprint-status.yaml`: new `FC-IA-1` action item; REL-AI-1 unchanged (remains open).

## Section 3 — Recommended Approach

**Direct Adjustment** (backlog reorganization + traceability refinement + a decision gate).

Rationale:

- The requirements, owners, and gates already exist; the gaps are decomposition discipline, explicit
  traceability, and an unrecorded IA decision — not missing scope.
- Reopening done epics or spinning up a new epic would be disproportionate and would blur ownership.
- The IA questions belong to stories not yet in dev, so a tracked/owned/dated **decision gate**
  (mirroring the resolved Story 11.0 and Story 11.8 gate pattern) is the lowest-risk instrument; it
  records provisional answers without forcing final product decisions prematurely.

Effort: Low–Medium (documentation/backlog edits; no product-source change in this proposal).
Risk: Low. Timeline: complete before Epic 11 route/remediation dev kickoff and before v1.0 RC
readiness re-classification.

## Section 4 — Detailed Change Proposals

### Issue 1 — REL-AI-1 / REL-1 release-evidence gate (no new planning edit)

**Finding:** already fully navigated in planning by
`sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md` (approved). That proposal:
tightened PRD FR-24 exit criteria; added the RG-1 implementation-story trigger to `epics.md`; created
the `REL-1` story (`_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md`);
and added `implementation_story`, `evidence_required`, and `closure_rule` to the `REL-AI-1` action in
`sprint-status.yaml`.

**Conclusion:** the residual is **implementation** of REL-1 (release workflow/config/governance-test
changes + package-consumer validation), not planning. `REL-AI-1` correctly stays `open` until the
Release Owner records evidence paths. **No planning edit is proposed here.**

**Optional hardening (recommended):** surface `REL-1` in the `sprint-status.yaml` story list (not only
via the action item's `implementation_story` field) so it is visibly tracked through create → dev →
review → done like other stories. Flagged as optional; apply only if the team wants REL-1 visible in
the story board.

**Handoff:** Release Owner + Developer + QA/Test Architect implement REL-1 per its existing acceptance
criteria; keep `REL-AI-1` open until evidence exists.

---

### Issue 2 — Decompose Epic 11 stories 11.17 / 11.18 / 11.19

Each parent stays as an index container (its blockquote and problem statement are preserved) and gains
a **Decomposition** block listing child stories, each with a named validation lane. The Epic 11
implementation-order table row 10 is expanded to reference the children.

#### Proposal 2A — Epic 11 order table (row 10)

Artifact: `_bmad-output/planning-artifacts/epics.md` — `### Epic 11 Implementation Order` table.

OLD:

```text
| 10 | 11.17 / 11.18 / 11.19 | Split before ready-for-dev; do not implement as broad bundles. |
```

NEW:

```text
| 10a | 11.17a–d one-type-per-file split (CLI · SourceTools · MCP/runtime · Shell) | Per-package child stories; each names its validation lane. |
| 10b | 11.18a–c LoggerMessage migration (fail-closed/security · warning+ · hot-path) | Security-adjacent child first; each names its lane. |
| 10c | 11.19a–d enforcement/policy alignment (CS1591 · NuGet audit · l10n+rename · analyzer-elevation decision) | Three child stories + one decision gate; no global warning/analyzer disable. |
```

#### Proposal 2B — Story 11.17 decomposition (one-type-per-file, by package)

Artifact: `epics.md` — inserted immediately after the Story 11.17 "Split-before-dev" blockquote,
before the `As a FrontComposer maintainer,` line.

NEW (insert):

```text
**Decomposition (correct course 2026-07-05).** Split by package into independently reviewable child
stories. Each keeps the parent constraint: mechanical only — behavior and public-API shape unchanged
except intentional file organization and any documented API-baseline update. A durable one-type-per-file
Governance guard (the "multi-type file" blind-spot guard class) is added or extended so the convention
is enforced, not merely applied.

- **11.17a — CLI package split.** `MigrationCommand.cs` (23 types), `InspectCommand.cs` (14 types) →
  one-type-per-file. Validation lane: CLI in-process xUnit lane + `frontcomposer.cli.inspect.v1` /
  `frontcomposer.cli.migrate.v1` contract pins + CLI `PublicAPI.Shipped.txt` unchanged.
- **11.17b — SourceTools package split.** `DriftDetection.cs` (17 types) → one-type-per-file.
  Validation lane: SourceTools drift lane + HFC parity + generated-output byte stability (P12
  no-`CompilationProvider` isolation preserved).
- **11.17c — MCP/runtime split + benchmark-harness relocation.** `SkillCorpus.cs` (~45 types) →
  one-type-per-file, and move the LLM benchmark harness out of the runtime package into
  `Shell.Tests.Bench` (`[Trait("Category","Performance")]`). Validation lane: MCP in-process lane +
  Testing package-boundary tests + `Shell.Tests.Bench` builds; the runtime package no longer ships the
  benchmark harness.
- **11.17d — Shell interface+impl+DTO bundle split.** Shell multi-type files (interface + impl + DTO
  bundles) → one-type-per-file, retaining the documented Fluxor action-group exception. Validation
  lane: focused Shell one-type-per-file Governance guard + broad Shell non-Contract lane +
  `PublicAPI.FcTbl.Shipped.txt` unchanged.
```

#### Proposal 2C — Story 11.18 decomposition (LoggerMessage, security-first)

Artifact: `epics.md` — inserted immediately after the Story 11.18 "Split-before-dev" blockquote.

NEW (insert):

```text
**Decomposition (correct course 2026-07-05).** Split by defect class, security-adjacent work first.
Each child preserves the parent's sanitization constraint: no raw token, tenant-secret, payload, stack
trace, or sensitive identifier is emitted.

- **11.18a — Fail-closed / security log sites (first).** MCP + Shell fail-closed branches →
  `[LoggerMessage]`. Validation lane: MCP + Shell Governance sanitized-logging lane (ties to
  NFR6/NFR10); sanitization tests prove no sensitive value is emitted.
- **11.18b — Warning-and-above log sites.** All Warning+ severity sites across the 50 Shell files →
  `[LoggerMessage]`. Validation lane: Shell unit lane + a guard that Warning+ sites use
  source-generated logging.
- **11.18c — Hot-path log sites.** Command-lifecycle, projection-refresh, and polling hot-path sites →
  `[LoggerMessage]`. Validation lane: LoggerMessage guard; remaining direct calls are below the
  migration threshold or documented intentional.
```

#### Proposal 2D — Story 11.19 decomposition (enforcement/policy, by defect class)

Artifact: `epics.md` — inserted immediately after the Story 11.19 "Split-before-dev" blockquote.

NEW (insert):

```text
**Decomposition (correct course 2026-07-05).** Split by defect class. Each child names its validation
lane and does not disable warnings or analyzer findings globally.

- **11.19a — Doc-comment (CS1591) enforcement realignment.** Restore documented CS1591 enforcement on
  the Contracts public API-freeze folders (the `.editorconfig` re-raise is currently dead under the
  src-wide NoWarn). Validation lane: Release build under `TreatWarningsAsErrors=true` + a guard proving
  CS1591 is enforced on the API-freeze surface.
- **11.19b — AppHost NuGet audit suppression.** Replace the blanket `NU1902-04` NoWarn with
  per-advisory `NuGetAuditSuppress` (CI-verifiable). Validation lane: CI audit lane / Governance test.
- **11.19c — Localization + identifier alignment.** Localize the `FcHomeCard` aria-label and the UI
  host `lang="en"`/English strings; rename `HFC2106_ThemeHydrationEmpty` (ID string unchanged; obsolete
  alias if the constant is public). Validation lane: Shell localization/Governance lane +
  diagnostic-catalog parity.
- **11.19d — Analyzer-elevation decision gate.** Architect records the `AnalysisMode Recommended`
  decision (adds no packages; burn-down cost owned). This is a decision gate, not broad implementation;
  any resulting implementation stories name their validation lane. Recorded under
  `_bmad-output/contracts/`.
```

---

### Issue 3 — PRD FR-14 / FR-15 / FR-16 subclause traceability

**Finding:** Epics 3 and 4 are `done` and the flagged behaviors are implemented and test-covered, but
several PRD subclauses are not named in story AC. The correction is a **trace addendum** that pins each
subclause to its owning story and named implementation symbol, and requires the exact passing-test
citation to be recorded in the story evidence/change-log before RC classification. **No done story AC
is rewritten and no story is reopened.**

Verified implementation symbols (not fabricated):
`ILifecycleStateService` (Contracts/Lifecycle) declares `IdempotentConfirmed`, `NeedsReview`, `Warning`,
`Degraded` alongside `Submitting/Acknowledged/Syncing/Confirmed/Rejected`;
`AuthorizingCommandServiceDecorator` + `CommandDispatchAuthorizationGate` (Shell/Services/Authorization)
own the authorization sequencing; `LifecycleStateService`, `PendingCommandStateService`,
`PendingCommandOutcomeResolver`, `FcPendingCommandSummary` own runtime lifecycle/pending state.

#### Proposal 3A — Command-FR trace addendum

Artifact: `epics.md` — inserted at the end of Epic 4, immediately before `## Epic 5: AI-Agent (MCP)
Surface`.

NEW (insert):

```text
### Command FR subclause traceability (PRD FR-14 / FR-15 / FR-16)

Added by correct course 2026-07-05 to make the partial-trace subclauses explicit. Epics 3 and 4 are
done and these behaviors are implemented; this addendum pins each subclause to its owning story and
named symbol. Before v1.0 RC classification, the owning story's evidence/change-log must cite the exact
passing test method(s), or add a short AC-refinement note. This addendum does not reopen any done story.

| PRD subclause | Owning story | Implementation symbol | AC status | Evidence action before RC |
| --- | --- | --- | --- | --- |
| FR-14 unsupported field types render placeholders, do not break the form | 3.1 | `FcFieldPlaceholder` + HFC1002 | In AC | Cite generator/`CommandRenderer*` test. |
| FR-14 supported field-type parsing | 3.1 / 3.2 | generated `CommandForm` parsers | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`, `CommandRendererTestFixtures.cs`. |
| FR-14 nullable numeric fields compile + round-trip culture-aware | 3.1 | nullable-numeric codegen (PR #48 minor batch) | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`. |
| FR-14 form state preserved on retryable pre-accept failures | 4.5 | retry/degraded path | Implicit | Cite retry test + `FcFormAbandonmentGuardTests`. |
| FR-14 `MessageId` is a ULID reused across pre-accept retry attempts | 3.3 + 4.5 | FC-CMD identity + `IUlidFactory` | Implicit | Cite `LifecycleStateServiceTests` / pending-command tests. |
| FR-15 Submitting / Acknowledged / Syncing / Confirmed / Rejected | 3.4 | `FcLifecycleWrapper` | In AC | Covered. |
| FR-15 IdempotentConfirmed, NeedsReview, Warning | 3.4 (+ runtime) | `ILifecycleStateService` + `LifecycleStateService` | Implicit — states exist, not named in AC | Add AC-refinement note naming these terminals; cite `FcLifecycleWrapperRejectionTests` / `FcLifecycleWrapperThresholdTests`. |
| FR-15 Degraded / accepted-HTTP is not projection-confirmed | 3.5 / 3.6 | `GET /api/v1/commands/status/{id}` confirmed-stable | In AC | Covered (3.5 + 3.6 budgets). |
| FR-16 `[RequiresPolicy]` evaluated before `BeforeSubmit` and again after for protected commands | 4.4 | `AuthorizingCommandServiceDecorator` + `CommandDispatchAuthorizationGate` | Implicit — sequencing not named | Add AC-refinement note pinning before/after sequencing; cite `RequiresPolicyAttributeTests` + authorization tests. |
| FR-16 service boundary enforces authorization | 4.4 | `AuthorizingCommandServiceDecorator` | In AC | Covered. |
| FR-16 FC-CNC v1 blocks later local submits (no queue/batch) | 4.3 | FC-CNC one-at-a-time | In AC | Covered. |
| FR-16 destructive confirmation / abandonment guard | 4.1 / 4.2 | `FcDestructiveConfirmationDialog` / `FcFormAbandonmentGuard` | In AC | Covered. |

Two AC-refinement notes are the only story-text changes: (1) name `IdempotentConfirmed` / `NeedsReview`
/ `Warning` as surfaced lifecycle terminals in Story 3.4's evidence; (2) name the `[RequiresPolicy]`
before/after `BeforeSubmit` sequencing in Story 4.4's evidence. Both reference existing code and tests;
neither changes implemented behavior.
```

---

### Issue 4 — Module-tab route encoding + projection-flyout IA decision gate (FC-IA-1)

Per the confirmed setup decision, add a tracked/owned/dated IA decision gate mirroring the resolved
Story 11.0 / 11.8 gate pattern, with **provisional recommended answers** to the four UX open questions.
Final sign-off stays with Product/UX.

#### Proposal 4A — UX experience Open Questions → FC-IA-1 gate

Artifact: `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` — replace the `## Open
Questions` section.

OLD:

```text
## Open Questions

1. Should every module workspace have a required default tab name such as **Overview**, **Search**, or the module plural name?
2. Should module tab selection be encoded in the route path, query string, or an internal router state with shareable links?
3. Should the shell rail keep a secondary projection flyout at all, or should projections be reachable only after opening the module workspace?
4. Which module should serve as the first visual reference implementation: Parties, Tenants, or another adopter module?
```

NEW:

```text
## IA Decision Gate — FC-IA-1 (module-tab route encoding + projection-flyout IA)

Added by correct course 2026-07-05. The four IA questions below are a **blocking decision gate**:
Story 11.7 (command/projection route-contract implementation) and any other navigation/module-tab route
story may **not** move to ready-for-dev until FC-IA-1 is recorded as a Product/UX-signed-off decision.
Owner **Product/UX + Architect**, assigned **2026-07-05**, due **before Epic 11 route/navigation dev
kickoff**. Provisional recommended answers are stated so implementation has a starting contract; final
values are Product/UX's to confirm.

1. **Required default tab.** Recommended: **Yes** — each module workspace opens on a required default
   tab. Provisional name = the module plural label (e.g. "Parties"), fallback "Overview". Preserves the
   one-entry-per-module rule.
2. **Tab selection encoding.** Recommended: **route-path segment** (`/{module}/{tab}`) — deep-linkable
   and shareable, consistent with the Story 11.0 path contract
   `/commands/{BoundedContext}/{CommandTypeName}`. Not query string; not internal-only router state.
3. **Projection flyout.** Recommended: **keep as strictly secondary** — the rail flyout routes into
   module workspace tabs and never becomes a second primary IA. This reconciles the Epic 8 projection
   flyout with the one-entry-per-module spine.
4. **First reference module.** Recommended (provisional): **Tenants** (already mid Fluent v5
   conversion); Parties acceptable if Product prefers a domain-richer exemplar.

**Closure rule:** FC-IA-1 moves to done only when Product/UX records final answers to 1–4 (accepting or
amending the recommendations) and the decision is referenced by Story 11.7. Until then Story 11.7 stays
blocked.
```

#### Proposal 4B — Epic 11 decision-gates block references FC-IA-1

Artifact: `epics.md` — append a sentence to the Epic 11 `**Decision gates ...**` blockquote (the block
ending with the Story 11.8 resolution).

NEW (append to the decision-gates blockquote):

```text
> **IA gate FC-IA-1** (module-tab route encoding + projection-flyout IA) — owner **Product/UX +
> Architect**, assigned **2026-07-05**, due **before Epic 11 route/navigation dev kickoff**. Recorded in
> `ux-experience-2026-07-05.md` (IA Decision Gate — FC-IA-1). Story 11.7 and any navigation/module-tab
> route story stay blocked until FC-IA-1 is Product/UX-signed-off.
```

#### Proposal 4C — Story 11.7 DoD depends on FC-IA-1

Artifact: `epics.md` — Story 11.7 acceptance criteria, extend the final blocking `Given`.

OLD:

```text
**Given** the contract-confirmation DoD,
**When** Story 11.0 is not done,
**Then** this story remains blocked and may not move to ready-for-dev.
```

NEW:

```text
**Given** the contract-confirmation DoD,
**When** Story 11.0 is not done **or** the FC-IA-1 module-tab route encoding / projection-flyout IA gate
is not Product/UX-signed-off,
**Then** this story remains blocked and may not move to ready-for-dev.
```

#### Proposal 4D — sprint-status FC-IA-1 action item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml` — append to `action_items`.

NEW (append):

```yaml
  - epic: 11
    action: "FC-IA-1: Record the module-tab route encoding and projection-flyout IA decision (UX open questions 1-4) with Product/UX sign-off before Story 11.7 or any navigation/module-tab route story moves to ready-for-dev."
    owner: "Product/UX + Architect"
    assigned: "2026-07-05"
    due: "before Epic 11 route/navigation dev kickoff"
    status: open
    provisional_recommendation:
      - "required default tab = module plural label, fallback Overview"
      - "tab encoding = route-path segment /{module}/{tab} (consistent with /commands/{BoundedContext}/{CommandTypeName})"
      - "projection flyout kept as strictly secondary, routing into module workspace tabs"
      - "first reference module = Tenants (provisional; Parties acceptable)"
    closure_rule: "Move to done only after Product/UX records final answers to open questions 1-4 and Story 11.7 references the decision."
```

## Section 5 — Implementation Handoff

Scope classification: **Moderate** (backlog reorganization + traceability + a decision gate).

Route to:

- **Product Owner + Developer:** apply Proposals 2A–2D (Epic 11 decomposition), 3A (command-FR trace
  addendum), and 4B–4C (Epic 11 gate references) to `epics.md`.
- **UX + Product:** own FC-IA-1 (Proposal 4A); record final answers to the four IA questions.
- **Architect:** co-owns FC-IA-1 and owns the 11.19d analyzer-elevation decision.
- **Test Architect:** confirms the named validation lanes for the 11.17/11.18/11.19 child stories and
  the exact test citations for the FR-14/15/16 subclauses.
- **Release Owner + Developer:** implement REL-1 (Issue 1 residual); keep REL-AI-1 open until evidence.

Recommended sequence:

1. Approve this proposal.
2. Apply Epic 11 order-table + parent decomposition edits (2A–2D).
3. Apply the command-FR trace addendum (3A) and the two AC-refinement evidence notes.
4. Add the FC-IA-1 gate to the UX artifact, Epic 11 gate block, Story 11.7 DoD, and sprint status
   (4A–4D).
5. Product/UX record FC-IA-1 final answers; Architect records the 11.19d analyzer decision.
6. Create child stories from 11.17/11.18/11.19 per the implementation-order table, each naming its
   validation lane, before any child moves to ready-for-dev.
7. Re-run implementation-readiness once REL-1 evidence exists and FC-IA-1 is signed off.

Success criteria:

- 11.17/11.18/11.19 no longer appear as broad bundles; each child story names a validation lane.
- PRD FR-14/15/16 subclauses each trace to an owning story + symbol + cited test.
- The module-tab IA is a recorded, owned, dated gate that blocks Story 11.7 until signed off.
- REL-AI-1 remains open until REL-1 records FR-24 evidence.

## Section 6 — Approval State

Status: **approved by Administrator on 2026-07-05.** Setup decisions confirmed 2026-07-05: Batch mode;
Issue 4 handled via the FC-IA-1 IA decision gate with provisional recommended answers; optional REL-1
story surfacing accepted.

Applied 2026-07-05:

- `epics.md`: Epic 11 order-table rows 10a/10b/10c (2A); Story 11.17/11.18/11.19 decomposition blocks
  (2B–2D); command-FR trace addendum after Epic 4 (3A); FC-IA-1 line in the Epic 11 decision-gates
  block (4B); Story 11.7 DoD extended to depend on FC-IA-1 (4C).
- `ux-experience-2026-07-05.md`: Open Questions section replaced by the FC-IA-1 IA Decision Gate (4A).
- `sprint-status.yaml`: FC-IA-1 action item added (4D); `rel-1-release-evidence-gate-before-v1-rc`
  surfaced in the story list (optional hardening).

Issue 1 required no planning edit — REL-1 implementation remains the residual and `REL-AI-1` stays
open. No product source code, release workflow, or governance test was modified by this proposal.
