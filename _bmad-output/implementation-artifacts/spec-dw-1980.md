---
title: 'DW-1980 Name the main landmark on generated command routes'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Generated full-page command routes give their `FcPageHeader` heading a stable id and name the shell `#fc-main-content` landmark from that heading through `FcContentLabel`, so the landmark is announced with the route name instead of the implicit "main".

</frozen-after-approval>

## Implementation Notes

- `CommandPageEmitter` emits `HeadingId="fc-command-heading-{sanitized command FQN}"` on the generated `FcPageHeader`, then an `FcContentLabel` with the same `LabelledBy`. The id reuses `CommandRendererEmitter.SanitizeCssId`, now internal with one merged doc comment, which already builds the stable trigger ids.
- Updated the governed `Page_FiveFields_FullPageBoundarySnapshot` baseline. Added `GeneratedCommandPageNamesTheMainLandmarkFromItsHeading`, a bUnit check that the rendered `h1` id matches the content label, and a live `route-contract.spec.ts` check that `#fc-main-content` references the focused heading id and is named "Configure Counter".
- The shell reference now says generated routes name the landmark once interactive, that a page-level `FcContentLabel` overrides the shell's `ContentLabel`/`ContentLabelledBy`, and how custom routes can do the same.
- Validation: SourceTools emitter and generator-driver tests 80/80; `CommandRendererFullPageTests` 14/14; Shell `Generated` and `Layout` namespaces 809/809 before the review patches; `route-contract.spec.ts` 60/60 on Chromium, Firefox and WebKit; `eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` passed.

## Review Triage Log

- low, patched — `SanitizeCssId` carried two `<summary>` elements; merged into one summary plus remarks.
- low, rejected — moving the id helper to a shared utility is a refactor; reusing the internal helper keeps one id scheme.
- medium, patched — added a bUnit assertion on the rendered page's heading id and content-label reference.
- medium, patched — the e2e check now asserts that `#fc-main-content` has `aria-labelledby` equal to the focused heading's id. Resetting the label on dispose is already covered by the shell content-landmark tests.
- maybe-false, deferred — the landmark name registers after render, so it may arrive after route focus moves. A native screen-reader check of the announced landmark would settle it.
- low, patched — the shell reference now explains that a page-level label takes precedence over the shell parameters.
- low, patched — the shell reference now says the name applies once the page is interactive.
- low, patched — the shell reference now explains how custom routes name the landmark, and says the generated id is not a public contract.
- low, deferred — module landing and aggregate list pages still leave `main` unnamed; this predates the change.
- low, patched — moved the generator assertions into a dedicated underscore-free test, ran the route contract on all three engines, and ran docs validation.
