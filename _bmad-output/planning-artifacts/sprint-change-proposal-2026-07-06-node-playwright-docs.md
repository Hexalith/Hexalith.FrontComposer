---
title: Sprint Change Proposal - Node and Playwright setup documentation
date: 2026-07-06
status: implemented
scope: minor
mode: batch
trigger: local e2e setup documentation lagged behind the current Node/npm and Playwright browser requirements
---

# Sprint Change Proposal - Node and Playwright setup documentation

## 1. Issue Summary

Local setup exposed a documentation gap: the Playwright e2e workspace declares `engines.node >=24.0.0`, `tests/e2e/.nvmrc` pins Node `24`, and `@faker-js/faker` requires npm `>=10`, but the generated development guide still described Node as generic LTS. The same pass found stale SDK references to `10.0.300` even though `global.json` pins `10.0.301`.

Evidence:

- `tests/e2e/package.json` declares `engines.node >=24.0.0`.
- `tests/e2e/.nvmrc` contains `24`.
- `global.json` pins SDK `10.0.301`.
- CI installs Chromium only for the accessibility/visual lane, while the Playwright config also declares Firefox and WebKit projects for local cross-browser runs.

## 2. Impact Analysis

Epic impact:

- Epic 7 and Epic 10 documentation/tooling-governance expectations are affected because setup instructions must match the actual CLI/e2e tooling surface.
- PRD FR-23 is affected because component, diagnostic, migration, and tooling docs must stay synchronized with runtime and test surfaces.
- No product scope, MVP behavior, architecture boundary, UX flow, or public API changes are required.

Story impact:

- Story 10.2 remains aligned with adopter-facing cleanup: stale setup labels and commands should not mislead contributors.
- No new story is required because this is a minor documentation correction with no backlog restructure.

Artifact conflicts:

- `_bmad-output/project-docs/development-guide.md` had stale Node and SDK setup instructions.
- `_bmad-output/project-docs/index.md`, `project-overview.md`, `source-tree-analysis.md`, and `project-scan-report.json` had stale SDK references.
- `_bmad-output/project-context.md` had a stale Playwright version and incomplete Node/npm/e2e install guidance.
- `docs/ide-parity-matrix.md`, `docs/ide-parity-matrix.json`, and `docs/hot-reload-guide.md` had stale SDK references.
- `docs/validation/producer-fingerprints.json` needed the `docs/ide-parity-matrix.md` producer hash updated after the intentional SDK-pin edit.

Technical impact:

- Documentation-only. No source, package, CI, or generated output behavior changes.
- No rollback required.
- No sprint-status epic/story changes required.

## 3. Recommended Approach

Use Direct Adjustment.

Rationale:

- The repository already encodes the real toolchain requirements in `global.json`, `tests/e2e/.nvmrc`, and `tests/e2e/package.json`.
- The correction is small, localized, and low-risk.
- Updating the docs prevents repeat local engine warnings and clarifies why CI installs only Chromium.

Effort: Low.
Risk: Low.
Timeline impact: None beyond the documentation patch and validation.

## 4. Detailed Change Proposals

### Project Context

Artifact: `_bmad-output/project-context.md`

OLD:

```text
E2E: Playwright 1.61.0, TypeScript 6.0.3, Node engine >=24.0.0
```

NEW:

```text
E2E: Playwright 1.61.1, TypeScript 6.0.3, Node engine >=24.0.0,
npm >=10; tests/e2e/.nvmrc pins Node 24
```

Rationale: Aligns project context with `tests/e2e/package.json`, lockfile-installed Playwright, and npm engine requirements.

### Development Guide

Artifact: `_bmad-output/project-docs/development-guide.md`

OLD:

```text
.NET SDK: 10.0.300
Node.js: LTS
```

NEW:

```text
.NET SDK: 10.0.301
Node.js: >=24.0.0 for the Playwright e2e workspace; tests/e2e/.nvmrc pins Node 24
npm: >=10 for the e2e workspace dependencies
```

Rationale: Removes the generic LTS instruction that caused local setup ambiguity.

OLD:

```bash
cd tests/e2e
npm ci
npx playwright install --with-deps chromium
npm run typecheck
npm run test:a11y
```

NEW:

```bash
cd tests/e2e
nvm use
npm ci
npx playwright install --with-deps chromium
npm run typecheck
npm run test:a11y
```

Rationale: Keeps the CI lane Chromium-only while documenting how to activate the Node pin locally.

Additional note added: Firefox/WebKit browser payloads are optional for local cross-browser checks and are installed by `npm run install:browsers` from `tests/e2e` or `npm run test:e2e:install` from the repository root.

### SDK Pin References

Artifacts:

- `_bmad-output/project-docs/index.md`
- `_bmad-output/project-docs/project-overview.md`
- `_bmad-output/project-docs/source-tree-analysis.md`
- `_bmad-output/project-docs/project-scan-report.json`
- `docs/ide-parity-matrix.md`
- `docs/ide-parity-matrix.json`
- `docs/hot-reload-guide.md`
- `docs/validation/producer-fingerprints.json`

OLD:

```text
10.0.300
```

NEW:

```text
10.0.301
```

Rationale: `global.json` is authoritative and pins `10.0.301`.

The producer fingerprint baseline for `docs/ide-parity-matrix.md` was updated because the documentation gate treats that file as a story-owned producer artifact.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation.

Implementation tasks:

- Update BMad generated project docs and agent context to reflect Node `>=24`, npm `>=10`, Playwright `1.61.1`, and CI Chromium-only browser install.
- Update stale SDK pin references from `10.0.300` to `10.0.301` where the document is intended to describe the current repository baseline.
- Update the producer fingerprint baseline for any intentionally changed producer artifact.
- Leave historical review evidence unchanged when it is describing the state observed by that review.
- Validate with targeted text search, JSON parsing, package installs, and Playwright launch smoke checks.

Success criteria:

- No active setup guide still presents Node as generic LTS for the e2e workspace.
- No current-baseline doc still claims SDK `10.0.300`.
- Documentation clearly distinguishes the Chromium-only CI accessibility/visual lane from optional local all-browser installs.
- `pwsh ./eng/validate-docs.ps1` passes.
- Repository status contains only intentional documentation changes and ignored npm/Playwright output remains ignored.

## Checklist Summary

- [x] 1.1 Triggering issue identified: local e2e setup documentation was stale after toolchain installation.
- [x] 1.2 Core problem defined: documentation mismatch, not code or CI behavior.
- [x] 1.3 Evidence collected from `global.json`, `tests/e2e/.nvmrc`, `tests/e2e/package.json`, and CI.
- [x] 2.1 Current epic can continue as planned.
- [x] 2.2 No epic-level scope change required.
- [x] 2.3 Remaining epics unaffected.
- [x] 2.4 No new epic required.
- [x] 2.5 No epic priority change required.
- [x] 3.1 PRD impact identified: FR-23 documentation synchronization only.
- [N/A] 3.2 Architecture impact: no architecture behavior changes.
- [N/A] 3.3 UX impact: no user-facing UX behavior changes.
- [x] 3.4 Documentation impact identified and patched.
- [x] 4.1 Direct Adjustment selected as viable.
- [N/A] 4.2 Rollback not viable or necessary.
- [N/A] 4.3 MVP review not required.
- [x] 4.4 Recommended path selected.
- [x] 5.1 Issue summary created.
- [x] 5.2 Artifact adjustment needs documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact documented as none.
- [x] 5.5 Handoff plan documented.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal checked for consistency.
- [x] 6.3 User approval inferred from direct request to update documentation.
- [N/A] 6.4 Sprint status unchanged because no epics or stories were added, removed, or renumbered.
- [x] 6.5 Next steps and validation criteria documented.
