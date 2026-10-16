# Twelve Firefox/WebKit visual failures: causal diagnosis

These are failed automated comparisons. No baseline image, screenshot threshold, Fluent typography mapping, density mapping or product styling was changed during this continuation.

The initial focused ordinary comparison ran all 18 existing cases: **6 passed / 12 failed**, with no retries or snapshot updating. The completed pre-cleanup three-spec run independently reproduced all twelve failures. Exact commands, JUnit, pixel counts and all 36 expected/actual/diff files are retained under [pre-cleanup](pre-cleanup/visual-verification.json), [comparison inventory](pre-cleanup/visual-comparisons/inventory.json) and [per-comparison measurements](pre-cleanup/visual-failure-matrix.json). Their source candidate is `sha256:5085a3fd41df4dbd1d358e9eda14fc2796c0c0c578f09a96d3b677f82f776918`; later verification binds the final candidate separately.

## Fonts and loading

The actual Fluent font stack is `'Segoe UI', 'Segoe UI Web (West European)', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', sans-serif`. None of its font-family tokens was changed. Local browser measurements show different resolved system faces despite the same computed stack: Chromium's 16px sample width is **398.140625px**, matching Liberation Sans/Arial; Firefox's **420.033325px** and WebKit's **420.0008544921875px** match Noto Sans. The probe uses the entire computed Fluent stack; checking one unavailable quoted family alone would incorrectly measure a serif fallback.

Every engine had `document.fonts.status = loaded`, an empty web-font face set, zero font requests, zero failed requests and zero page errors. Two successive font-ready/layout-stable full-page captures were byte-identical in each engine. This rules out an unfinished web-font download or screenshot-before-font-ready race for the reproduced failure. Measurements and screenshots are in [the current environment probe](font-probes/current-final/measurements.json); [the executable probe](diagnose-visual.mjs) reproduces canonical visual preparation without editing production files.

An **isolated Fontconfig process experiment**, using the already-installed DejaVu Sans, changed no repository fonts or CSS. It reproduced the unchanged route-heading region **exactly in all twelve baseline images** (decoded RGB equality, 1184×52 region at x76/y124). The Noto rendering matched none. [All twelve region comparisons](font-region-comparisons.json), [DejaVu light measurements](font-probes/dejavu-final/measurements.json), [dark measurements](font-probes/dejavu-dark/measurements.json) and [temporary configuration](font-probes/dejavu-fonts.conf) retain that proof. This establishes a system-font mismatch rather than a speculative antialiasing explanation. It does not make any whole-image comparison pass.

The local host is Ubuntu 26.04, Fontconfig 2.17.1, FreeType 2.14.2, GTK3 3.24.52 and GTK4 4.22.4. [Exact package/runtime inventory](font-environment.txt) is retained. Baseline provenance names hosted Ubuntu Quality run `36836271385` and commit `87e33b06ad8d4b2d9d2b99874823567da8757f7d`; both that commit and this checkout pin Playwright **1.63.0**. No dependency upgrade explains this mismatch. The hosted OS/font/GTK/FreeType inventory is unavailable in the repository, so this diagnosis does not invent its versions or attribute remaining pixels to a particular rasterizer release.

## Layout and native rendering

Font alignment alone leaves structural differences. Current source after the baseline capture includes the approved Story 13.6 pointer-target floor, inherited `overflow-wrap:anywhere`, bounded/padded grid scrollports, inline diagnostic-link treatment and generated-grid sizing changes. [Exact source differences from the baseline capture](baseline-layout-source.diff) identify them. The baseline rationale explicitly records that the intentional 2026-10-15 update changed **only Chromium Linux** images; Firefox/WebKit images still contain the earlier layout.

A diagnostic page-only reversal of the two identified shell-wrap/specimen-target deltas reduced DejaVu Light Compact page height from **3845 to 3800** in Firefox and **3886 to 3840** in WebKit; retained expectations are **3778** and **3818**. [Counterfactual measurements](font-probes/historical-layout/measurements.json) and their screenshots are experiments, never canonical passing evidence. No rollback was applied to source. They isolate layout as a second cause and show that neither a font-only change nor an isolated two-rule reversal reconstructs the old full page.

Native controls also differ across engines in the measured current environment: the density sample button uses Firefox's native `sans-serif`, 13.3281px with 1px/4px padding, versus WebKit's `-webkit-standard`, 16px with 2px/6px/3px padding. Browser-specific baselines already exist for this reason. The ordinary visual route uses the existing custom Counter template; its measured nested row must not be confused with `generatedGrid=true`, whose separate executable regression asserts 32px rows and matching ItemSize, spacing growth, density switches and restoration. No CSS workaround, font override, browser-specific product branch or relaxed comparison was introduced to force an old bitmap match.

Both themes reproduce each density's height discrepancy; the full per-case pixel counts remain in the machine-readable matrix:

| Engine | Density | Expected height | Actual height | Themes affected |
|---|---|---:|---:|---|
| Firefox | Compact | 3778 | 3820 | Light, Dark |
| Firefox | Comfortable | 3846 | 3880 | Light, Dark |
| Firefox | Roomy | 3914 | 3940 | Light, Dark |
| WebKit | Compact | 3818 | 3863 | Light, Dark |
| WebKit | Comfortable | 3886 | 3923 | Light, Dark |
| WebKit | Roomy | 3954 | 3983 | Light, Dark |

## Gate disposition

The loading-race and browser-package-upgrade hypotheses are rejected by the executed evidence. A reproducible font-resolution mismatch and baseline-versus-current layout changes are established. This is **not** proof that every remaining pixel difference is approved: the twelve complete comparisons stay failed pending visual-owner reconciliation on the hosted environment. Existing assertions remain authoritative.

QA Engineer and the visual baseline owner must retain the hosted OS/font/native-renderer inventory, run the existing Firefox/WebKit comparison command on this candidate, inspect every expected/actual/diff set, and decide the remaining layout/raster dispositions. Command, from `tests/e2e`: `npx playwright test specs/specimen-accessibility.spec.ts --project=firefox --project=webkit --grep "visual baseline" --workers 4`. A subsequent image update requires separate authorization; this task explicitly preserves images. Windows, human AT/zoom/devices, Fluent/Product approvals and live-backend acceptance remain open.
