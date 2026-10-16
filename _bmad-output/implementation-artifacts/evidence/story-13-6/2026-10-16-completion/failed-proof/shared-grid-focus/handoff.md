# Shared grid focus correction

Settled verification: **24 passed, 0 failed, 0 skipped (42.8s)** across Chromium, Firefox and WebKit. All eight previously failing module-navigation/data-formatting cycles and their Chromium controls pass, along with the current type cycles and themed focus/contrast regressions.

`fc-projection.css` now owns the2px inset keyboard contour for generated `.fc-datagrid-host` regions and their actual grid-scoped diagnostic links (`.fc-projection-grid td .fc-field-placeholder a`). Normal mode uses the existing `--colorStrokeFocus2` token; supported forced-color mode uses CanvasText. This changes focus paint without changing content, layout boxes or default density. The type specimen retains only the color adaptation required by its independent local theme, alongside its already-verified narrow navigation spacing.

Actual DOM/computed-style evidence in `original-grid-owners.json` shows the generator's row-count span immediately preceding its focusable region. Native Firefox1px/WebKit3px external outlines crossed into that text. The intermediate run resolved those eight original failures, then retained four fixture modality failures and a later real WebKit diagnostic-link overlap. The fixture now establishes real keyboard modality after selecting the actual Fluent Light/Dark theme; the diagnostic link gets the same owned inset contour. No measurement assertion was weakened.

The existing two themed tests now also use a real Counter projection grid outside the type specimen, select actual Light/Dark through the Fluent theme menu, verify actual theme paint, and recheck full focus geometry and3:1 contrast in normal/forced-color modes. WebKit's unsupported actual forced-palette replacement remains explicitly attributed; Windows/platform/manual gates remain open.

Exact settled command, from repository root:

```sh
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/story13-6-shared-grid-settled FC_E2E_HTML_REPORT_DIR=/tmp/story13-6-shared-grid-settled-report npm --prefix tests/e2e test -- specs/responsive-accessibility.spec.ts --grep 'module navigation and tabs: 320|data-formatting: 320|type: 320|narrow specimen navigation' --project=chromium --project=firefox --project=webkit --workers=3
npm --prefix tests/e2e run typecheck
git diff --check
```

Exact final log/JUnit/self-contained HTML report are beside this file. The intermediate19passed/5failed result and its contexts/screenshots/traces are retained separately. Parent's prior353passed/8failed/2skipped full run remains retained under `build-review/failed-proof/parent-third`; this packet does not relabel it passing.

Source is frozen for the parent-owned full verification and candidate refresh. No temporary executable spec or child-owned host remains. Parent Test host5070/session61380 remains running. Overall Story13.6 and existing visual/configured Shell/manual/platform gates remain open.
