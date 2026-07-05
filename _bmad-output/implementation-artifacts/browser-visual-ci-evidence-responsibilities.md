# Browser/Visual CI Evidence Responsibilities

Status: active tracking ledger
Owner: QA Engineer
Source: E8-AI-5, Epic 8 retrospective follow-through

This ledger turns local Playwright/Kestrel browser blockers into named CI responsibilities. A generic
"browser lane is blocked locally" note is not enough for future visual, hover/focus, touch, or visual
baseline work when the behavior depends on a real browser.

## Closure Rules

- Each row must name an owner, CI lane or explicit CI-run responsibility, expected artifact path, and
  closure evidence.
- A row can close only with a passing CI run artifact, an approved non-update decision, or an approved
  supersession decision that names the replacement evidence.
- If a future story hits the same gap, cite the row ID in the story evidence instead of creating a new
  anonymous blocker note.
- E8-AI-5 closes when this ledger is active and sprint status routes the rows; the individual E8-CI
  rows remain open until their own closure evidence is recorded.

## Epic 8 Rows

| ID | Source | Owner | Lane / CI responsibility | Expected artifact path | Closure evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| E8-CI-1 | Story 8.1 review follow-up | QA Engineer | Windows visual baseline refresh, then `accessibility-visual` CI job (`tests/e2e` `npm run test:a11y` plus `validate:visual-governance`) | `accessibility-visual-artifacts` artifact with `tests/e2e/playwright-report/**`, `tests/e2e/test-results/**`, and any PR diff under `tests/e2e/specs/*-chromium-win32.png` | Updated six win32 visual baselines with rationale, or explicit non-update decision, plus a passing `accessibility-visual` run. | tracked-open |
| E8-CI-2 | Story 8.3 review follow-up | QA Engineer | Shell chrome browser lane `npm --prefix tests/e2e run test:fc-shell-chrome`; either wire/execute it as a named CI run or record approved supersession | Dedicated shell-chrome Playwright artifact or `accessibility-visual-artifacts` if the lane is added there | Story 8.3 zero-config/default-logo/custom-logo browser assertions pass, or an approved supersession names the bUnit branch pin `FrontComposerShellTests.HeaderLogo_WhenProvided_RendersBetweenHeaderStartAndAppTitle` as sufficient and explains why browser closure is no longer required. | tracked-open |
| E8-CI-3 | Story 8.7 review follow-up | QA Engineer | `accessibility-visual` CI job running `tests/e2e` `npm run test:a11y` | `accessibility-visual-artifacts` artifact with `tests/e2e/test-results/junit.xml`, traces/screenshots/videos on failure, and `tests/e2e/playwright-report/**` | The status-icon focus/hover/touch test in `specimen-accessibility.spec.ts` passes in a protected CI run, or an approved non-update decision replaces it with equivalent browser evidence. | tracked-open |

## Future Story Evidence Template

```md
Browser/visual CI responsibility:
- Row ID: <E8-CI-* or new row id>
- Local blocker: <exact command + blocker, or N/A>
- CI lane: <workflow job + script/command>
- Owner: <role/person>
- Expected artifact: <artifact name/path>
- Closure evidence: <run id/artifact path, explicit non-update decision, or approved supersession>
```
