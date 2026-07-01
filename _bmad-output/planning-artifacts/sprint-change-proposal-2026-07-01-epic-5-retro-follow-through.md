---
workflow: bmad-correct-course
date: 2026-07-01
mode: Batch
status: Approved - routed for process follow-up
change_trigger: Epic 5 retrospective follow-through for Epic 6 customization readiness
source: _bmad-output/implementation-artifacts/epic-5-retro-2026-06-05.md
scope_classification: Moderate
approval: Approved by Administrator on 2026-07-01
---

# Sprint Change Proposal: Epic 5 Retro Follow-Through for Epic 6 Customization Readiness

## 1. Issue Summary

The Epic 5 retrospective completed on 2026-06-05. It did not invalidate Epic 6, but it identified
specific changes needed before customization work could proceed safely:

- File List reconciliation had failed repeatedly across recent stories, including Story 5.1 and
  Story 5.5.
- Epic 6 needed an explicit customization override-precedence record before Level 2 templates and
  Level 4 full-view overrides were reasoned about together.
- Epic 6 accessibility diagnostics needed to stay independent from MCP resource visibility and
  schema-negotiation security.
- MCP contract updates from Epic 5 had to remain stable: projection resource URIs use
  `frontcomposer://<bounded-context>/projections/<projection-name>`, lifecycle retry fields are nested
  under `retry`, schema compatibility blocks side effects before allocation/dispatch/query work, and
  hidden-equivalent failures remain opaque.
- Historical story labels in brownfield code and tests should be cleaned only when nearby code is
  already being edited for a real reason.

Evidence from the trigger file:

- Epic 5 action items E5-AI-1 through E5-AI-5 explicitly name File List reconciliation, MCP doc
  alignment, hidden-equivalent/schema-before-side-effect test preservation, Epic 6 precedence and
  accessibility-boundary records, and stale-label cleanup.
- The retrospective states that no discovery invalidates Epic 6, but Epic 6 must carry generated
  protocol-shape discipline and mechanical evidence hygiene.
- The retrospective's documentation audit already updated stale MCP URI, lifecycle retry, and schema
  fingerprint wording where verified.

Current-state reconciliation as of 2026-07-01:

- Stories 6.1, 6.2, 6.3, and 6.4 are done.
- The Epic 6 stories absorbed most Epic 5 retro guidance: each story records the MCP non-goals,
  schema/fingerprint boundaries, FC-CUST contract artifacts, diagnostic phase honesty, and File List
  reconciliation requirements.
- The same evidence-hygiene defect still recurred in Stories 6.1, 6.2, and 6.4 before review
  auto-fixes. Story 6.3 passed the gate. Epic 8 now carries an open action item to make changed-file
  and story-task reconciliation mechanical.

## 2. Impact Analysis

Epic impact:

- Epic 5 remains done. Its MCP contracts are stable and should not be reopened.
- Epic 6 remains valid. No epic redefinition, rollback, or resequencing is needed.
- Epic 6 story text needed retrospective-derived acceptance additions and non-goals; those additions
  are already visible in the completed Story 6.1-6.4 artifacts.
- Epic 7 is not directly changed by this proposal, but diagnostic tooling work must preserve the
  phase-honesty learned in Epic 6.
- Epic 8's open reconciliation action should be treated as the current home for the repeated
  evidence-hygiene process gap unless the backlog owner creates a cross-epic QA automation story.

Story impact:

- Story 6.1: Level-2 templates needed the FC-CUST override-resolution and Level-2 contract artifact,
  a direct cache-key/equality pin, and File List reconciliation as a review gate.
- Story 6.2: Level-3 field slots needed truthful HFC1038-HFC1041 phase disposition, a Level-3
  contract artifact, and explicit File List reconciliation.
- Story 6.3: Level-4 full-view overrides needed deterministic Level-4 over Level-2 precedence,
  truthful HFC1042-HFC1046/HFC2121 phase disposition, and File List reconciliation.
- Story 6.4: override accessibility diagnostics needed HFC1050-HFC1055 build diagnostics, DEBUG plus
  Development panel gating, Release/Production non-render guarantees, and contract evidence.
- Process follow-up: story review promotion should not rely on a manual checklist alone for File List
  reconciliation.

Artifact conflicts:

- PRD: no authored PRD exists in this project. `epics.md` is the requirements inventory and PRD proxy.
  No MVP scope change is required.
- Epics: `epics.md` remains valid. The richer Story 6.1-6.4 artifacts supersede the shorter epic-level
  acceptance wording where they add retrospective-derived details.
- Architecture: no new architecture pattern is required. Current architecture/project-context already
  records MCP resource grammar, schema-fingerprint rules, fail-closed MCP gates, and customization
  precedence.
- UX: no user-flow change is required. NFR6 accessibility is reinforced through Story 6.4 diagnostics.
- Secondary artifacts: sprint-status action tracking should connect the File List automation action to
  its Epic 5 origin and repeated Story 6/Epic 8 evidence.

Technical impact:

- No change to `CanonicalSchemaMaterial`, schema fingerprint algorithms, MCP projection URI grammar,
  fail-closed MCP gates, package versions, public API baselines, generated-output paths, pacts, or
  submodule layout is proposed here.
- The residual implementation work is process automation: a changed-file/story-task/File List
  reconciliation check before review completion.

## 3. Recommended Approach

Selected path: Direct Adjustment with retrospective reconciliation.

Rationale: the Epic 5 retro findings refine story readiness and review gates; they do not change the
product goal or require rollback. The correct response is to keep Epic 6's added contract and
diagnostic constraints, avoid reopening completed MCP work, and route the remaining evidence-hygiene
gap to QA automation.

Effort:

- Low for documentation/reconciliation: this proposal records the impact and confirms that most story
  changes already landed.
- Medium for the remaining process automation, depending on whether it is implemented as a BMAD
  story-review guard, script, sprint-status validator, or CI check.

Risk:

- Low product risk: no MVP scope or shipped behavior changes.
- Medium process risk: manual File List reconciliation has repeatedly failed even when explicitly
  listed as a task.

Rejected paths:

- Rollback: not useful. The completed Epic 6 stories incorporated the needed constraints.
- MVP review: not applicable. The retro says Epic 6 can proceed and current sprint status shows Epics
  1-7 done.
- Reopen Epic 5: not needed. MCP behavior is stable; the follow-through belongs to customization
  stories and review automation.

## 4. Checklist Outcome

- [x] 1.1 Trigger identified: Epic 5 retrospective dated 2026-06-05.
- [x] 1.2 Core problem defined: retrospective follow-through was needed for Epic 6 readiness and
  evidence hygiene.
- [x] 1.3 Evidence gathered: Epic 5 action items, completed Epic 6 story files, sprint-status history,
  `epics.md`, architecture, project context, and docs index reviewed.
- [x] 2.1 Current epic assessed: Epic 5 remains complete.
- [x] 2.2 Epic changes assessed: Epic 6 needed story-level refinements, not an epic rewrite.
- [x] 2.3 Future epics assessed: Epic 7 inherits diagnostic phase honesty; Epic 8 currently carries
  the open reconciliation automation action.
- [x] 2.4 New/obsolete epics assessed: none.
- [x] 2.5 Epic ordering assessed: no resequencing needed.
- [!] 3.1 PRD impact assessed: no authored PRD exists; `epics.md` is the requirements inventory and
  PRD proxy.
- [x] 3.2 Architecture impact assessed: existing architecture records the necessary MCP and
  customization boundaries.
- [N/A] 3.3 UI/UX impact: no new screen or flow change; NFR6 is reinforced by diagnostics.
- [x] 3.4 Secondary artifacts assessed: sprint-status/action tracking and review automation remain
  relevant.
- [x] 4.1 Direct adjustment viable: yes.
- [N/A] 4.2 Rollback path: not useful.
- [N/A] 4.3 MVP review: no MVP scope impact.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal components captured in this document.
- [x] 6.3 User approval obtained: Administrator approved this proposal on 2026-07-01.
- [x] 6.4 Sprint-status update applied: the existing Epic 8 reconciliation action was updated rather
  than duplicating a second action item.
- [x] 6.5 Handoff plan defined below.

## 5. Detailed Change Proposals

### Story 6.1 - Level-2 ProjectionTemplate overrides

Section: Acceptance Criteria / Dev Notes

OLD:

```markdown
Given a Blazor component annotated [ProjectionTemplate] with a typed Context parameter,
When registered via AddHexalithProjectionTemplates<TMarker>,
Then it renders in place of the generated view for its projection+role.

Given an invalid template,
When built,
Then HFC1033 / HFC1034 / HFC1037 / HFC1035-HFC1036 are reported respectively.
```

NEW:

```markdown
Add AC3: produce the FC-CUST override-resolution and Level-2 template contract artifact,
including Level 4 -> Level 2 -> generated default precedence, Level 3 composition boundaries,
diagnostic disposition, MCP non-goals, and cache-safety.

Add AC4: behavior remains unchanged or pinned, with an explicit ProjectionTemplateMarkerInfo
equality/cache-key pin, unchanged public baselines unless intentionally owned, and File List
reconciliation against git changed files before review promotion.
```

Rationale: implements Epic 5 retro E5-AI-4 and E5-AI-1 before the customization epic builds on
override precedence. Current state: already present in Story 6.1 and completed, with review auto-fix
for File List drift.

### Story 6.2 - Level-3 field-slot overrides

Section: Acceptance Criteria / Diagnostic Disposition

OLD:

```markdown
Given an invalid/duplicate slot selector or component,
When built,
Then HFC1038-HFC1041 are reported per the catalog.
```

NEW:

```markdown
Make the diagnostic phase truthful. HFC1038-HFC1041 must be recorded as startup/runtime or
call-site diagnostics unless real build-time SourceTools/analyzer emission and default-lane tests
are added. The FC-CUST Level-3 contract must cite source/tests and list any build-time analyzer
work as an explicit owned follow-up.

Add behavior/evidence AC: preserve schema/MCP/package/public baselines and reconcile the File List
against git changed files before review promotion.
```

Rationale: prevents false build-time diagnostic claims and preserves MCP/schema boundaries. Current
state: already present in Story 6.2 and completed, with review auto-fix for File List drift and a
recorded residual catalog wording seam.

### Story 6.3 - Level-4 full-view overrides

Section: Acceptance Criteria / Precedence and Diagnostics

OLD:

```markdown
Given a registered Level-4 view override,
When the projection route resolves,
Then the override registry supplies the full custom view in place of the generated one.

Given both a Level-2 template and a Level-4 override exist,
When resolved,
Then precedence follows the documented override-resolution order deterministically.
```

NEW:

```markdown
Pin the exact order: Level 4 full-view override -> Level 2 template -> generated default body.
Level 3 field slots compose only when the selected body delegates through generated delegates.
Context.DefaultBody must bypass the active Level-4 descriptor to avoid recursion.

Record HFC1042-HFC1046 and HFC2121 with the real build/startup/runtime phase and fallback behavior.
Do not claim build-time SourceTools emission unless implementation and default-lane tests prove it.

Keep MCP resource security, schema fingerprints, package versions, EventStore boundaries, public
baselines, and generated-output paths untouched. Reconcile the File List against git changed files.
```

Rationale: completes the Epic 5 retro requirement to document override precedence before Level-2 and
Level-4 are reasoned about together. Current state: already present in Story 6.3 and completed; this
story passed File List reconciliation without review correction.

### Story 6.4 - Override-accessibility safety diagnostics

Section: Acceptance Criteria

OLD:

```markdown
Given a custom override,
When built,
Then HFC1050-HFC1055 flag missing accessible name, keyboard reachability, suppressed focus,
missing aria-live parity, motion-without-reduced-motion, and color-without-forced-colors.

Given DEBUG + IsDevelopment(),
When a customization-contract mismatch exists,
Then FcCustomizationDiagnosticPanel displays it.
```

NEW:

```markdown
Pin HFC1050-HFC1055 across Level 2, Level 3, and Level 4 customization surfaces, with negative
controls for non-custom components, comments, and safe fallback patterns.

Render contract-mismatch diagnostics only when both gates hold: DEBUG build and Development
environment. Release, Production, and Staging must not register or render the mismatch provider or
panel path.

Record the FC-CUST accessibility diagnostics contract with analyzer disposition, static-analysis
limits, panel redaction, non-goals, source/test citations, and changed-file reconciliation.
```

Rationale: keeps customization diagnostics independent of MCP security while preserving NFR6. Current
state: already present in Story 6.4 and completed, with review auto-fix for File List drift.

### Sprint Status / Process Tracking

Section: `action_items`

OLD:

```yaml
action_items:
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
```

NEW:

```yaml
action_items:
  - epic: 8
    action: "Make changed-file, story-task, and File List reconciliation mechanical before story review completion. Origin: Epic 5 retro E5-AI-1; repeated in Stories 6.1, 6.2, 6.4 and Epic 8."
    owner: "QA automation maintainer"
    status: open
```

Rationale: avoid a duplicate action item while preserving origin and scope. This edit should only be
applied after Administrator approval because `sprint-status.yaml` is the active workflow ledger.

### PRD / Requirements Inventory

Section: PRD impact

OLD:

```markdown
No authored PRD exists.
```

NEW:

```markdown
No authored PRD exists. `epics.md` remains the requirements inventory and PRD proxy. No MVP scope
change is required; the completed Story 6.1-6.4 artifacts provide the detailed acceptance deltas.
```

Rationale: matches the established project convention used by prior correct-course proposals.

### Architecture and UX

Section: Architecture / UX impact

OLD:

```markdown
Architecture and UX are optional inputs for this change.
```

NEW:

```markdown
No architecture rewrite or UX flow change is required. Preserve current MCP and customization
architecture boundaries:

- Projection resources stay under frontcomposer://<bounded-context>/projections/<projection-name>.
- Skill resources stay under frontcomposer://skills/.
- Schema mismatch blocks side effects before command construction, ULID allocation, lifecycle
  tracking, dispatch, projection query, or render work.
- Customization diagnostics must not weaken MCP tenant/resource visibility gates.
- NFR6 accessibility is reinforced through HFC1050-HFC1055 and development-only sanitized panel
  diagnostics.
```

Rationale: keeps the correction scoped to story/process follow-through.

## 6. Implementation Handoff

Scope classification: Moderate.

Reason: the proposal crosses multiple story artifacts and the sprint workflow ledger, but it does not
change MVP scope, product architecture, or shipped runtime behavior.

Recommended routing:

- Product Owner / Developer: approve the proposal and confirm no duplicate backlog item is desired.
- QA automation maintainer: implement the mechanical changed-file/story-task/File List reconciliation
  guard before story review completion.
- Technical Writer: no immediate doc rewrite unless future verification finds MCP or FC-CUST wording
  drift.
- Architect: preserve the existing FC-CUST precedence and MCP non-goal boundaries in future story
  creation.

Success criteria:

- Administrator approves or revises this proposal.
- Sprint ledger either updates the existing Epic 8 reconciliation action text or creates a single
  non-duplicative cross-epic QA automation follow-up.
- Future story review cannot mark File List reconciliation complete without a generated changed-file
  comparison against the story File List.
- No MCP URI, schema-fingerprint, fail-closed security, package, public API, pact, generated-output,
  or submodule contract changes occur as part of this correction.

## 7. Approval Record

Decision: approved by Administrator on 2026-07-01.

Routed action: QA automation maintainer owns the mechanical changed-file, story-task, and File List
reconciliation guard before story review completion.
