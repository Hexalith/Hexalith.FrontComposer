---
title: Preserve native home-card names and localized pending meaning
type: bugfix
created: 2026-10-31
status: in-review
baseline_commit: a9c66bfee79dddcf57664971c1617633cf707919
---

## Intent and Boundaries

Home cards must expose all visible title, count, new-capability and projection-row text through their native accessible name. Replacing that content with an aria-label fails the current axe label-content-name-mismatch gate; moving localized pending meaning solely into aria-description loses that meaning from the computed name. Remove both card-level overrides and append the existing localized pending summary using the existing fc-sr-only utility only for positive aggregate counts. Preserve zero-count presentation, navigation, localization resources, styling and all accessibility rules.

The authorized CI-fix handoff builds on the existing uncommitted card/description change and its two bUnit assertions. No commits, pushes, version/gitlink edits, new runtime fixture behavior or dependency changes belong to this handoff.

## Scope and Acceptance

- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor`: no aria-label/aria-description override; positive counts retain the existing localized pending summary in native name content via fc-sr-only.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Home/FcHomeDirectoryTests.cs`: English/French positive multirow coverage retains localized summary, both row labels/counts and aggregate count; zero counts have no pending summary or naming override.
- `tests/e2e/specs/accessibility-state-matrix.spec.ts`: existing real home-page browser journey requires actual rendered cards and compares each computed name with normalized rendered heading/row text plus any localized pending summary through Playwright toHaveAccessibleName. A fixed Counter sample assertion independently expects `Counter CounterProjection`; existing axe 4.14 rules remain unchanged. Browser evidence reports actual observed counts and does not claim a positive pending fixture unless one is present. Raw DOM textContent is unsuitable because Blazor removes formatting whitespace between semantic blocks.
- This spec records exact focused verification and any blockers. Build Counter.Web and Shell.Tests in Release/package mode, run only FcHomeDirectoryTests, typecheck browser tests under Node 24, then run the existing home-page accessibility journeys.

## Verification

Executed on 2026-10-31 from the FrontComposer root:

```bash
dotnet build samples/Counter/Counter.Web/Counter.Web.csproj --configuration Release -m:1
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Components.Home.FcHomeDirectoryTests
```

Both Release/package builds exited zero with zero warnings/errors. The focused bUnit class passed all 23 tests with no failures/skips, including English/French positive aggregate counts distributed across two projection rows and the zero-count control.

Executed from `tests/e2e` using the existing locked browser workspace, Node 24.22.1, Playwright 1.64.1 and unchanged axe-core/@axe-core/playwright 4.14.0:

```bash
PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH npm run typecheck
PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH CI=true \
FC_E2E_OUTPUT_DIR=/tmp/fc-home-card-browser-verified-results \
FC_E2E_HTML_REPORT_DIR=/tmp/fc-home-card-browser-verified-report \
FC_E2E_JSON_PATH=/tmp/fc-home-card-browser-verified.json \
FC_E2E_JUNIT_PATH=/tmp/fc-home-card-browser-verified.xml \
npm test -- specs/accessibility-state-matrix.spec.ts --project=chromium --grep 'home: composed text, targets, current-state and forced-color cues' --workers=1 --retries=0
```

Typechecking passed. Both real Chromium home journeys passed, with no skips/retries: light and dark themes each observed three actual cards. Computed names were `Counter CounterProjection`, `Default`, and `Specimens SpecimenFormattingProjection SpecimenStatusProjection`. The original axe gate ran in both journeys with zero blocking, report-only or unknown violations; no rule was disabled.

The real home fixture renders zero pending counts. Browser proof therefore covers actual native title/projection names, including the multirow Specimens card; positive localized pending semantics are qualified by the bUnit cases and are not claimed as browser evidence. Blazor strips formatting whitespace from raw DOM textContent, so browser expectations use rendered heading/row text and any localized hidden pending summary, plus the independent fixed Counter name assertion.

Compact facts and tested-worktree file hashes are retained in [home-card-native-name-verification-2026-10-31.json](home-card-native-name-verification-2026-10-31.json) next to this spec, outside evidence folders. Summary SHA-256: `ffa5059291613ab05a4ec93ab47620c94bc0926ba1a16d56b776ca337ae0f3a9`. Temporary detailed build/test/browser logs remain under `/tmp/fc-home-card-*`. `git diff --check` passes. Upstream advanced four commits during the handoff, but none touches the three modified source/test files; no ref, version or gitlink was changed here. Parent owns independent review and integration.
