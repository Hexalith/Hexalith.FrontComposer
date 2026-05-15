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

Story 12.5 release-certification logs must also record:

- Stable gate id
- Task and acceptance-criteria ids
- Canonical gate status: `completed`, `not performed`, `blocked`, `accepted v1 constraint`, or `post-v1 roadmap`
- Release impact and owner for any incomplete gate
- Reopen event or revalidation trigger
- Approval reference for completed or accepted gates
- Sanitization/redaction status for every evidence path or retained artifact
- Release classification summary: `ready`, `blocked`, or `ready-with-accepted-constraints`

Minimum manual matrix before release/package promotion:

- NVDA with Firefox
- JAWS with Chrome
- VoiceOver with Safari

Tablet and phone fallback checks are also release-candidate gates. Classify tablet against the touch-adapted UX tier. Classify phone against the functional-fallback commitment, not full daily-use design support.

Manual assistive-technology, tablet, and phone gates are complete only when dated manual evidence exists. Automated axe, keyboard, focus, forced-colors, reduced-motion, zoom/reflow, visual baseline, and specimen-manifest evidence can support release decisions, but must not be used as a substitute for manual screen-reader or real-device completion.

## Release Evidence Packs

Release evidence packs should include one canonical row per gate. If the same gate appears in multiple tables or notes, the stable gate id decides which row is canonical, and any contradiction blocks the release classification until resolved.

Each pack should include:

- Current evidence inventory before template or pack edits
- Manual screen-reader matrix
- Real-device matrix
- Broader accessibility classification for cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion scope
- Accepted constraints register
- Post-v1 roadmap register
- Stakeholder acceptance records for Product, Quality/Test, Release Owner, and Accessibility/Stakeholder roles
- Adopter communication notes
- Evidence manifest for screenshots, recordings, exported logs, external links, or retained artifacts
- Machine-readable final classification summary

Final classification is fail-closed:

- `ready` requires every required gate to be completed with sanitized evidence and stakeholder sign-off.
- `ready-with-accepted-constraints` requires every incomplete gate to be an approved accepted constraint or named post-v1 roadmap item.
- `blocked` is required when a required gate is not performed, blocked, missing required fields, missing an owner, missing sanitization proof, or missing required stakeholder approval.

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
