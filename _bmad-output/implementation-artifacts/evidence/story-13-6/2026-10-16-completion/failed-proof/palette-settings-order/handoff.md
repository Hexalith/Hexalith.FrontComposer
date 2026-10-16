# Narrow correction handoff

The supported-clip, screenshot-scroll and generated-grid setup corrections passed their focused checks. A bounded diagnostic probe then reproduced the palette-to-Settings failure and identified a separate, redundant Fluent focus return. The per-dialog correction passed the actual delayed-vendor-callback regression and live journeys in Chromium, Firefox and WebKit.

Changed source:

- `tests/e2e/helpers/responsive-accessibility.ts`: resolve supported rectangular legacy/inset clips, exclude only proven fully hidden paint, measure visible Fluent pseudo-border underlines within their supported clip, and fail closed on unsupported clips/masks. Preserve all saved document/open-shadow scroll offsets around diagnostic screenshots.
- `tests/e2e/specs/accessibility-state-matrix.spec.ts`: use scroll-preserving screenshots and wait for a real rendered grid followed by the actual two fixture rows before entered recovery capture/removal. The parent timeout showed virtual spacer-only grid content below the viewport; no reducer overwrite was observed.
- `tests/e2e/specs/responsive-accessibility.spec.ts`: focus-only shell-scroll fixture; supported-clip real/synthetic positives; retained G04/G06 negatives; screenshot restoration negative; deterministic actual Fluent delayed-return regression.
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js`: clear the pinned Fluent `previousActiveElement` slot on closed `beforetoggle` only for palette/settings dialogs whose guarded return the shell owns. Native modal behavior and other Fluent dialog slots retain their existing paths.

The failed probe timeline is conclusive: application restore entry 5940.0ms; application callback 5955.3ms with `userMovedFocus=false` and body active; real Tab reaches Settings 5964.4ms; Fluent library timer focuses the palette trigger 5974.4ms; Enter/click target palette trigger 5979ms, reopening it. There was one application restore and no replacement reservation during this interval. The original uninstrumented failure, actual reproduced trace, exact focus stacks/native focus events and vendor excerpt are retained. The first probe's separate pre-dismissal empty selected-cue inventory failure remains unattributed; its assertion is unchanged.

Commands (from repository root):

```sh
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/story13-6-clip-owner-settled FC_E2E_HTML_REPORT_DIR=/tmp/story13-6-clip-owner-settled-report npm --prefix tests/e2e test -- specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --grep 'G04|G06|supported clipped paint|diagnostic screenshots|shell focus scrolling|delayed generated detail|active toolbar|all eleven actual lifecycle|projection reconnect' --project=chromium --project=firefox --project=webkit --workers=3
# 39 passed (1.3m), before the later runtime correction.

PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/story13-6-real-clip-positive-final FC_E2E_HTML_REPORT_DIR=/tmp/story13-6-real-clip-positive-final-report npm --prefix tests/e2e test -- specs/responsive-accessibility.spec.ts --grep 'real Fluent search' --project=chromium --project=firefox --project=webkit --workers=3
# 3 passed (3.7s), before the later runtime correction.

PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/story13-6-vendor-return-fixed FC_E2E_HTML_REPORT_DIR=/tmp/story13-6-vendor-return-fixed-report npm --prefix tests/e2e test -- specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --grep 'owned palette return|delayed overlay disposal|active toolbar' --project=chromium --project=firefox --project=webkit --workers=3
# 12 passed (21.7s), after parent rebuilt Counter.Web/Shell.Tests and restarted its Test host.

npm --prefix tests/e2e run typecheck
git diff --check
# Both passed.
```

Instrumented bounded commands used only `specs/review-focus-order.temp.spec.ts --grep 'active toolbar' --project=chromium --repeat-each=8 --workers=3`, with `/tmp/story13-6-focus-order` and `/tmp/story13-6-focus-order-callback` output/report/log prefixes. The source instrumentation is retained as text; the temporary executable spec was removed.

Parent owns the final candidate manifest and full affected-spec rerun. Prior Shell/visual/manual/platform gates remain open; this packet makes no overall Story 13.6 completion claim. The parent-owned Test host on5070/session69107 remains running; no child host was started.
