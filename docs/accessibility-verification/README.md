# Accessibility Verification

This folder holds release-branch evidence for FrontComposer accessibility and visual specimen verification.

The CI gate proves the committed generated and Shell specimen surfaces only:

- `/__frontcomposer/specimens/type`
- `/__frontcomposer/specimens/data-formatting`

Arbitrary adopter-provided custom components remain governed by the custom-component accessibility contract: accessible names, keyboard reachability, visible focus, state announcement, reduced-motion support, and forced-colors support.

## Required Release Log

Create one dated log per release branch or package-promotion candidate. Do not record pass results for audits that were not performed.

### Core identity fields (always required)

- Release branch or tag
- Commit or immutable artifact reference
- Stable gate id
- Task and acceptance-criteria ids
- Canonical gate status: `completed`, `not performed`, `blocked`, `accepted v1 constraint`, or `post-v1 roadmap`
- Reviewer or sign-off owner

### Per-status required fields

Use the per-status matrix to determine which additional fields are required for each row. The full matrix lives in [`manual-log-template.md`](./manual-log-template.md). Summary:

- `completed` requires Date; Tester; OS; Browser and version; Screen reader and version; Specimen route or flow; UX baseline or responsive tier; Result (Pass / Fail); Issue links (when Result=Fail); Approval reference; Sanitization status; Evidence attachment paths or links. The tester for a manual AT or device `completed` row must be a human; AI-agent identifiers are valid as `Prepared by` on a pack but never as the tester of a manual `completed` row.
- `not performed` requires Owner; Release impact; Reason; Next action; Reopen event or revalidation trigger.
- `blocked` requires Owner; Release impact; Blocker ref; Decision needed; Reopen event or revalidation trigger.
- `accepted v1 constraint` requires Owner; Release impact; Downstream consumer impact; Adopter communication need; Evidence reference; Expiry or revalidation trigger; Reopen event; Approval references for the four canonical roles (Product, Quality/Test, Accessibility/Stakeholder, Release Owner).
- `post-v1 roadmap` requires Owner; Story or roadmap reference; Target release or non-planning rationale; Release impact; Reopen event.

Sanitization/redaction status applies to every evidence path or retained artifact, regardless of canonical status.

### Release classification summary

Each release-candidate evidence pack includes a release classification summary: `ready`, `blocked`, or `ready-with-accepted-constraints` (see "Release Evidence Packs" below).

### Minimum manual matrix before release/package promotion

- NVDA with Firefox
- JAWS with Chrome
- VoiceOver with Safari

Tablet and phone fallback checks are also release-candidate gates. Classify tablet against the touch-adapted UX tier. Classify phone against the functional-fallback commitment, not full daily-use design support.

Manual assistive-technology, tablet, and phone gates are complete only when dated manual evidence exists. Automated axe, keyboard, focus, forced-colors, reduced-motion, zoom/reflow, visual baseline, and specimen-manifest evidence can support release decisions, but must not be used as a substitute for manual screen-reader or real-device completion.

## Release Evidence Packs

Release evidence packs should include one canonical row per gate. If the same gate appears in multiple tables in the same pack, the Broader Accessibility Classification table is canonical; supplemental tables (such as the Post-v1 Roadmap Register) must not contradict the canonical status. If the same gate appears across multiple pack files, the most recent pack with a matching release-candidate identity is canonical, and any contradiction blocks the release classification until resolved.

Each pack must include:

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

### Per-row `post-v1 roadmap` rule

Per-row `post-v1 roadmap` status is provisional. AC23 is satisfied at pack level whenever the overall `final_classification` is `blocked`. When a future pack proposes `ready` or `ready-with-accepted-constraints`, each ROADMAP-* row must obtain Product, Accessibility/Stakeholder, and Release Owner approval references before being claimed as roadmap rather than `blocked`. Approval references are recorded in the Post-v1 Roadmap Register.

### Final classification

Final classification is fail-closed:

- `ready` requires every required gate to be `completed` with sanitized evidence and stakeholder sign-off for all four canonical roles (Product, Quality/Test, Release Owner, Accessibility/Stakeholder).
- `ready-with-accepted-constraints` requires every incomplete gate to be either an approved `accepted v1 constraint` (with the four-role approval references per `accepted v1 constraint` rules above) or a named `post-v1 roadmap` item with the per-row roadmap approval references.
- `blocked` is required when a required gate is `not performed`, `blocked`, missing required fields, missing an owner, missing sanitization proof, or missing required stakeholder approval.

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

## Named CI Evidence Follow-Ups

When local browser execution is blocked, the story or retrospective must name the exact command, local
result, blocker timing, fallback evidence, CI owner, CI lane, and artifact path expected to close the
evidence gap. A generic "Playwright is socket-blocked locally" note is not enough for promotion when the
story changes visual, hover/focus, touch, or screenshot-baseline behavior. If browser assertions did not
run, local typecheck, bUnit, or direct xUnit evidence is fallback evidence only; CI remains authoritative
for the browser/a11y/visual gate.

| ID | Source | Lane | Owner | Closure evidence |
| --- | --- | --- | --- | --- |
| E8-CI-1 | Story 8.1 review | Windows visual baseline update lane | QA Engineer | Updated win32 visual baselines or explicit non-update decision. |
| E8-CI-2 | Story 8.3 review | Shell chrome Playwright lane | QA Engineer | Browser assertion for custom-logo non-decorative branch or documented supersession by bUnit pin. |
| E8-CI-3 | Story 8.7 review | Status icon tooltip/touch browser lane | QA Engineer | Playwright browser run with the hasTouch-scoped test passing. |
