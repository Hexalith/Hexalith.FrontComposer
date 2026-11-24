---
title: 'DW-1962 Scope e2e Counter heading checks to the route heading'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Replace the remaining page-wide `Counter` heading locators in the e2e specs with the shared `CounterPage.heading` locator, the main-scoped exact level-1 heading, so the checks cannot pass on a different heading.

</frozen-after-approval>

## Implementation Notes

- Replaced all 14 `page.getByRole('heading', { name: 'Counter' })` uses across nine specs with `new CounterPage(page).heading`, the main-scoped exact level-one locator. Review found three more main-scoped but non-exact checks in `route-contract.spec.ts` and `page-toolbar.spec.ts`; they now use the same locator.
- `smoke.spec.ts` now asserts that `/counter` has exactly one level-one heading. The old page-wide locator gave that protection against a second `Counter` heading only by accident.
- Validation: `npm run typecheck` passes. The 12 touched specs pass on Chromium against the Release Counter host (55 tests). `override-accessibility-safety-diagnostics.spec.ts` passes (2 tests) against private Development and Test Debug hosts on ports 5094/5095, because a stale process from another session holds its default port 5084.

## Review Triage Log

- medium, verified — the override spec's edited assertion had not run; it passes against private hosts on free ports.
- low, rejected — consolidating the eight `gotoCounter` copies into `CounterPage.goto()` is a refactor, not a defect.
- medium, patched — three main-scoped Counter heading checks without `exact: true` still matched `Configure Counter` as a substring; they now use `CounterPage.heading`.
- low, rejected — a locator-shape lint guard is new tooling with little payoff after this cleanup.
- medium, patched — the main-scoped locator no longer catches a second `Counter` level-one heading in the banner; `smoke.spec.ts` now asserts a single page-wide level-one heading.
- low, rejected — the locator change does not depend on the browser engine, and CI runs the suite on Chromium, Firefox and WebKit.
- low, patched after commit — the edit rewrote the working copies with LF against the `eol=crlf` policy; they were restored to CRLF. Committed content is unaffected because the index stores LF either way.
