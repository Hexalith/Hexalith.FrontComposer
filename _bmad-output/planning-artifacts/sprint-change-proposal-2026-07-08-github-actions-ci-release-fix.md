---
title: Sprint Change Proposal - GitHub Actions CI and Release Fix
status: approved-implemented
created: 2026-07-08
owner: Administrator
approvedBy: Administrator
approvedAt: 2026-07-08
scope: minor
trigger:
  - https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/28940462528/job/85861112215
  - https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/28940462519
---

# Sprint Change Proposal: GitHub Actions CI and Release Fix

## 1. Issue Summary

The 2026-07-08 GitHub Actions runs for commit `238aaa37c1ef56a95437d3b522026394017ddc85` failed in:

- CI `build-and-test`, Gate 3a default lane.
- CI `accessibility-visual`, Playwright specimen gate.
- Release `Run release tests`.

Evidence:

- `IdeParityMatrixContractTests.MatrixJson_HasFailClosedSchemaForEveryRow` expected `.NET SDK 10.0.302` while `global.json`, matrix metadata, and CI resolved `10.0.302`.
- The specimen visual hook test could not find `.fc-projection-connection-status-host ... .fc-projection-connection-status-pulse` in the browser CSSOM.
- The visual baseline failures were downstream of the specimen host not directly loading the Shell scoped CSS bundle.

## 2. Impact Analysis

Epic impact:

- Epic 7 authoring tooling and drift safety: affected through stale IDE parity SDK evidence.
- Epic 11 visual-conformance guards: affected through the Counter specimen host loading the wrong Shell scoped CSS asset path.

Story impact:

- No new story, epic, or PRD scope is required.
- This is a direct adjustment to CI references, parity evidence, and the specimen host.

Artifact conflicts:

- PRD: no requirement change.
- Epics: no sequencing or acceptance criteria change.
- Architecture/UX: no design contract change; the fix preserves the existing Fluent/scoped CSS visual-conformance guard.
- CI/CD: update workflow SDK references from `10.0.302` to `10.0.302`.

Technical impact:

- Release and CI lanes now agree with the repository SDK pin.
- The Counter specimen host directly serves `_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.bundle.scp.css`, matching the app-host convention used by `Hexalith.FrontComposer.UI`.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- The failures are configuration/fixture drift, not product-scope drift.
- No rollback or MVP review is justified.
- The changes are low risk and directly verifiable with the failed CI commands.

Effort: Low.
Risk: Low.
Timeline impact: none beyond rerunning CI and release.

## 4. Detailed Change Proposals

### CI and IDE Parity SDK References

Files:

- `.github/workflows/ci.yml`
- `.github/workflows/ide-parity-revalidation.yml`
- `.github/workflows/mutation-property-nightly.yml`
- `.github/workflows/nightly.yml`
- `.github/workflows/quarantine-governance-nightly.yml`
- `artifacts/ide-parity/evidence/*.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`

OLD:

```text
10.0.302
```

NEW:

```text
10.0.302
```

Rationale: align workflows, parity evidence, and the fail-closed matrix test with `global.json`.

### Counter Specimen Shell Scoped CSS

File: `samples/Counter/Counter.Web/Components/App.razor`

OLD:

```html
<link href="_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.styles.css" rel="stylesheet" />
```

NEW:

```html
<link href="_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.bundle.scp.css" rel="stylesheet" />
```

Rationale: consuming apps expose dependency scoped CSS through the `.bundle.scp.css` asset. Loading it directly makes Shell scoped CSS rules visible to the browser CSSOM guard and stabilizes the visual baseline tests.

## 5. Checklist Summary

- [x] 1.1 Trigger identified: GitHub Actions CI and Release failures from the linked runs.
- [x] 1.2 Core problem defined: SDK reference drift and stale/missing direct Shell CSS asset path.
- [x] 1.3 Evidence gathered: GitHub logs, local static web asset manifest, served CSS, Playwright reproduction.
- [x] 2.1-2.5 Epic impact assessed: minor direct fix; no epic resequencing.
- [x] 3.1 PRD conflict checked: none.
- [x] 3.2 Architecture conflict checked: none; fix preserves scoped CSS/visual guard architecture.
- [x] 3.3 UX conflict checked: none; visual guard remains intact.
- [x] 3.4 Secondary artifacts checked: CI workflows and parity evidence updated.
- [x] 4.1 Direct adjustment selected.
- [N/A] 4.2 Rollback rejected.
- [N/A] 4.3 MVP review rejected.
- [x] 5.1-5.5 Proposal and handoff recorded.
- [x] 6.1-6.5 Verification and handoff complete; no sprint-status epic/story changes required.

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation.

Approval: Administrator approved the proposal on 2026-07-08.

Implementation tasks:

- Update SDK references to `10.0.302`.
- Update the Counter specimen host Shell scoped CSS link.
- Rebuild and rerun the failed lanes.

Success criteria:

- SourceTools IDE parity matrix test passes.
- Full SourceTools test project passes.
- Filtered solution default lane passes.
- `npm run test:a11y` passes against the Release specimen host.
- No `10.0.302` or stale `Hexalith.FrontComposer.Shell.styles.css` references remain in governed repo surfaces.

Verification completed locally:

- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName=Hexalith.FrontComposer.SourceTools.Tests.IdeParity.IdeParityMatrixContractTests.MatrixJson_HasFailClosedSchemaForEveryRow"`
- `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj --configuration Release`
- `PLAYWRIGHT_SKIP_WEBSERVER=1 BASE_URL=http://127.0.0.1:5070 npx playwright test specs/specimen-accessibility.spec.ts --project=chromium --grep "story 11.5 scoped Fluent-root visual hooks are reachable"`
- `PLAYWRIGHT_SKIP_WEBSERVER=1 BASE_URL=http://127.0.0.1:5070 npx playwright test specs/specimen-accessibility.spec.ts --project=chromium --grep "visual baseline"`
- `PLAYWRIGHT_SKIP_WEBSERVER=1 BASE_URL=http://127.0.0.1:5070 npm run test:a11y`
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
