# Accessibility Verification

This folder holds release-branch evidence for FrontComposer accessibility and visual specimen verification.

The CI gate proves the committed generated and Shell specimen surfaces only:

- `/__frontcomposer/specimens/type`
- `/__frontcomposer/specimens/data-formatting`

Arbitrary adopter-provided custom components remain governed by the custom-component accessibility contract: accessible names, keyboard reachability, visible focus, state announcement, reduced-motion support, and forced-colors support.

## Required Release Log

Create one dated log per release branch or package-promotion candidate. Do not record pass results for audits that were not performed.

Required fields:

- Release branch or tag
- Date
- Tester
- Operating system
- Browser and version
- Screen reader and version
- Specimen route
- Pass or fail
- Issue links
- Resolution status
- Reviewer or sign-off owner
- Evidence attachment paths or links

Minimum manual matrix before release/package promotion:

- NVDA with Firefox
- JAWS with Chrome
- VoiceOver with Safari

## Automated Evidence

Playwright keeps the HTML report, JUnit output, screenshots, visual diffs, traces/videos on failure, and bounded axe summaries under `tests/e2e/playwright-report/` and `tests/e2e/test-results/`.

Axe summaries include route, rule id, impact, help URL, bounded selectors, and truncation markers. They must not include full DOM dumps, cookies, tokens, local machine paths, or environment secrets.

## Temporary Axe Suppressions

Suppressions must be scoped. Blanket page-level exclusions or broad rule disables are not allowed.

Each suppression must name:

- Specimen route
- WCAG or axe rule id
- Selector
- Rationale
- Owner
- Expiry or review date
- Linked story or issue
- Evidence that the issue is third-party or intentionally deferred

## Visual Baseline Changes

Use `npm run test:e2e:visual:update` locally to intentionally update the six v1 baselines: Light/Dark x Compact/Comfortable/Roomy.

When baselines change, include a rationale paragraph plus before/after screenshot artifacts or links. CI never regenerates baselines, suppressions, or specimen manifest entries automatically.
