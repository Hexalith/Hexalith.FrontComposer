# Story 13.6 accessibility result and human checklist

## Approved specimen heading and platform baselines — 2026-11-14

The explicit user “approve” applies to the prepared “Type specimen” heading/description and its affected visual baselines. The exact approved two-line patch SHA-256 bc61d50661e75ad3a3c2b553c6ad198ac34ba762f41fca3ace8af22a20923d35 is now compiled. Home and type heading reflow pass, and all 24 Linux/Windows baseline comparisons pass without snapshot update. This supersedes the preceding unapplied-proposal state. Story 13.6 remains in-progress for the independent failures, runtime and acceptance gates below.

**Candidate:** root 1a68cf5540b6e01848cb6fdb2e37c6d3cd4dd947 plus src/tests/samples patch SHA-256 79a33432312b19a1a65ef47a11dd2ca400b141ca4cc66dbaed97e947bd218928, including the preserved Home correction and the 24 updated PNGs. FrontComposerTypeSpecimen.razor SHA-256 is cf9c4e5b77b8fff291d115f254ae4aa64e4599f107968f1a609097e3e4a47301; responsive-accessibility.spec.ts is 3eacd9353835e386cad9f7bc18fc47f3ac09e566feda9203eef0872f7cfe1f7f; destructive-command-confirmation.spec.ts is eb010c9b7c1342825810082e2b6387de41fcc0aef4cd40602d4c5efb8455a286. Compiled Counter.Specimens.dll is f845dc778913affc4505b98bae10ec8f1522ded1b2907afa50ae92d48850e16a. The sole related locator naming the former long H1 now uses the approved short name; its focus-return behavior remains asserted.

| Verification | Current result |
| --- | --- |
| Debug Counter build, including changed specimen | Zero warnings/errors |
| TypeScript | Exit 0 |
| Focused heading/reflow/scroll/disposal lane | 45/45 across Chromium/Firefox/WebKit; zero failed/skipped/flaky/retries |
| Affected specimen lane | 60/60, including eighteen no-update Linux visual comparisons; zero failed/skipped/flaky/retries |
| Destructive-invoker return to the approved route heading | 3/3 engines |
| Native Windows Chromium baseline regeneration / no-update comparison | 6/6 and 6/6; zero failed/skipped/flaky/retries |
| Native Chromium actual 400% zoom | Four of four Home/type default/spacing canonical geometries pass |
| Requested mobile-grid combined-spacing recheck | 45 focused passes plus six retained failures; same missing badge table in both themes on every engine |

The six mobile-grid checks were run against the rebuilt approved content, so their prior failures were not simply carried forward. After the existing clipping/target probes, table.fc-projection-grid is absent and the table-layout assertion fails. No unrelated implementation correction is included. The previous 93/6 operator run, original-keyboard controls and every older failure retain their original source attribution; this bounded pass does not replace a complete browser/state-artifact/.NET/hosted gate.

### Actual browser zoom and platform capture

The compiled-source headed Chromium observer verifies 1280×900 CSS pixels/DPR 1 → native 320×225/DPR 4 using the extension's automatic per-tab browser zoom API, with outer width 1280, CSS zoom 1 and visual scale 1. All four first canonical measurements pass: Home default/spaced y=124–210 / 118–210; type default/spaced y=42–160 / 23–157. The approved title is read from the compiled component; no DOM title replacement is used. Complete CDP captures are 1280×900 physical pixels. The observer script SHA-256 is 5b28005b17b37daf58ffaf2c3ed7e061d24f836754b151b066549daf3532a70d. These are automated geometry observations, not generated/heard-speech or human AT/device completion.

Linux uses Node 26.4.0 and locked Playwright 1.64.1 with the unchanged native Linux Fontconfig. Windows is genuinely native win32 Node 26.1.0 / OS release 10.0.26300, running the repository's same Playwright 1.64.1 CLI over its WSL UNC path. Its missing pinned Chrome for Testing and Headless Shell 156.0.8078.4 revision 1248 were installed into the normal Windows Playwright cache, without a repository dependency change. Windows Chromium reaches the same explicitly diagnostic Test host through localhost. All six Windows PNGs come from that renderer, not Linux-produced substitutes.

All 24 before/after pairs retain identical dimensions: Chromium Linux/Windows 1280×3826/3894/3962, Firefox Linux 1280×3853/3921/3969, WebKit Linux 1280×3894/3962/4010 for Compact/Comfortable/Roomy in both themes. The title and paragraph are the intended change. Every header matrix and representative full Light/Dark frames were inspected; all lower surfaces remain present. Parent pixel inspection outside header y>=260 finds no channel delta>32 on any image, and WebKit pixels there are identical; that metric is analysis only, with no changed test tolerance. The existing Windows 76,000-pixel allowance, Linux thresholds, font settings, helpers, typography and unrelated PNG/Verify baselines are preserved. The approval/before/after rationale is appended to docs/accessibility-verification/baseline-change-rationale.md.

### Commands, hashes and retained output

Linux raw output, exact scripts, before images, comparison sheets and baseline-comparison.json remain local under /tmp/fc-story13-6-approved-heading-20261114-q_ac1rc9/. That comparison file contains all 24 before/after raw hashes and has SHA-256 1a49ef4cfb743ff7343c3599b7057e84a1a03abe6f26a216314a1875f79d762f. Before PNGs also remain retrievable from the source-owned baseline at root 1a68. Windows reports and exact argv/environment JSON remain under C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-approved-heading-20261114-q_ac1rc9\. The final Windows wrapper SHA-256 is ef9459085d363ed5c647f59c1e6d9b45dd6fabb41af7830701d7a437daa0c797. Parent image/pixel audits are /tmp/fc-story13-6-approved-heading-parent-j35fzub8/baseline-image-audit.json and baseline-pixel-audit.json. No new evidence packet or runtime-identity record is committed.

Repository-root commands:

    aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
    dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers
    npm --prefix tests/e2e run typecheck

The explicit diagnostic host uses the unchanged Test settings: DOTNET_ROLL_FORWARD=LatestMajor, ASPNETCORE_ENVIRONMENT=Test, Hexalith__FrontComposer__Specimens__Enabled=true, Hexalith__Shell__FormAbandonmentThresholdSeconds=5, Hexalith__Shell__TimeoutActionThresholdMs=5000, Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000, Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 and Logging__LogLevel__Default=Warning, followed by:

    dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070

Each Linux Playwright command ran from tests/e2e with BASE_URL=http://127.0.0.1:5070, PLAYWRIGHT_SKIP_WEBSERVER=1, FC_E2E_OUTPUT_DIR=<scratch>/<lane>/out and FC_E2E_HTML_REPORT_DIR=<scratch>/<lane>/html, then the following argv. All use --workers=2 --retries=0:

- linux-update: npx --no -- playwright test specs/specimen-accessibility.spec.ts --grep 'visual baseline' --update-snapshots=all
- focused-final: npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'a focused .* heading stays visible|resize focus visibility|shell focus scrolling|held shell focus visibility frame|O5 the desktop content scrollport'
- focus-and-mobile: the same focused expression plus '|generated mobile grid columns'. Its six failures are retained.
- specimen-verify: npx --no -- playwright test specs/specimen-accessibility.spec.ts --grep 'manifest names|type specimen is nonblank|type specimen renders required|theme and density controls|expanded specimen detail|dark generated command|viewport reflow|visual baseline'
- destructive-heading: npx --no -- playwright test specs/destructive-command-confirmation.spec.ts --grep 'removed destructive invoker restores focus to the route heading'

The native-zoom command from the repository root is:

    xvfb-run -a -s '-screen 0 1600x1200x24' tests/e2e/node_modules/.bin/playwright test --config=/tmp/fc-story13-6-approved-heading-20261114-q_ac1rc9/native-approved/zoom.config.ts

Windows uses the absolute C:\Program Files\nodejs\node.exe. Its CLI install command targets the repository's pinned node_modules/playwright/cli.js with install chromium. The final wrapper windows-visual.cjs runs phases update and verify through that same CLI, existing playwright.config.ts and specimen-accessibility.spec.ts, with --grep 'visual baseline' --project=chromium --workers=2 --retries=0; only update adds --update-snapshots=all. The exact native paths, cwd and output environments are preserved in update-command.json and verify-command.json. Earlier read-only Windows capability-probe and first wrapper syntax failures happened before browser execution and are retained separately; they produced no adopted captures. No failed product comparison is erased by the successful update/verify runs.

Report SHA-256 values:

- Focused 45: a50c03a71692b6a19ce242288f16519d753460efec88a1039e24529552c4cb2d.
- Affected specimen 60: e2bc608513253240bccb8349c767cf47850acb496b4850c5c00f2b80e69905e7.
- Destructive 3: 3f289902f7b681b871d2a64e845fe50afd85b0516b71bd7ce5e43b0ce36c5158.
- Windows update 6 / verify 6: e318308f1521af0059b9dcce343ed9c016f9d0baf9b6b26e84f46bc134688d37 / c9a60cebea2b0963b55e9218c0a97d620c725236bb66946cc01ac786f555105b.
- Mobile recheck 45/6: 52c5442d9d6f25b0f455b5a9dd46b6aec930480f14ca9a4edbdec74b174a11fc.
- Native four-geometry runner: d7e4b50b9624c6eae7bfcde5c31257f95cd0441ee080625c26b4dca4a2305cbb.

**Remaining limits.** Exact-target Aspire startup again fails seven net10/net11 NU1201 mismatches. The earlier ordinary net10 MessagePack binding failure remains a separately attributed unresolved failure; no normal-runtime success is inferred from the title change. All current runtime checks use explicit roll-forward, and the loaded libcoreclr path confirms 11.0.0. The six mobile-grid failures, current-candidate human observations, applicable changed-cue WebKit acceptance and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. The historical manual Pass/waiver records, eighteen itemized boxes, original unfinished acceptance tasks, D3/O2 deferrals and frozen criteria are unchanged. HZ3 is resolved by the explicit user decision; only HZ4–HZ7 close on this implementation evidence. No dependency, policy, public API, gitlink, staging, commit, push or hosted run is added; no current-candidate CI link is available.

**Final checks and cleanup.** Visual governance passes for exactly 24 changed snapshots; the global story-artifact validator and git diff --check exit 0. Byte audits confirm the exact approved two-line sample patch, preserved Home module, original frozen/checklist prefix, full preceding result/rationale content, CRLF and every unrelated PNG/Verify baseline. The owned Test host is stopped, port 5070 is closed, native Chromium/Xvfb and Windows Playwright runners exited, and exact-target Aspire reports no running AppHost. Disposable task zoom profiles were removed; reports, before/after captures and raw failures remain local. No index, commit, push or hosted state was changed.

## Focused heading reflow correction and runtime limits — 2026-11-14

Home's already-focused route heading now remains visible after a viewport/zoom reflow without another focus event. No new native generated/heard-speech observation is claimed. AE-05 remains open for the intrinsically oversized type-specimen heading. A concrete content proposal is prepared but unapplied; current acceptance remains incomplete.

**Candidate.** Root 1a68cf5540b6e01848cb6fdb2e37c6d3cd4dd947 plus the two-file src/tests patch SHA-256 0b38ecbfb0678f599375398ce182fffdb8fd88016820524888c2019c4e222fa8. Final fc-keyboard.js SHA-256 is 64dc9446d10219f530fdd8fbe274b1ddeebe4b6a6ebb2aaff1112978347ec06d; responsive-accessibility.spec.ts is dc58ecff44007bd9db992b127b84cc1a40ac6701abb6c5dc1777faff72183f1b. HTTP-served keyboard bytes match source. Pre-existing dirty Builds, Parties and Platform submodule work is preserved. The original frozen baseline/intent and every original checkbox are unchanged.

The parent invocation began at root 5c54d54e740ec5be87e29eabbd311f56831ced64. An external operation advanced it to 1a68cf5540b6e01848cb6fdb2e37c6d3cd4dd947 before this implementation subtask recorded its baseline and executed these checks. This task created neither commit and preserved the external update; all fresh results above use the recorded 1a68 root and their stated patch/runtime limits.

**Correction.** The existing shell focus path watches only the programmatically focused route heading and its owning boxes. Viewport resize and the later Fluent tier reflow can therefore reveal that same heading without refocusing it. Normal controls retain their existing one-shot focus scrolling. Native navigation, pointer input, wheel, heading Space and heading Ctrl+Home/End release the resize watch; disposal removes the observer and listeners. Neutral modifier-only, consumed and IME input preserve pending visibility. The new regressions establish zero extra Home focus events, real native scroll precedence and disposal. Linux WebKit does not scroll this focused-heading scrollport on Ctrl+Home/End; its two rows explicitly prove cancellation only and attach that capability observation.

### Runtime and verification boundaries

The ordinary exact-target Aspire start exits 2 with seven NU1201 errors: selected Tenants/Parties UI, EventStore.Aspire and Builds.Tooling projects target net11.0 while their FrontComposer owners target net10.0. Exact-target describe reports no running AppHost. The narrow Debug Counter build passes with zero warnings/errors, but its ordinary net10.0 Test host closes the Blazor circuit: MessagePack throws MissingMethodException for Microsoft.AspNetCore.SignalR.IInvocationBinder.GetTarget(ReadOnlySpan<byte>). Counter's dependency output selects SignalR 11.0.0 while its runtimeconfig selects ASP.NET Core 10.0.

Browser/operator/visual/native-zoom checks below therefore use an explicitly diagnostic standalone host with DOTNET_ROLL_FORWARD=LatestMajor and the installed ASP.NET Core 11.0.0 runtime. No dependency, target framework, catalog, policy or runtimeconfig file changed. These results do not qualify the ordinary .NET 10 runtime or repair the AppHost graph. A full .NET/state-artifact/hosted gate is not claimed.

| Lane | Result and attribution |
| --- | --- |
| Pre-edit scanner/measurement checks | 108/108 across Chromium/Firefox/WebKit; zero skips/flaky/retries; helpers unchanged by this correction |
| Node evidence / final TypeScript | 29/29 on Linux / exit 0 |
| New Home reflow regression against original keyboard module | Six expected failures, two per engine |
| Final affected heading/scroll/keyboard/disposal lane | 39/39, thirteen per engine; zero failures/skips/flaky/retries |
| Current diagnostic operator-surface lane | 93 passed / six failed / zero skips/flaky/retries; before the final Ctrl-boundary guard/test additions |
| Original-keyboard control for all six operator failures | Six failures on all engines/themes; exact existing mobile-grid journey with the original module intercepted, so these failures are not introduced by the heading correction |
| Final native Chromium zoom observations | Home default/spaced: two canonical passes; type default/spaced: two retained canonical failures; the diagnostic runner passes its expected-result assertions |
| Approved visual comparisons | 18/18 unchanged on the final production source; visual-final-confirmed report SHA-256 9bb645030008dd57c18c7cbcc209ca1256231085b05133b77fb14ecd12601bb4 |

The six broader failures are both themes' existing “generated mobile grid columns retain native cell targets with combined spacing” cases: after the clipping/target probes, the expected badge table is absent and toHaveCSS('table-layout','auto') fails. Both the preceding source and the original-keyboard control reproduce them. Their deeper runtime/virtualization cause is unproven; narrowing the observer was a conservative scope correction, not a demonstrated resolution. The broader operator report belongs to keyboard SHA-256 6c303c7b78b56f6f5f070da95840d4cc0ced4ed5c2a3592bd328062745d70988 and the starting src/tests fingerprint 87764037bc02d0486bde3e5474c39511cbd00f51d8e35e238c3351541239e736. The final changes add Ctrl-boundary cancellation and its focused tests; no broader final-candidate pass is inferred.

### Actual zoom and unapplied specimen proposal

The headed Chromium observer uses a temporary extension's automatic per-tab tabs.setZoom(4)/getZoom with viewport emulation disabled. Each case verifies reference 1280×900 CSS pixels/DPR 1, then 320×225/DPR 4, outer width 1280, CSS zoom 1 and visual scale 1. Complete CDP captures are 1280×900 physical pixels. The final production observer is native-complete/heading-diagnostic.spec.ts, SHA-256 4be80ec1fd7fcd3661fca1cc992099b7c1900d1fa68c04c3a05c3556f14d253a; its report is ae9cac87dc7c9e1bd4fb5d845ff38d6a3f20e27ccf9fd50a3d57777099dbbf34. Later test-only capability notes and the focus-counter assertion-message correction leave production/helper bytes unchanged.

Home's final canonical envelopes are y=124–210 default and y=118–210 with all spacing, with no failures. Its original envelopes were y=184–270 and y=196–288. The type H1 is intrinsically 260px/420px tall; its complete 274px/434px indicator envelope cannot fit the 225px viewport even after explicit nearest/start scrolling. Its failures remain attached unchanged; no threshold or exception is weakened.

The prepared, **unapplied** patch at /tmp/fc-story13-6-current-20261114-76m_fore/oversized-proposal/specimen-heading.patch (SHA-256 bc61d50661e75ad3a3c2b553c6ad198ac34ba762f41fca3ace8af22a20923d35) proposes:

    <h1 id="fc-type-specimen-title">Type specimen</h1>
    <p>Type, theme, density, lifecycle, and navigation specimen. Generated shell surfaces rendered with deterministic local data.</p>

A DOM-only preview preserves typography, tokens and breakpoints. At actual 400% zoom, its H1 is 104px/120px tall with complete 118px/134px indicator envelopes and zero canonical failures. The full-viewport default and spacing previews are oversized-proposal-fullviewport/heading-type-false.png and heading-type-true.png under the scratch root below; both were inspected. Its script SHA-256 is 79244fe80a0f33bd261ebee7b2ed0da127c4eb121b06dd0161e4901cacee8e7e. Earlier Playwright-sized cropped previews remain separate and are not whole-viewport evidence. Applying this content change would alter approved type-specimen images and needs the pending explicit visual decision. Sample source and every approved image remain untouched. No oversized-heading exception or frozen-criterion amendment is inferred.

### Exact commands and retained attempts

Raw reports, scripts, captures and failed attempts stay outside the checkout at /tmp/fc-story13-6-current-20261114-76m_fore/; no committed evidence packet is added. Node 26.4.0 and locked/installed Playwright 1.64.1 were used. Commands from the repository root:

    aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
    aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
    dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers
    node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
    npm --prefix tests/e2e run typecheck

Diagnostic host, with all existing Test timings retained:

    env DOTNET_ROLL_FORWARD=LatestMajor ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true Hexalith__Shell__FormAbandonmentThresholdSeconds=5 Hexalith__Shell__TimeoutActionThresholdMs=5000 Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 Logging__LogLevel__Default=Warning dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070

All repository Playwright commands ran from tests/e2e with BASE_URL=http://127.0.0.1:5070, PLAYWRIGHT_SKIP_WEBSERVER=1, FC_E2E_OUTPUT_DIR=<scratch>/<lane>/out and FC_E2E_HTML_REPORT_DIR=<scratch>/<lane>/html. Their exact selections were:

| Lane | Command after that environment prefix |
| --- | --- |
| browser | npx --no -- playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper impact partitioning\|default scanner\|R4 \|R5 \|R6 \|R7 ' --workers=2 --retries=0 |
| focused-final-wording | npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'a focused Home heading\|resize focus visibility\|shell focus scrolling\|held shell focus visibility frame\|O5 the desktop content scrollport' --workers=2 --retries=0 |
| operator-final | npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'Story 13.6 responsive operator surfaces' --workers=2 --retries=0 |
| visual-final-confirmed | npx --no -- playwright test specs/specimen-accessibility.spec.ts --grep 'visual baseline' --workers=2 --retries=0 |

The original-keyboard control used --config=<scratch>/grid-control/playwright.config.ts with the same two workers/no retries and output roots grid-control/out-2 and grid-control/html-2. It copies the existing journey verbatim and serves keyboard-before.bin for only the keyboard-module request. Actual native commands ran from the repository root:

    xvfb-run -a -s '-screen 0 1600x1200x24' tests/e2e/node_modules/.bin/playwright test --config=/tmp/fc-story13-6-current-20261114-76m_fore/native-complete/zoom.config.ts
    xvfb-run -a -s '-screen 0 1600x1200x24' tests/e2e/node_modules/.bin/playwright test --config=/tmp/fc-story13-6-current-20261114-76m_fore/oversized-proposal-fullviewport/zoom.config.ts

Retained failures and intermediate attempts are not replaced by final focused passes:

- The initial native observer was interrupted (exit 130); the second timed out at 120s waiting for hydration on the ordinary broken runtime. Disk exhaustion was separately observed at 6.8MB free; only this task's disposable browser profile was removed. The empty/failed low-space runtime diagnostic and the detailed MessagePack diagnostic are retained.
- The first regression command used the wrong working directory and found no tests. The corrected heading-red-2 run fails 6/6 against unchanged product source. The early corrected heading-green lane passes 24/24.
- The first broad observer run is 119 passed / seven failed: six badge-table failures and one WebKit fixture failure. The later narrowed-observer focused lane is 30 passed / the same six badge failures; neither is a complete pass. The original-keyboard control's first configuration attempt failed ESM import; its corrected run reproduces all six failures.
- Wheel and Space red controls each fail on all three engines. The Ctrl red control has four actual native rewind failures in Chromium/Firefox plus two unsupported-native-scroll precondition failures in Linux WebKit. The first final Ctrl lane is 37/39 for that same capability distinction; the corrected capability-labelled lane is 39/39. Earlier 33/33 belongs to the preceding Space/wheel candidate.
- The first tightened native API check fails on floating-point serialization 3.9999999999999996 versus 4 before geometry. The subsequent API check uses ten-decimal numeric agreement; exact viewport/DPR/CSS-zoom checks and all focus thresholds remain unchanged. Initial and corrected reports stay in different directories.

Final focused JSON SHA-256: 25391902966e8bd3afdaee672879e55a3cd22fc68ea41cbdba2fe5c95223dd22. Broader failed JSON: 7b87b756e97e297ef20b1b4e178459f8db460f9d6724a9fe63a01298aa57cb89. Original-keyboard control JSON: c56ecb9a7e662778568b01dc778d98f7a1b45a2d1956b7a51ecf17c43c1d1f0c.

**Repository checks and cleanup.** The global story-artifact validator and git diff --check exit 0. Original-prefix/historical-suffix byte checks preserve all previous spec/result text, frozen intent and original checkbox lines; all four changed files retain CRLF. All 24 tracked specimen PNGs byte-match HEAD, including the 18 executed Linux visual comparisons. The owned Test host is stopped, port 5070 is closed, exact-target Aspire stop/describe report no running AppHost, and all temporary Chromium/Xvfb sessions are closed. Owned disposable zoom profiles were removed; reports, scripts, captures, proposed patch and prior failed output remain retained. No native Windows/AT session was started.

**Acceptance.** The six broader failures, ordinary-runtime/AppHost blockers, oversized specimen, current-candidate human observations, applicable changed-cue WebKit acceptance and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. Historical MAN-136 Pass/waiver records, eighteen itemized boxes, D3/O2 deferrals, original incomplete matrix/result/Evidence tasks and Story/sprint in-progress remain unchanged. No new human observation or approval is inferred. No dependency, policy, baseline, public API, gitlink, staging, commit, push or hosted run is added.

## Agent-performed accessibility observations — 2026-11-07

The user requested "do observations yourself" and confirmed that Windows was unlocked and would be left idle. The agent ran the available observations against root `e972d0f06a8eec3446db0b2270fdb14eef4c1186`, using the preceding clean Debug Counter build and the same standalone Test-host launch command. HTTP-served `fc-keyboard.js` and `fc-focus.js` byte-match source (`9b4f25a652572658e755a5dc673c08732493aeba6ae3c98e5648eef0f8184f6e` and `885eabb1ead35f829373f7ec3b25d8beecdf36d0514e50558b7a50abebcf2b87`); loaded Shell DLL is `6588d976d9882e713bf0e7022af6940c57ca81f62dd952ca08904565f00e851f`. These are agent-generated observations; generated speech and emulation are not human heard-speech or physical-device completion.

**Outcome:** responsive, actual-zoom Tab journeys and emulated touch operations pass. A separate focused-heading observation fails the strict AE-05 geometry floor in four cases. Native NVDA coverage is partial because Windows refuses the existing owned-Firefox foreground guard. This continuation does not establish overall accessibility acceptance.

| Observation lane | Result | Executed scope |
| --- | --- | --- |
| Existing responsive operator-surface suite | **99 passed**, zero failed/skipped/flaky/retries | 33 per Chromium/Firefox/WebKit: Home, module navigation/tabs and three specimens; 320px at 900px/225px heights, combined spacing, complete Tab cycles, targets, bounded scrolling, WCAG 2.2 axe, contrast and media checks |
| Actual headed Chromium browser zoom | **14 passed**, zero failed/skipped/flaky/retries | Five page surfaces and palette/Settings, each with/without combined spacing; existing clipping/target/overflow/axe helpers and verbatim keyboard-cycle implementation |
| Focused headings after actual zoom | **0 passed / 4 failed** | Home and type specimen, each with/without spacing; canonical `focusGeometry` rejects viewport and vertical-scrollport clipping |
| Emulated tablet/phone touch | **4 passed** | iPad gen 7 and Pixel 7 profiles, portrait/landscape; Home-to-Counter tap, Settings Done dismissal, palette query/result navigation, rotation retaining route, no page overflow |
| Windows NVDA/Firefox, first attempt | **3 passed / 19 failed** | One focused-detail-loss explanation generated exactly once, plus two silent 10s idle windows; later keyboard checks refused foreground or failed dependent preconditions |
| Windows NVDA/Firefox, idle-gated retry | **2 passed / 20 failed** | Both key-free idle checks pass; native keys are refused, so the later 400% journeys are not reached |

### Reproduced focused-heading finding

Recipe: load at 100%, set real automatic per-tab browser zoom to 400%, apply all four spacing overrides when listed, focus the page H1, settle paint, then run the unchanged canonical `focusGeometry`. Each measured viewport is **320×225 CSS px, DPR 4**, CSS zoom **1**, visual viewport scale **1**. The complete focused target/ring does not fit the active viewport:

| Focused H1 | Spacing | Top → bottom, CSS px | Canonical failures |
| --- | --- | --- | --- |
| Home | Default | 184 → 270 | Viewport clipping; two vertical-scrollport clipping reports |
| Home | Combined overrides | 196 → 288 | Same |
| Type specimen | Default | 170 → 444 | Same |
| Type specimen | Combined overrides | 215 → 649 | Same |

The 14 passing Tab-cycle journeys advance past the already-focused H1 and do not assert its geometry; they therefore do not refute this additional failure. This is an observed focused-heading-after-zoom concern, not a claim that every fresh route-entry sequence was reproduced. Screenshots and the exact focus/zoom sequence are retained. No product correction, threshold change or accepted exception is applied; **AE-05 remains open** on this finding.

### Actual zoom, visual inspection and touch attribution

The temporary headed Chromium **156.0.8078.4** observer uses a minimal Manifest V3 extension with `chrome.tabs.setZoomSettings({mode:'automatic',scope:'per-tab'})` and `chrome.tabs.setZoom(4)`, with `viewport:null`. Reference **1280×900, DPR 1** becomes **320×225, DPR 4**, retaining outer width **1280**, CSS zoom **1** and visual scale **1**. Xvfb supplies a real headed display. CDP captures the complete **1280×900 physical-pixel** viewport. This does not substitute a 320px viewport resize or CSS transform for browser zoom. The temporary observer reuses the repository's exact assertion helpers and keyboard-cycle function without changing product files or approved images.

The agent inspected desktop and 320px Home, Settings, palette and type captures, plus actual-zoom captures. Home labels remain readable; Settings displays the density controls and wrapping forced-density note; the palette exposes its combobox/results and returns focus to its trigger. The separate H1 clipping above remains visible in the zoom captures. These spot observations are not a new complete visual-baseline approval.

Touch execution uses pinned Playwright **1.64.1** Chromium with `hasTouch`/mobile device emulation, not Safari or a physical tablet/phone. The initial harness failed **4/4** looking up Settings Done by Playwright role. A follow-up passed **2/4** and exposed an additional setup mistake: persisted route restoration displaced Home in the second orientation. The final run clears setup storage before navigation and inspects Chrome's **native CDP accessibility tree**: exactly one unignored `button` named **Done** exists in all four cases, although Playwright's role lookup returns zero. Tapping the stable host works. These failures are retained as harness/locator findings, not silently treated as product failures or erased by the final pass.

### Native Windows observations and limits

Installed versions are Windows **10.0.26300.0**, NVDA **2026.2** and Firefox **155.0.1**; JAWS is absent. The initial global `LogonUI` probe was misleading: current RDP session **1** is unlocked, while `LogonUI` belongs to session **3**. The corrected retry gate requires zero current-session LogonUI processes and at least **210,000ms** idle; it opened at **235,906ms**. Nevertheless the unchanged key server reports **"Foreground is not the owned Firefox; no keys sent"**. A bounded diagnostic reproduced that refusal at both the historical off-screen placement and `(20,20)` on the current display; placement alone does not resolve it. The cause of the OS foreground refusal is not established. No foreground-lock workaround or guard relaxation was used.

Both canonical attempts use runner SHA-256 `4d040174775d135e697307dafc892691ee88c86e92e3ac658803129c51bb5c8e`, changing only output directories. The first attempt's focused-detail-loss sequence passed with one generated neutral explanation; all other new speech/zoom claims stay limited to the checks actually reached. The two silent idle windows pass in each attempt. Earlier successful full native runs and the approved D3/O2 deferrals retain their original attribution. No new human observation or native 400% pass is inferred. No macOS/VoiceOver or physical-device connection is accessible in this session; Windows reports no present WPD/Android USB device class, and no ADB/iOS device tool is available.

### Commands, retained results and acceptance

All Linux raw evidence, scripts and captures are under `/tmp/fc-story13-6-observations-20261107-l883mjts/`. Exact commands (repository root unless noted):

```text
# tests/e2e working directory
BASE_URL=http://127.0.0.1:5070 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_A11Y_EVIDENCE_RUN_ID=story13-6-observations-20261107 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-observations-20261107-l883mjts/browser/out FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-observations-20261107-l883mjts/browser/html npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'Story 13.6 responsive operator surfaces' --workers=2 --retries=0
# repository root; each zoom config uses one worker and zero retries
xvfb-run -a -s '-screen 0 1600x1200x24' /home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/.bin/playwright test --config=/tmp/fc-story13-6-observations-20261107-l883mjts/zoom.config.ts
xvfb-run -a -s '-screen 0 1600x1200x24' /home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/.bin/playwright test --config=/tmp/fc-story13-6-observations-20261107-l883mjts/route-entry/zoom.config.ts
node /tmp/fc-story13-6-observations-20261107-l883mjts/touch-final.cjs
FC_NAT_OUT=observations-20261107-l883mjts WSLENV=FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\runner-canonical\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
python3 -u /tmp/fc-story13-6-observations-20261107-l883mjts/run-native-after-idle.py
```

The last wrapper records its exact argv/environment and launches the same canonical runner with only `FC_NAT_OUT=observations-20261107-l883mjts-retry` after the corrected idle gate. Windows raw logs/slices stay under those two output directories in `C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\`. The complete summary at `observation-summary.json` is SHA-256 `d41bcf3a2c31b2bd3128dca062aac653a9bb9e191a62357a89f7a992334c2bae`; it binds source/binary identities, commands' reports, script hashes and cleanup. Report SHA-256 values:

- Responsive: `b52579e7b5d88a7383ca1ca036e11263b829f75a2838939d2b0b8dad8997c863`.
- Actual-zoom journeys: `b505b65379e34ffeb31c0ca578a60574774fcd8488ef4b46302c33580777f39a`.
- Focused-heading failures: `89539d6f9d889c1cec86c2991253e8b255b3468c7815b5464912df4025bffda6`.
- Final touch: `d87ad8d87adb767ca590c527f7aa9b36829c84dee0589534a264649a6a227e46`.
- Native first/retry: `6d72e3e0e7fcf10e00ccd2c87e7402b6f460526e7e7224534bf7f6241cf4999d` / `14bdccaa2869f33f6fbb3359cd5beac09b1cb7e7d1669aad205e901933032e89`.

**Repository validation.** `git diff --check` and original-byte/checkbox/frozen-block/CRLF preservation checks pass. `python3 eng/validate-story-artifacts.py` exits **1** on the same unchanged Epic 11 tracking: `E11R-AI-1 has unsupported status 'in-progress'` and `E11R-AI-1 must remain open while the active Story 11.25 successor evidence is superseded`. Exact output is retained at `final-story-validator.log`; the gate and Epic 11 records are unchanged.

**Cleanup and status.** The Test host and browser contexts are stopped; port **5070 is closed**. Final Windows inventory reports NVDA **0**, Firefox **0**, geckodriver **0** and port 9516 listeners **0**. Only this result and the primary spec change. The original eighteen human checklist boxes, frozen intent/baseline, historical MAN-136 records/waiver, approved D3/O2 deferrals and independent owner decisions are preserved. Story/sprint remain **in-progress**; the agent observations do not claim manual human completion, physical-device completion, a full visual/state-artifact gate, hosted CI or owner approval.

## Current committed-source keyboard verification — 2026-11-07

All approved product/test corrections are present at clean root `e972d0f06a8eec3446db0b2270fdb14eef4c1186`. This continuation adds evidence only; no new product defect was demonstrated. The [2026-11-04 residual decision](spec-13-6-verify-responsive-and-assistive-accessibility.md#residual-native-findings-decision--2026-11-04) remains authoritative: FX5, FX7 and NAT4 are closed under the narrowed acceptance, while the native D3 duplicate context and O2 skip-link speech remain explicitly deferred. Earlier open-task statements below describe their dated attempts.

**Source and reuse limits.** The `85d223d6cf799b530e7ef68b3e77663c5a5ef84a` → `b70627f649826b1a2338ccc13295ba02b71f7424` `src tests` patch hashes to `b6a0c10b724913409d0d0dcce3691f71c914d6ffae9059607801d6d35ff5677d`, exactly matching the preceding recorded passing review. Since that source, commit `44d802b995da97c6e921b5e1eba6a1eabfd1431d` changes the accessibility implementation only in `fc-keyboard.js`: native navigation cancels a superseded focus-visibility frame; neutral, prevented and composing input retain it. Its four regressions and the adjacent O5 scrollport case are the affected verification lane. That module/spec diff from `b70627f6` to the current root hashes to `e8fd501a65d403c2c6ff9ef8ecfa4d51c558f30e9b1adb205cf44e1ac78e5272`. Other current authentication/MCP, dependency and governance changes are outside this bounded evidence; the previous complete browser, visual, bUnit and native results retain their original source attribution. No current full-runtime equivalence is inferred.

**Fresh checks.** From the repository root, `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug -m:1 -v:minimal --disable-build-servers` exits **0**, with **zero warnings/errors**. From `tests/e2e`, `npm run typecheck` exits **0**. Installed Playwright initially resolved to **1.65.0**, while the tracked lockfile selects **1.64.1**; `npm ci` and `npx --no -- playwright install chromium firefox webkit` both exit **0** and restore the pinned tools without tracked dependency changes. No browser test ran with the stale installation.

The owned standalone Test host was launched from the repository root with:

```text
env ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true Hexalith__Shell__FormAbandonmentThresholdSeconds=5 Hexalith__Shell__TimeoutActionThresholdMs=5000 Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070
```

`/home` and `/counter` returned **200**. The served `/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js` byte-matched the source, SHA-256 `9b4f25a652572658e755a5dc673c08732493aeba6ae3c98e5648eef0f8184f6e`. From `tests/e2e`, the exact browser command was:

```text
BASE_URL=http://127.0.0.1:5070 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-20261107-274wjetg/browser/out FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-20261107-274wjetg/browser/html npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'held shell focus visibility frame|O5 the desktop content scrollport' --workers=2 --retries=0
```

Result: **15/15 passed**, five each on Chromium, Firefox and WebKit; **zero failed/skipped/flaky/runner errors**, zero retries. Native PageDown remains effective after a held obsolete callback is released; neutral, consumed and IME keys preserve the pending visibility adjustment; O5 retains keyboard scrolling without a redundant content stop. This is automated browser evidence, not actual browser zoom or human AT/device evidence. This partial lane does not contain the full state/visual artifact matrix, so no complete artifact-validator or visual pass is claimed.

Raw logs and configured HTML/JUnit/JSON reports remain external at `/tmp/fc-story13-6-current-20261107-274wjetg/`. Report JSON SHA-256 is `8a8f74e5a22be988b566fd17f70a7b396fcedc8db3392d28255f0b4046aed2e2`; browser log `ba8fe1be71e922594f505a87b7c83bb845f4dea7558976de2c20db0d4cd1fef6`; build log `b59be737691d5f0b807916936b0e48160a5489de894d6274b7f4e3c9ba8f2201`; typecheck log `f25f1003ff1cfe84552996098a4b5da48fac24aca38a162310dab5aa0f742da2`. The host was stopped and port **5070 is closed**. No native Windows, NVDA or driver process was started. No hosted run was queried or initiated; no current-candidate CI link is supplied by this check.

**Repository checks.** `git diff --check` exits **0**. Byte checks preserve the complete original spec prefix, the complete historical result content, every checkbox, the frozen block and CRLF. `python3 eng/validate-story-artifacts.py` exits **1** on unchanged Epic 11 tracking: `repository integrity: E11R-AI-1 has unsupported status 'in-progress'` and `repository integrity: E11R-AI-1 must remain open while the active Story 11.25 successor evidence is superseded`. Exact output is `story-validator.log` in the scratch directory above. This independent repository-integrity failure is retained; no Epic 11 record or validation rule is changed.

**Acceptance remains incomplete.** Current-candidate human observations, applicable changed-cue WebKit capability acceptance and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. The seven historical Owner-reported MAN-136 Pass records and their supporting-details waiver remain intact; no renewed human observation is inferred. The eighteen itemized boxes, original matrix/result/Evidence acceptance tasks, frozen intent and Story/sprint **in-progress** status are unchanged. Only this result and the primary spec change; no source, test, dependency declaration, policy, baseline or gitlink is edited, and nothing is staged, committed, pushed or published.

## Code review of the native findings corrections — 2026-11-04

The user chose to defer the D3 and O2 residuals and proceed to review. Three review layers (blind, edge-case and verification-gap) reviewed the uncommitted `src`/`tests` delta. The 26 findings break down as follows:

- **Six patches**, NG01–NG06:
  - a stored-preference silent-hydration bUnit pin;
  - two comment corrections;
  - one test rename;
  - skipping an unchanged grid scroll-margin write;
  - an O5 heading-focus assertion.
- **Three deferrals of pre-existing issues**, recorded in `deferred-work.md`:
  - ND01, the Modal-derived `alertdialog` role;
  - ND02, the Firefox phone-tier layout scroller stop seen in both native runs;
  - ND03, adopter `<header>` banners under `div role="main"`.
- **Fourteen rejections.** Among them, a three-engine experiment refutes the claim that D1 reintroduced desktop cell overlap: Chromium and WebKit measure identically before and after.

The spec's triage log has every verdict.

**Verification after the patches**, from `src`/`tests` patch SHA-256 `b6a0c10b724913409d0d0dcce3691f71c914d6ffae9059607801d6d35ff5677d` on root `85d223d6`:

- Debug builds exit 0, and the touched bUnit plus conformance set passes **105/105**. TypeScript exits 0.
- The complete three-spec gate plus `palette-native-accessibility` ran with two workers and no retries on the standalone Debug Test host: Chromium **389**, Firefox **387 / 2 named skips**, WebKit **387 / 2 named skips**. Every validator passed, and no approved PNG changed.
- Report JSON SHA-256: Chromium `5bda42fa781704076df0e30f3a4a89ba8eb11a414fdce6c847423eca764caf43`, Firefox `f576096a5968f1048424e58a51c02ce17dc8f2905001c30576119999ea9bcbda`, WebKit `ccf8beb247c9aec23cc17fc05967cfe6587c5e7a1fe41e55e5ff4bde21c85982`.
- An earlier identical run passed every test, but its validators exited 1 because the parent had overridden the reporters with `--reporter=line`. That was an invocation error and is retained as a failed attempt.

The eighteen boxes, owner decisions and Story/sprint **in-progress** are unchanged.

## FX8 native NAT rerun on an idle unlocked session — 2026-11-04

The user unlocked Windows and replied "continue" after the parent asked them to leave it idle. All observations below are **automated**: NVDA `Speaking` records are generated speech, never heard speech, and no human observation is inferred. The canonical complete run passed **27 of 31** checks. D1, D2 at actual 400% zoom, O1, O3, O4, O5 and the drawer part of O2 are confirmed natively. **D3 remains:** the palette still generates its dialog context twice. **O2 remains for the palette and Settings:** closing either still generates "Skip to content" before the trigger. Both are reported for human direction and were not fixed in this pass. No product, test, sample or baseline file changed, and nothing was staged, committed or pushed.

**Source and host.** Root `85d223d6cf799b530e7ef68b3e77663c5a5ef84a` plus the uncommitted `git diff --binary HEAD -- src tests` (SHA-256 `d4408cade71ea63ed4dde9ac22021d7f45cddfdeae1774c66b3cd14c02268ad6`, unchanged by the parent's CRLF normalization of `FcHomeDirectory.razor`). `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers` exited **0**, zero warnings/errors (log SHA-256 `1a35e58e63a5322fdf88e571b6eced87f50b1b75497a114a7f77e32bbb6e3f68`). The standalone Test host used the exact command and environment of the [2026-11-03 section](#current-committed-source-local-browser-and-native-verification--2026-11-03). `/home` returned 200, and the scenario specimen served `fc-type-specimen` and `fc-ae-scenarios`.

**Idle gate.** `fx8/gate-and-run.sh` (SHA-256 `3f3ee642f5e0b9905ac293bca5b6ebe02d074ae32cb0accd33c0e025c70182a8`) polled the user's probe `C:\Users\JeromePiquot\AppData\Local\Temp\fc-win-probe.ps1` (SHA-256 `63c39a2a1cd32f7debdb7d1179413c494cb51a2681f3d568c21de8178690c3ca`) every 15 s. It would launch only with `logonui=False` and at least 210,000 ms since the last user input (`ForegroundLockTimeout` 200,000). The gate log `fx8/idle-gate-1.log` (SHA-256 `5091b8cd1b8cc33a822f7374809dcfcbb8a71472271e637f460d0fadcd4aa3e9`) records five samples from 08:00:33Z: idle rose from about 160,000 ms to **222,578 ms** at 08:01:35Z, when the gate opened. Every sample showed WindowsTerminal in the foreground, no NVDA or Firefox, and displays DISPLAY1 1234×2194 (primary), DISPLAY3 3413×1440 and DISPLAY2 2560×1440. The runner and its foreground check were unchanged; no foreground-lock workaround was used.

**Command**, from the repository root under WSL, launched by the gate:

```text
FC_NAT_OUT=fx8-nat WSLENV=FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\runner-canonical\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
```

Runner SHA-256 `4d040174775d135e697307dafc892691ee88c86e92e3ac658803129c51bb5c8e` (with `common.cjs` `5bdfdff8…` and `keyserver.ps1` `9667e647…`, both unchanged). Inventory: Windows 10.0.26300.0, NVDA 2026.2, Firefox 155.0.1. Summary SHA-256 `a53468d2c089e97fa380251bcd15bbf3a02ceaa3bb75c9ed1ac24c797ee27c9c`; NVDA log SHA-256 `da5cde289f1bcbf9502ab5bcfcf18c9c42b3fb0e96af3f12f6df62849f860400`; console log SHA-256 `a0976498022112c114fe44c2df0f6b0cf67d262331aed5399c3e4ae1fed5faae`. No keyed check lost the foreground, so no second run (`fx8-nat-2`) was needed.

**Window and actual zoom.** Reference **1280×900 CSS pixels at DPR 1.5**. **16 native Ctrl+=** presses reached **320×225 at DPR 6.0** (exactly 4 × 1.5), CSS zoom 1, visual scale 1. Ctrl+0 restored the reference.

| Check group | Result | Native observation and attribution |
| --- | --- | --- |
| NAT1 missing-item sequence | **7/7** | Unchanged behavior: one focused explanation; one polite explanation with external focus and silent repeats; no stale recovery after native Tab, tenant/user change or navigation; Clear filter focused with its description |
| NAT2 zero-field and accepted command | **4/4** | One outcome record, silent idle and re-render, removal on navigation, new attempt clears; Batch Increment generates one "Command confirmed" with the pending summary |
| NAT3 direct speech and silent restore | **7/7** | Route and blocked headings once each; no regenerated confirmation after away/back or reload; one new confirmation; both 10 s idle windows silent |
| NAT4 traversal at 100% | **3/3** | `/home` 14 stops, `/counter` 30, scenario 120 (cap), **zero failing stops**. **D1 confirmed:** the desktop scenario grids pass. **O5 confirmed:** no shell content-scrollport stop remains |
| NAT4 actual 400% zoom and reset | **2/2** | As above |
| NAT4 traversal at 400%, `/home` and `/counter`, with and without spacing | **4/4** | 9 and 25 stops |
| NAT4 traversal at 400%, scenario, with and without spacing | **0/2** | Wrapped at 112 stops; exactly **1** failing stop each: the counter-grid Metadata cell (rect 44–845 / 44–1040 px in a 320 px viewport). Attributed to the recorded D2 disposition (WCAG 2.4.11, cell wider than the viewport). **D2 confirmed:** all 24 badge-grid cell stops, rows 1–6, lie inside the viewport with all corners hit |
| NAT4 overlays at 400%, with and without spacing | **0/2** | Settings and drawer pass. The palette fails on identical duplicate speech; attributed to **D3**, see below |

### Remaining findings for human direction (not fixed)

- **D3 — palette context still generated twice.** Native Enter at 400% generated "Command palette dialog", "Search results list", "COMMANDS grouping" twice, then the active option. With spacing, the sequence was the dialog context and active option, "Focus mode", then "Search results list", "COMMANDS grouping" and the active option again. The FX5 change did take effect: the dialog name is no longer doubled inside one sequence (before, "dialog Command palette" was followed by a separate "Command palette dialog"). The repeat itself remains. Browser traces show Fluent's native `showModal` entry focus (on the dialog or its shadow `div tabindex="-1"`) before the input in the same frame, and an unchanged active option. The remaining cause is therefore likely in the vendor dialog entry step and NVDA's handling of a second focus event; it is not proven. FX5 stays open.
- **O2 — palette and Settings Escape still generate "Skip to content".** Palette close generated "Browse mode", then on the second Escape "same page link Skip to content" and then the trigger. Settings close generated "clickable same page link Skip to content" and then the trigger. The drawer no longer does: it generated only the toggle button. Linux traces show the palette and Settings focus returning directly in the same task. The native speech therefore probably comes from NVDA's virtual buffer resetting to the document start when the modal closes, not from a DOM focus stop; that is not proven. The O2 spoken symptom is not resolved for these two overlays.

### Other native confirmations

- **O1:** the NVDA log holds only **2** "Density set to" records, against 18 in the 2026-11-03 run. They are "Comfortable", generated during the Ctrl+= zoom-in when the Tablet tier forces Comfortable, and "Compact", generated after Ctrl+0 restores the desktop tier. Both are post-hydration, viewport-forced changes that FX4 keeps announced. Full loads and reloads generated none.
- **O3:** the `/home` route heading is generated as "Welcome back, FrontComposer specimen user. heading level 1" without the former "banner landmark" prefix.
- **O4:** the drawer opens as "Primary navigation dialog".

**Cleanup.** The runner verified NVDA count 0, owned Firefox and driver absent, geckodriver 0 and port 9516 free. The Test host was stopped afterwards and port 5070 is closed. The host log (SHA-256 `2b1a77a9540f055be61e733d3e0e5b435715ebabf67735279490c17cfde3bc79`) retains specimen diagnostics, with no correlated failed check. Raw slices, traversals and the NVDA log stay in `C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\fx8-nat\`; the gate log and console log stay in the session scratchpad's `fx8/` directory.

**Checkbox effect.**
- FX6 is checked: both 400% scenario traversals pass every badge-grid stop, with only the recorded Metadata disposition failing.
- FX3 is checked: `nat4:traverse-scenario-100` passes natively, and every remaining failure is attributable to D2 (the Metadata disposition) or D3.
- FX8 is checked: the complete native suite ran on an unlocked, idle session and every failure is attributed.
- FX5 stays open because the D3 duplicate remains.
- **FX7 is returned to unchecked:** the palette and Settings parts of O2 still generate "Skip to content" natively.
- NAT4 stays open: D3 was corrected rather than deferred, so its remaining failure keeps NAT4 open.

The eighteen boxes, owner decisions, frozen criteria and Story/sprint **in-progress** are unchanged.

## Remaining native findings correction (FX4–FX8) — 2026-11-04

After the D1 decision the user replied "fix". The spec reads that as authorizing corrections for D2, D3, O1 and O2–O5; the reading is the assistant's, and the user may narrow it. Status: FX4, FX6 and FX7 are implemented and verified in browsers and .NET. FX5 is implemented but stays open: browsers cannot show one native focus step, and NVDA confirmation is still needed. The native NAT rerun required by FX8 is **blocked**, so FX8 and NAT4 stay unchecked. No dependency, baseline, threshold, emitter, CI or public API changed. Nothing was staged, committed or pushed.

**Source identity.** Root `85d223d6cf799b530e7ef68b3e77663c5a5ef84a` plus the uncommitted `git diff --binary HEAD -- src tests` (SHA-256 `d4408cade71ea63ed4dde9ac22021d7f45cddfdeae1774c66b3cd14c02268ad6`). That diff includes the earlier D1 change to `fc-projection.css`. Raw file SHA-256: `FcDensityAnnouncer.razor.cs` `6d06f0a3e4483bfdd73c8009515964a674420616044fb974b6e21b7064a80f08`; `FcCommandPalette.razor` `7616eae2172783c515737e390399047e0efa1f3e784e408876d7726467d69ea8`; `FcHomeDirectory.razor` `0326def45df00e211e56f463617ca7f4d056943df7d8c53873ac496441b4c819`; `FcHamburgerToggle.razor` `6cf9be695dea270750197a8467165d67d1b4ff33aad9cb3785f0dd3ef82dde0b`; `FrontComposerShell.razor` `ce64cf88776f9c58f85fda918d7da4403b22011171161263bcee6b0e518f7889`; `fc-datagrid.js` `6bbadf4894f0981622dce2e4b6d6466a9648782d38ca5a0fecd406c2d3233a3e`; `fc-focus.js` `885eabb1ead35f829373f7ec3b25d8beecdf36d0514e50558b7a50abebcf2b87`; `fc-keyboard.js` `78e2b66d1784a2b2bae9ae0aa13c288eaa4b14991a51274db129bcb79a4586d7`; `responsive-accessibility.spec.ts` `0f733faa7f9a373084c9976c47ac732a8f80bd0f634704ffe9b88e0cc9a63a27`; `palette-native-accessibility.spec.ts` `d44580b2e2cbbac3ebb5bdd956342b3601593c05f2ea7bdcc99dac95b078fc64`.

### Corrections

- **O1 / FX4 — silent load-time density.** On a full load, the density feature default (Comfortable) was replaced first by bootstrap-capped hydration and then by the recompute from the first measured viewport tier. The announcer spoke that second change: "Density set to Compact." on every load. `FcDensityAnnouncer` now selects effective density, user preference and hydration state together. Changes made while hydration is idle or in progress are silent. Without a preference change, a recompute is silent until a second measured tier has been reported; Fluxor's existing `IActionSubscriber` counts `ViewportTierChangedAction`. Post-hydration user selections and later viewport-forced changes are announced. No new public type or API was added.
- **D3 / FX5 — one palette dialog name.** The palette root no longer nests `role="dialog"` with the same name inside Fluent's dialog. It declares `data-fc-dialog-label`, and a `fc-keyboard.js` observer forwards that label to the `fluent-dialog` host's `aria-label`, which Fluent forwards to its native dialog. In all three engines, Playwright now finds one dialog-role element, an `alertdialog` named "Command palette", and no plain `dialog`. Before the fix, Chromium and Firefox showed an unnamed `alertdialog` plus a nested `dialog` "Command palette". The search input carries `autofocus`, so Fluent's own `show()` focuses it in the same task that opens the dialog. **Limit:** Fluent's native `showModal` still focuses its own entry point first (the shadow `div tabindex="-1"` in Chromium/WebKit, the dialog element in Firefox). That step belongs to the vendor template and cannot be removed without a dependency change. The regression therefore asserts that no rendered frame leaves focus there before the input (WebKit previously took a separate frame). Whether NVDA now generates the dialog context once has not been verified.
- **D2 / FX6 — badge-grid Tab scrolling.** Cause: before measuring, `observeRowMetrics` briefly sets every row and cell to `height:auto`. The forced layout clamped the labelled region's `scrollTop` to the shorter content (29 of 137), so a cell that native focus had just scrolled into view fell back out. The probe now saves every enclosing scroll offset and restores it together with the styles. The existing grid `focusin` handler also gives a focused body cell `scroll-margin-block-start` equal to the sticky-header height plus 8 px, the margin Fluent already applies for its arrow navigation. Only `fc-datagrid.js` changed; it is the existing grid focus code that FX6 permits "only if needed". **Metadata disposition:** the unsupported-field cell is wider than a 320-pixel viewport. Under WCAG 2.4.11 (AA) it stays partly visible, with its full row height inside the scrollport. The canonical `focusGeometry` result for the cell is attached unchanged; thresholds were not weakened. The cell's only actionable control, its diagnostic link, passes `focusGeometry` with no failures.
- **O2 / FX7 — direct Escape return.** Blazor removes the palette's `fluent-dialog` straight after the Escape round trip. That left focus on `<body>` for one rendered frame before `restoreOverlayOrigin` ran from a later animation frame; in Chromium the blur and refocus also landed in different frames. For the palette and Settings, `fc-focus.js` now captures the overlay origin when Escape is pressed (native `cancel` or a host `keydown`). It then returns focus to that origin synchronously, from Fluent's host `toggle` (closed) or from a removal observer in the same task. Any other key or pointer input clears the mark, so a result that navigates keeps its route-focus path. A pending abandonment stay keeps its existing precedence. In Linux Settings and the drawer already returned within one frame; their regressions pin it.
- **O3 / FX7.** `FcHomeDirectory`'s `<header>` uses `role="presentation"`, following `FcPageHeader`.
- **O4 / FX7.** `FcHamburgerToggle` sets `PanelHeader` to the existing localized `NavMenuAriaLabel` (en "Primary navigation", fr "Navigation principale"; no resource-file change). `PanelHeader` alone only renders `<span slot="title">`. The same localized value is therefore declared as `data-fc-dialog-label`, and the `fc-keyboard.js` observer names the drawer host. It locates the host through Fluent's `{hamburger id}-drawer` id, and the browser regression pins that link. The observer also aligns the open shadow dialog's `aria-label`, because Chromium/WebKit bind the host label once, before it arrives.
- **O5 / FX7 — disposition: redundant stop removed.** Only Firefox made the overflowing `.fc-shell-content` scrollport an unnamed Tab stop; Chromium/WebKit skip scrollers that have focusable content. The skip link targets `#fc-main-content` and route focus targets the heading, both inside the scrollport. The layout item is therefore `tabindex="-1"`. In all three engines, Tab from the last rail control lands inside `#fc-main-content`, and PageDown from the route heading scrolls the content by 445 px. Naming it as a region was rejected because a region landmark would wrap the `main` landmark.

### Verification

| Lane | Result |
| --- | --- |
| Counter Debug build (`dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers`) and Shell.Tests Debug build | Exit 0, zero warnings/errors |
| Touched-component bUnit, Fluent conformance, resources, density lint (`-class` FcDensityAnnouncer, FcCommandPalette, FcHamburgerToggle, FcHomeDirectory, FrontComposerShellContentLandmark, FrontComposerShell, Story13AccessibilityPrimitives, FcPaletteResultList, FcPaletteTriggerButton, FcSettingsDialog, FcSettingsButton, DensityNoPerComponentLogicLint, FcShellResources, FluentConformance) | **287 passed**, zero failed/skipped |
| Shell governance (`-namespace Hexalith.FrontComposer.Shell.Tests.Governance`) | First attempt **199 passed / 2 failed**: the two analyzer-policy canonical Release builds hit NETSDK1064 (stale Release restore of Cli's `Microsoft.CodeAnalysis.CSharp.Workspaces` 5.9.0). After `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release`: **201 passed**, zero failed |
| `npm --prefix tests/e2e run typecheck` | Exit 0 |
| New browser regressions (D2 ×2, O1, O2 ×3, O3, O4, O5, palette entry), three engines | **30 passed** |
| Red control: same selection against the HEAD `fc-datagrid.js`/`fc-focus.js`/`fc-keyboard.js`, temporarily swapped and then restored byte-for-byte | **15 failed / 3 passed**. D2, palette entry, O2 palette, O4 and O2 drawer failed in every engine; the drawer's O2 locator needs the drawer name. O2 Settings passed, as it already returned within one frame. O1, O3 and O5 need Razor/C# and use the bUnit tests and pre-fix diagnostics as their red evidence |
| Complete three-spec gate, first attempt | Chromium **384 passed / 1 failed** (`held generated recovery cancels after tenant scope change in dark`; the validator therefore exited 1); Firefox **383 passed / 2 skips**; WebKit **383 passed / 2 skips**; both validators exited 0 |
| Failed test repeated in isolation (`--repeat-each=5`, all four tenant/user × theme variants) | **20/20 passed** |
| Complete Chromium second attempt | **385 passed**, validator exit 0 |
| `palette-native-accessibility`, `route-skip-links`, `settings-persistence`, `sidebar-responsive` | **41 passed** per engine (123 total) |
| Related `route-contract`, `overlay-initialization`, `form-abandonment-guard`, `shell-chrome`, `projection-command-announcements` (not required) | **150 passed** |
| Visual comparisons / state files / artifact size | **18/18** in each complete attempt; approved PNGs unchanged; 116 states per engine; every root below 52,428,800 bytes |

All lanes ran with two workers and `--retries=0`. The coordinator `fx2/gate/run-gate.py` (`f395af42e25a93cda67622bd0b2f87357956908ebaf9ae406dcd696dca9be983`) wrote exact argv, environment, exits and log hashes to `fx2/gate/commands.jsonl` (`9293bf464ed2791b7568ef99b72ed5a7c7e2704ce8943d40127431f676abb7ba`). Run IDs were `story13-6-fx48-<engine>-20261104`, plus `story13-6-fx48-chromium-20261104-2` for the second Chromium attempt. Report JSON SHA-256: Chromium `565ea9f47e3be9e2babdb1cdc76f31c78ccede393df3845d2a62e600963c2dad`, Firefox `fdc6b6332dbd881751e61feea708ca3b7a82125332e7ece1ce6b572bef33d59a`, WebKit `e5a29e5640330f010b6f861730554f23cdbb514857a77168f5fecc0ab5e05578`. Results/HTML bytes: Chromium 17,055,393 / 12,065,460; Firefox 15,578,470 / 8,460,101; WebKit 12,804,626 / 7,042,771. The named skips are unchanged from the D1 gate.

**Chromium failure attribution.** After a scope change, the failing test focuses `fc-ae-rows-two` and expects that focus to persist. It passed in the D1 gate, in 20 isolated repeats and in the complete second attempt. None of the changed modules touches its path: blocked-scope heading entry, `focusReplacementHeading` gating and the scope remount. It is recorded as one intermittent failure of a complete gate, not substituted by the reruns. Its cause is not proven.

### Native rerun — blocked

At 06:23Z the Windows session showed Windows Terminal in the foreground, and the display configuration had changed to a single 3840×2400 panel. The canonical runner (`runner-canonical\native-nat.cjs`, unchanged SHA-256 `4d040174775d135e697307dafc892691ee88c86e92e3ac658803129c51bb5c8e`) ran with only a new output directory:

```text
FC_NAT_OUT=fx48-nat WSLENV=FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\runner-canonical\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
```

It recorded **2 passed / 20 failed** (2026-11-04T06:32:14Z–06:33:39Z; summary SHA-256 `2ac37964ef49c4617733f62661f08eb5c7aa937a232713e3aedcbe7db24cdaef`). Only the two key-free 10-second idle checks passed. Every other check failed before sending a key, with "Foreground is not the owned Firefox; no keys sent" (or a precondition that depended on it): Windows refused to give the owned Firefox the foreground while another application held it. The 400% checks never ran, because they depend on the zoom check. The reference measured 1280×900 at DPR 2.5, which would quantize 400% exactly. Cleanup verified NVDA count 0, owned Firefox and driver absent, and port 9516 free. No foreground-lock workaround was used. The earlier `fx-nat4` attempt, made while the session was locked, stays recorded.

**Remaining.** On an unlocked, otherwise idle Windows session, rerun the same command with a new output directory. All checks must pass, or each failure must be attributed. That run also supplies the only NVDA evidence for D3's single dialog context and O2's spoken return. Until then FX5, FX8 and NAT4 stay unchecked. The eighteen boxes, owner decisions, frozen criteria and Story/sprint **in-progress** are unchanged; no human observation is inferred. Owned hosts, browsers, NVDA and drivers are stopped, and ports 5070 and 9516 are closed. Diagnostics and logs are under `/tmp/claude-1000/-home-administrator-projects-hexalith-frontcomposer/d5dfd614-82d8-474d-97ad-7501244cce1d/scratchpad/nat-Gmn4/fx2/`: `combined-after4.json` `d8db15ab4a27997af1fc4586d4518ad4ea16fd114b2850f2a51cfcf7c3da0315`, `close-trace-before.txt` `78adf0ac2c62dc0669a6340ebc172e40483da93e2266c167084ccf4408e4db2b`, `close-trace-after4.txt` `a325692c3be8a09043cb754da4a62de68a1801ec3196c1ce93633f87a78d540c`, `new-tests-2/run.log` `708aca1f309be1cf67c9628eb4a0185cbf0a7564eb8b38d6aa5ed1badaacde45` and `red/run.log` `110ec72d062c21cd4e1078d6369c1a4db2e93008b501b83338130f96e92b47c9`.

## Firefox generated-grid layout correction (D1) — 2026-11-04

The user chose to correct D1 now and defer D2, D3, O1 and O2–O5 (`deferred-work.md`). The CSS fix and its three-engine browser verification are complete. The required native NAT4 rerun is **blocked**: the Windows session is locked, so no native key can reach Firefox. NAT4 therefore stays unchecked. No baseline, threshold, dependency, emitter, sample or CI file changed, and nothing was staged, committed or pushed.

**Source identity.** Root `85d223d6cf799b530e7ef68b3e77663c5a5ef84a`, whose `src`/`tests`/`samples` bytes equal `37181beae5f52d8cd98091664355871f4eb64f22`. The uncommitted `git diff --binary HEAD -- src tests` SHA-256 is `bbc4aa3baa7d1d29c7d2edf4ca6c9aafaeefe9efb6dc217d63f5e614d3ec970b`. Raw file hashes: `fc-projection.css` `6c4d4054adeb6fd1eedd87e6e685c400d8c2e162b8865a5612b71f7dfaceedf9`, `responsive-accessibility.spec.ts` `aac7bc7df9a946df683d256cb20dd7489156f4edd0910d866f6b82498770f6b5`.

**FX1 — cause and correction.** An injected-style probe against the pre-fix host isolated the trigger. Removing `min-inline-size: max-content` from the generated **table** alone made Firefox lay it out at 1,160 px at 1280 CSS pixels. Removing it from the `th`/`td` cells, or from the `fc-datagrid-host` region, left the table at 17,895,614 px. Fluent renders the table with inline `table-layout: fixed; width: 100%`, and Gecko resolves a max-content minimum on that table to its maximum coordinate. The correction keeps the shared `.fc-projection-grid` rule for the host and cells. It adds `table.fc-projection-grid { min-inline-size: auto; }` and restores `min-inline-size: max-content` only on the existing phone/tablet `.fluent-layout[global-scrollbar]` rule, beside its `table-layout: auto`. No emitter or public API change was needed.

The same probe compared current and corrected CSS, with and without the four text-spacing overrides, at 1440, 1280, 1024, 1023, 768 and 320 CSS pixels in Linux Chromium 156, Firefox 157 and WebKit. Chromium and WebKit table, region and row measurements were identical in all 12 width and spacing combinations each. Firefox was identical at the three global-scrollbar widths (1023, 768 and 320, including 47 px spaced rows at 320). At 1440/1280/1024 the Firefox table moved from 17,895,590–17,895,614 px to 1,296/1,160/904 px, equal to WebKit. Probe outputs are `fx/experiment.json` (`9c87cd13ba6dca78d494645d81d67cb218ebe20704409174c5a6aa8761f364dd`) and `fx/candidate.json` (`2173e1186d12d070663e76b3424d92f01dea1508b098b06eec05a3467826a687`) under the session scratchpad.

**FX2 — regression.** The new test `D1 desktop generated grids keep their tables and column headers within the labelled scrollport` loads the scenario specimen at 1280×900. For both generated tables it requires a labelled owning region, a table width no more than 1 px beyond the region's client width, and every header cell intersecting the region's scrollport. From `tests/e2e`:

```text
BASE_URL=http://127.0.0.1:5070 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=<root>/out FC_E2E_HTML_REPORT_DIR=<root>/html npx --no -- playwright test specs/responsive-accessibility.spec.ts --grep 'D1 desktop generated grids' --workers=2 --retries=0 --reporter=line
```

| Attempt | Result |
| --- | --- |
| Pre-fix CSS (`320e9664…`, served byte-identically) | **1 failed / 2 passed**: Firefox received **17,895,614** against `<= 1177`; Chromium and WebKit passed. Log `fx/red/run.log` `719dfdc1e5683266fdd229909b7581dabf1d49e583808fe2a317aecdd2c8478d` |
| Corrected CSS on the rebuilt host | **3 passed**. Log `fx/green/run.log` `24a44539135f39f9933473fd5668ac564ac7645c760b5303c3e926f8cddc0f53` |

No existing narrow, zoom or spacing grid assertion was edited.

**FX3 — verification.**

| Lane | Result |
| --- | --- |
| `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers` | Exit 0, zero warnings/errors |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers` | Exit 0, zero warnings/errors |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FluentConformanceTests' -class '*DensityNoPerComponentLogicLintTest' -parallel none` (the Shell tests that read or scan this CSS) | **40 passed**, zero failed/skipped |
| `npm --prefix tests/e2e run typecheck` | Exit 0 |
| Complete Chromium selection | **376 passed**, zero skipped/failed/flaky, zero retries; validator exit 0 |
| Complete Firefox selection | **374 passed / 2 named skips**, zero failed/flaky; validator exit 0 |
| Complete WebKit selection | **374 passed / 2 named skips**, zero failed/flaky; validator exit 0 |
| Browser aggregate | **1,124 passed / 4 named skips**, 1,128 total (the earlier 1,121/4 plus the new case on three engines) |
| Visual comparisons, included above | **18/18**; all 24 specimen PNG baselines unchanged (`git diff` empty) |
| State files / artifact size | 116 per engine; results/HTML bytes Chromium 14,347,952 / 7,988,958, Firefox 15,561,448 / 8,439,710, WebKit 12,768,964 / 7,024,649, all below 52,428,800 |

Each engine ran from `tests/e2e` as `npx --no -- playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=<engine> --workers=2 --retries=0`, followed by `node scripts/validate-a11y-artifacts.mjs` in the same environment. That environment set `BASE_URL=http://127.0.0.1:5070`, `PLAYWRIGHT_SKIP_WEBSERVER=1`, separate output and HTML roots, `FC_A11Y_EVIDENCE_ENGINE=<engine>` and `FC_A11Y_EVIDENCE_RUN_ID=story13-6-d1-<engine>-20261104`. The coordinator `fx/gate/run-gate.py` (`82edd863d4e4ebce91a7760b1279a6b64e3a15d1ef0f62d17c5d9b5005c94420`) wrote exact argv, environment, exits and log hashes to `fx/gate/commands.jsonl` (`6fefccd7e8422808030d47b988507206cb50850987bfc857016344bc55df70d4`). Report JSON SHA-256: Chromium `95bd96cecf8aba70e1c3da8fff43589974e61dd54db36a98d15a63a6e96c2538`, Firefox `f514ee1d1147c5159fb8738a754dadcd16c18af6a86e4543c0cb15565bdf6588`, WebKit `79a9a4398f3309ff150ae778010d1a11f21aa531c414c118a083d20b9760dd24`. Firefox skips the native-plane bevel proof and the story 11.5 motion pin; WebKit skips the overflow clip-margin case and the same motion pin. Every lane passed on its first execution.

**Native NAT4 rerun — blocked.** The canonical runner (`runner-canonical\native-nat.cjs`, unchanged SHA-256 `4d040174775d135e697307dafc892691ee88c86e92e3ac658803129c51bb5c8e`) ran with only a new output directory and the existing NAT4 filter:

```text
FC_NAT_ONLY=nat4 FC_NAT_OUT=fx-nat4 WSLENV=FC_NAT_ONLY:FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\runner-canonical\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
```

It recorded **0 passed / 4 failed**, each failing with "Foreground is not the owned Firefox; no keys sent" before any key was sent. The 400% traversals and overlays never ran, because they depend on the zoom check. Summary SHA-256: `33beab48b3a0df4dcb971a006a9775da85b9aed68a9b45e4acacd7919b0a99b2`. NVDA's only speech was "Desktop window". A foreground probe found window handle 0 and `LogonUI` running: the workstation is locked. The display configuration also differed (reference DPR 2.5 instead of 1.5). A bounded unlock poll (`fx/lock-poll.log`, SHA-256 `e48a214fb12ed922d5d47e332c6cfb4ea75dfc17afcfb946e4f5487fe6e428f5`) found the session still locked in all 20 samples, 30 seconds apart, from 2026-11-04T05:00:41Z to 05:10:21Z. This is an environment blocker, not a product result. Cleanup verified NVDA count 0, owned Firefox and driver absent, and port 9516 free.

**Remaining.** NAT4 needs the same command repeated on an unlocked session with the 150% secondary display active. `nat4:traverse-scenario-100` must then pass, and any remaining failure must be attributable to deferred D2 or D3. Until then NAT4 stays unchecked. The eighteen boxes, owner decisions, frozen criteria and Story/sprint **in-progress** are unchanged; no human observation is inferred. The host was stopped and port 5070 is closed.

## Agent-generated native NVDA evidence — 2026-11-03

The user answered "create evidence yourself". Every observation below is **automated**: NVDA `Speaking` records are generated speech, never heard speech, and no human observation, waiver or amendment is inferred. The eighteen itemized boxes stay unchecked. OI-16/G-4/FLUENT-APP-1/Product/Architect decisions and the changed-cue WebKit capability acceptance are decisions, not evidence, and stay open.

**Tested source.** Root `37181beae5f52d8cd98091664355871f4eb64f22`. `git diff --binary b105c846c31844441f1cff84b5c187da0eff3546 HEAD -- src tests samples` is empty, so product, test and sample bytes equal the earlier `b105c846` candidate; only `.github/workflows/release.yml` and story documentation changed since. `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q -nodeReuse:false --disable-build-servers` exited **0**, zero warnings/errors. The standalone host used the exact command and environment of the [preceding committed-source section](#current-committed-source-local-browser-and-native-verification--2026-11-03) on `http://127.0.0.1:5070`.

**Inventory.** Windows **10.0.26300.0**, NVDA **2026.2**, stock Firefox **155.0.1**, geckodriver SHA-256 `e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d`. The standard JAWS directory is absent (`Test-Path` False). No macOS/VoiceOver or physical tablet/phone is connected. The keyboard layout is French AZERTY (`0x040C`); every key used has a layout-independent virtual-key code.

**Runner.** New directory `C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb`; the base runner `native-current.cjs` (SHA-256 `3a9c73dccc83f25f8b8c3ca05be20c9f38fc160df71f356f9376843ddaf8822c`) was copied in unchanged and earlier directories were not touched. The executed copy is `runner-canonical\native-nat.cjs` (SHA-256 `4d040174775d135e697307dafc892691ee88c86e92e3ac658803129c51bb5c8e`) with `common.cjs` (`5bdfdff8feb8d8526e7d9ed6c797660a4596eafdf0d3d2ee2fa2d6fe5ab9e514`) and `keyserver.ps1` (`9667e6474865d86d54f6ff8cdbdb36cd9056d0e72e91983e55fb0fadc28bceb7`). It keeps the base pre-existing-NVDA refusal, geckodriver port 9516, NVDA launch `-c <dir> -f <log> -l 12 --disable-addons`, foreground check before `keybd_event`, `Speaking` extraction and cleanup. Two changes are mechanical: keys go through one persistent PowerShell process that repeats the same foreground check per command, so a native Tab arrives within the 500 ms the delayed-result fixture allows; and a device-pixel `SetWindowPos` gives an exact 1280-CSS-pixel reference. `nvda-config\nvda.ini` is the earlier profile file (SHA-256 `b2963b75307472ca72891f5381952aea8e68da29bd0d5de365277e745cdd0eef`; automatic say-all on page load off).

Commands, from the repository root under WSL:

```text
FC_NAT_OUT=canonical WSLENV=FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
FC_NAT_ONLY='nat4:traverse-scenario-100,nat4:actual,nat4:traverse-scenario-400,nat4:overlays-palette-settings-drawer-400,nat4:zoom-reset' FC_NAT_OUT=focused-rerun WSLENV=FC_NAT_ONLY:FC_NAT_OUT node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb\runner-canonical\native-nat.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-nat-ybWKjtDb'
```

The canonical run (2026-11-03T20:24:09Z–20:34:39Z) used the byte-identical runner before it was frozen into `runner-canonical\`. Its summary SHA-256 is `9d330accdbeba22e90d7ed915b88052e4de7ac7403d29da72ebd80570d408e5e`; its NVDA log SHA-256 is `2ac055107a8b1e43f2ec19abaad7bf8bb1c63f683869c7e75cf8742ea21ea8b8`. The focused-rerun summary SHA-256 is `e2235a53eb5b0d42ec54abe957d7595fc4cc9472032fd8c62fa6e05aceaac33e`.

**Window and actual zoom.** The primary display is 2160×3840 at 175% (DPR 1.7647), where 400% cannot quantize to exactly four times the reference. The owned window was therefore placed on the 3840×2160 display at 150%. Reference: inner **1280×900 CSS pixels, DPR 1.5**, CSS zoom 1, visual scale 1. **16 native Ctrl+=** presses reached **320×225 CSS pixels at DPR 6.0** (exactly 4 × 1.5), CSS zoom 1, visual scale 1, verified again on every page load. Ctrl+0 restored 1280 / DPR 1.5. Firefox 155 steps zoom more finely than the classic table, so the presses are counted rather than fixed.

### Canonical results — 31 checks: 26 passed / 5 failed

| Check | Result | Native observation |
| --- | --- | --- |
| NAT1a focused detail loss | Pass | Native Shift+Tab/Tab onto the detail link; the missing result focused the non-live explanation (`aria-live=off`); **1** generated record "The expanded item is not in the current results."; shared status empty; no Clear filter or AM-22 copy; **0** records in a further 1.5 s. |
| NAT1b external focus and repeats | Pass | Focus stayed on `fc-ae-rows-two`; **1** shared polite record; three `fc-ae-rows-repeat` refreshes kept focus and message revision `3`, **0** records. |
| NAT1c delayed result, native Tab | Pass | The native Tab key returned 294 ms after the delayed action started, moving focus out of the detail to the specimen "Counter name" edit; after 2.6 s focus was unchanged and the explanation was never focused. **1** shared polite record. Decision 1 applies here: once focus has left the detail before removal, the removal is no longer a focus loss, so the stale-recovery check is "no focus move". The NAT1(c) "no speech" clause is asserted for the scope and navigation cases. |
| NAT1c delayed result, tenant change | Pass | Scope changed 6 ms after the delayed action; **0** explanation records, no explanation or status remains; the replacement tenant renders "Workspace unavailable" and its heading takes focus (not a recovery target). |
| NAT1c delayed result, user change | Pass | Same outcome for the user change: **0** records, blocked heading focused. |
| NAT1c delayed result, navigation to `/home` | Pass | Navigation 12 ms after the delayed action; the route heading is focused; **0** explanation records. |
| NAT1c filter-hidden item | Pass | Native Enter on the Success chip; Clear filter focused; **1** record "specimen status region Clear filter button 1 expanded item hidden by current filter". |
| NAT2 zero-field persistence | Pass | Validation outcome generated **1** record and kept trigger focus; **0** records during 3 s idle; scripted `fc-ae-lifecycle-idle` + `fc-ae-rows-two` re-render kept identical feedback, **0** records, no open dialog. |
| NAT2 navigation away/back | Pass | In-app `/home` and back: feedback removed; **0** records of the earlier outcome. |
| NAT2 new attempt | Pass | Validation then `accepted` with native Enter: prior feedback cleared, pending summary visible, no dialog; "Command accepted. Updating the view." generated. |
| NAT2 `/counter` Batch Increment | Pass | Native Enter on the submit button; statuses `Command accepted. Updating the view.` → `Command confirmed.`; pending summary appeared (absent on an idle load); **1** polite owner; **1** "Command confirmed" record; no assertive/alert node or mutation (DOM-level). |
| NAT3 `/counter` away and back | Pass | Native Enter on Specimens then Counter tiles; each route heading generated; **0** "Command confirmed" records. |
| NAT3 `/counter` native F5 reload | Pass | **0** "Command confirmed" records. The reload slice also contains "Density set to Compact." (observation O1). |
| NAT3 distinct new command | Pass | A second submission generated **1** "Command confirmed" record; no assertive mutation. |
| NAT3 route headings | Pass | Native Enter on rail tiles Counter → Specimens → Default → Counter: each focused route `h1` generated exactly **1** heading record. |
| NAT3 blocked scope heading | Pass | Native Enter on `fc-focus-block-scope`: **1** record "Workspace unavailable heading level 1". |
| NAT3 10 s idle `/counter` | Pass | **0** speech records. |
| NAT3 10 s idle specimen grid | Pass | **0** speech records. The live-only EventStore fresh-row lane is cited, not closed. |
| NAT4 traversal `/home`, `/counter` at 100% | Pass | Wrapped through 7 browser-chrome stops: 14 and 31 content stops; every stop has non-empty speech, no identical duplicate, a rectangle inside the viewport, a hit at all four inset corners and no horizontal overflow. |
| NAT4 traversal scenario at 100% | **Fail** | 120-stop cap; **42** failing stops, all in the two generated grids (finding D1). |
| NAT4 actual 400% zoom | Pass | As above. |
| NAT4 traversal `/home`, `/counter` at 400%, with and without spacing | Pass | Wrapped: 9 and 25 content stops in each spacing mode, all criteria met. |
| NAT4 traversal scenario at 400%, with and without spacing | **Fail** each | Wrapped at 112 content stops; **15** failing stops each, all in grid cells (finding D2). |
| NAT4 overlays at 400%, with and without spacing | **Fail** each | Settings and drawer met the criteria; the palette open generated an identical duplicate context (finding D3). All three returned focus to their trigger. |
| NAT4 zoom reset | Pass | 1280 / DPR 1.5 restored. |

Native Tab traversal checks are simplified geometry checks: focused-rectangle containment, a top-element hit at four corners inset by the border radius, and document horizontal overflow. They do not replace the canonical `focusGeometry` helper, target inventory or contrast proof.

**Focused rerun.** The one focused rerun of the five failing checks reproduced all five (2 passed / 5 failed: zoom and reset passed), with failing-stop sets identical to the canonical run. Independent Linux Playwright diagnostics without NVDA (below) reproduce D1, D2, the D3 focus sequence and O1, so the runner is not their cause.

### Reproduced findings for human direction (no product change made)

- **D1. Firefox desktop generated grids.** At 1280 CSS pixels both generated DataGrids lay out at Gecko's maximum coordinate: table width **17,895,614 px**, each data `th` **4,473,910 px**, the labelled region's `scrollWidth` 17,895,622. Only the Id column is visible. Focusing an expand button scrolls the region to 17,894,444 px and shows empty rows (button at x 1314–1338, outside the 76–1252 scrollport). Chromium 156 lays out the same table at 1160 px. The `th` cells carry class `fc-projection-grid` and compute `min-width: max-content; max-width: 100%` from `fc-projection.css:186` (`.fc-projection-grid { max-inline-size: 100%; min-inline-size: max-content; }`, present since `b9e13697`) inside Fluent's `table-layout: fixed; width: 100%`. That rule is the likely trigger; the cause is not proven. Reproduced on Windows Firefox 155 (canonical and rerun) and Linux Firefox 157. The phone-tier `table-layout: auto` override does not apply at Desktop. How the approved 1280-pixel Firefox specimen baselines relate to this layout was not assessed.
- **D2. Phone-tier badge grid, lower rows.** At actual 400% zoom, native Tab onto cells in rows 5–7 leaves them clipped by the grid scrollport or below the 225-pixel viewport. In Linux Firefox 157 and Chromium 156 at a 320×225 *reflow* viewport the region stays at `scrollTop` 29 of a 137-pixel range while rows 6–7 hold focus, and the element at their centers is not the cell. The same fail-closed rule also rejects the 800–996-pixel counter Metadata cell, which cannot fit a 320-pixel viewport.
- **D3. Palette open duplicate context.** Opening the palette with native Enter generates "Command palette dialog", "Search results list" and "COMMANDS grouping" twice before the active option, in both zoom variants and the rerun. The earlier `b105` log shows the same pattern at 100%. A Linux Firefox focus trace shows focus entering the native dialog, then the search input 11 ms later, after its combobox attributes are assigned.
- **O1. Density announcement on every full load.** Each full page load generates "Density set to Compact." without any operator change: **18** records in the canonical log, **14** and **12** in two earlier native logs. Linux Firefox and Chromium populate the same polite region on `/counter` and `/home` with no stored density. `FcDensityAnnouncer` documents skipping first-render announcements. This bears on boxes 15–16; it is not a NAT3 criterion, which concerns the earlier outcome text.
- **O2. Intermediate return focus.** Closing the palette, Settings or drawer generates "Skip to content link" before the trigger, at 400% here and at 100% in the earlier log. Final focus is correct.
- **O3. Second banner landmark.** Firefox exposes the Home `<header>`, inside `div role="main"`, as a banner landmark: "banner landmark Welcome back, FrontComposer specimen user. heading level 1".
- **O4. Unnamed drawer.** The drawer dialog opens as "dialog" with no name.
- **O5. Unnamed scroll container.** At desktop width Firefox makes the scrollable shell content container a Tab stop on `/counter` and the scenario page. It has no role or name, and NVDA generates its entire content as one record (1,526 characters on `/counter`).

Diagnostics (Linux, `tests/e2e` Playwright 1.64.1, no NVDA) live in `/tmp/claude-1000/-home-administrator-projects-hexalith-frontcomposer/d5dfd614-82d8-474d-97ad-7501244cce1d/scratchpad/nat-Gmn4/explore/`: `diag-grid-narrow.json` (`c7c54de96d06b99abc2fdb861bb65af312e96de7c94586246432940c662e8c1d`), `diag-palette-focus.json` (`625023d02daca599f7727ecd10bf887a3a3bf06f1b837344338f336d218aa04b`), `diag-density.json` (`0079e068a698d47418c3d999472e32d1a730bd2ac5291597dc7072aa4e7fb527`), and grid captures `firefox-badge-grid-1280.png` (`2f2c0aca30ed787659cb58eaa95cc384d74b1be6776f53994b3c09f238fed6da`), `firefox-badge-grid-1280-expand-focused.png` (`a8e6fedbdcc4c2d91585430d6299b25d4f028e4d9870bc9ef0446254b2aa3ac0`) and `chromium-badge-grid-1280.png` (`a67b7e7a4af66952afac17563b4dfb396a59f5a18be550f97cc567d686fa5f39`).

**Retained attempts.** Runner development: `dev-1-nat1a` 1 passed / 0 failed; `dev-2-nat4-smoke` 3 passed / 2 failed. D3 first surfaced in `dev-2-nat4-smoke`. Setup probes without NVDA: `probe-window-zoom.json` found 9 presses gave only DPR 2.857, and `probe-zoom-steps.json` recorded Firefox's finer steps. All stay in the Windows directory; per-check speech slices are under `canonical\slices\` and `focused-rerun\slices\`, and traversal stop records under `*\traversal-*.json`.

**Cleanup.** Each run recorded NVDA count **0**, owned Firefox and driver PIDs absent, geckodriver count 0 and port 9516 free. The host was stopped afterwards and port 5070 is closed. The host log (SHA-256 `d94652343bd3759a61383f91c0d334b91430acfb80f3e80152d20cbb07067f21`) retains tenant-mismatch and same-source specimen diagnostics, plus one circuit-disposal `JSDisconnectedException` on a navigation, with no correlated failed check. No clean-log or full-topology claim is made.

### Box map — eighteen itemized boxes, all still unchecked

| Box | Coverage on this workstation | Evidence and remaining gap |
| --- | --- | --- |
| Common 1 — operator journey | Automated native, partial | NAT4 traversal of skip links, hamburger/drawer, rail, theme/palette/settings/account, Home cards, `/counter` tabs, accordions and forms; NAT3 route headings. Toolbar, projection states, destructive dialog and abandonment guard: automated browser only (`accessibility-state-matrix.spec.ts` active-toolbar and validation/policy/destructive/abandonment states). JAWS and VoiceOver pairings not performable here. |
| Common 2 — 320 px and actual 400% | Automated native, **failing** | NAT4 actual Firefox 400% (D1 at desktop, D2 at 400%); actual Chromium 400% 32/32 in the preceding section; 320 px reflow in `responsive-accessibility.spec.ts`. |
| Common 3 — text spacing | Automated native, partial | NAT4 400%-spaced traversals; row growth and truthful virtualization: browser only (`responsive-accessibility.spec.ts`). |
| Common 4 — target inventory | Automated browser only | Target inventory and exceptions in `responsive-accessibility.spec.ts`; not measured natively. |
| Common 5 — keyboard stops unobscured | Automated native (simplified), **failing** | NAT4 corner hit tests (D1, D2); ring visibility and modal containment: browser only (`focusGeometry`, keyboard-cycle journeys). O2 is relevant. |
| Common 6 — themes, forced colors, contrast | Automated browser only | State matrix (116 states per engine), Chromium/Firefox system palette; accepted WebKit limitation. |
| Common 7 — reduced motion | Automated browser only | Reduced-motion fixtures in `responsive-accessibility.spec.ts`. |
| Missing-item 1 | Automated native | NAT1a. |
| Missing-item 2 | Automated native | NAT1b. |
| Missing-item 3 | Automated native | NAT1c (native Tab, tenant, user, navigation; filter-hidden Clear filter). |
| Zero-field 1 — outcome matrix | Automated native | Eleven outcomes 13/13 in the preceding section (identical product bytes); narrow/zoom wrapping: browser only (`generated zero-field wrapped feedback clears the complete trigger focus ring`). |
| Zero-field 2 — persistence and removal | Automated native, partial | NAT2 idle, re-render, navigation, new attempt; scope-change removal: .NET `ShellScopeRemountClearsFeedbackAndCancelsHeldPriorScopeDispatch`. |
| Zero-field 3 — accepted command | Automated native | NAT2 zero-field `accepted` and `/counter` Batch Increment. |
| Direct speech 1 — owners | Automated native, partial | NAT3 route and blocked headings, NAT1a focused non-live text, NAT2 polite lifecycle, palette option once in the preceding section; projection connection status: browser only (`projection-command-announcements.spec.ts`). D3 is relevant. |
| Direct speech 2 — silent restore, new message | Automated native, partial | NAT2 navigation and NAT3 away/back, reload and new command; O1 is relevant. |
| Direct speech 3 — silent ticks and fresh cues | Automated native, partial | NAT3 10 s idle windows, NAT1b repeated refreshes; fresh-cue rules: browser lanes; live-only EventStore lane cited, not closed. |
| Physical tablet | Not performable here | No connected device. |
| Physical phone | Not performable here | No connected device. |

These automated results supply no human candidate-specific observation. Historical MAN-136 reports and the waiver, the frozen criteria, the original unchecked acceptance tasks and Story/sprint **in-progress** remain unchanged. Only this result and the primary spec change; no product, test, sample, dependency, baseline, CI or gitlink file changed.

## Current committed-source local browser and native verification — 2026-11-03

The full primary spec and both frontmatter context files were loaded. The initially clean tested root is `b105c846c31844441f1cff84b5c187da0eff3546`; all specified corrections through GEO1–GEO4 are committed and no additional product failure was demonstrated. Its `git diff --binary fbe01c913cb863abb0fd4a317d370d36fff593b3 HEAD -- src tests` SHA-256 is `4e50ecd68e8b45f4f4db9feec43bc62a0331eafd474897d20f6e1efd5db46f0e`, matching the final GEO source. Six selections (Builds, EventStore, Memories, Parties, Platform and Tenants) have advanced since that earlier proof. Fresh results below bind the rebuilt current runtime; earlier results keep their original candidates. Only this result and the primary spec change.

| Current local lane | Outcome |
| --- | --- |
| Complete three-spec Chromium selection | **375 passed**, zero skipped/failed/flaky/errors/retries |
| Complete three-spec Firefox selection | **373 passed / two named skips**, zero failed/flaky/errors/retries |
| Complete three-spec WebKit selection | **373 passed / two named skips**, zero failed/flaky/errors/retries |
| Browser aggregate | **1,121 passed / four named skips**, 1,125 total |
| Current-run state evidence / artifact validators | **116 states per engine** / all three exit **0** |
| Visual comparisons, included above | **18/18**; all **69** tracked PNG/Verify baselines unchanged |
| Configured individual Debug Shell suite | **3,899 passed**, zero failed/skipped/errors/not-run |
| Shell.Tests Debug build, including Counter | Zero warnings/errors |
| Fresh actual native Chromium 400% zoom | **32/32** |
| Fresh native Windows NVDA/Firefox | **13/13** |
| Linux Node evidence / TypeScript | **29/29** / exit **0** |

Firefox skips `review native-plane circular corner proof rejects supported bevel paint` and `story 11.5 scoped Fluent-root visual hooks are reachable in normal and reduced motion`. WebKit skips `review clipping preserves visible competing paint: overflow clip margin` and that same Chromium-specific motion pin. WebKit forced-color checks execute and retain media-only attribution where actual palette replacement is unsupported.

Exact root commands:

```text
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1 -v:q
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-b105c846-188r1aqs/shell-configured.xml
node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
npm --prefix tests/e2e run typecheck
```

From `tests/e2e`, once per `<engine>` (`chromium`, `firefox`, `webkit`):

```text
BASE_URL=http://127.0.0.1:5070 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-b105c846-188r1aqs/<engine> FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-b105c846-188r1aqs/<engine>-html FC_A11Y_EVIDENCE_ENGINE=<engine> FC_A11Y_EVIDENCE_RUN_ID=story13-6-b105c846-<engine>-20261103 npx --no -- playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=<engine> --workers=2 --retries=0
```

Each was followed by `node scripts/validate-a11y-artifacts.mjs` in the **same engine/run/output environment**. Exact expanded argv, environment, cwd, durations, exits and log hashes are in `/tmp/fc-story13-6-b105c846-188r1aqs/commands.jsonl`. The executed coordinator is `python3 /tmp/fc-story13-6-b105c846-188r1aqs/run-browsers.py`. All three lanes pass on their first execution in this continuation.

| Report | JSON SHA-256 | Results / HTML bytes |
| --- | --- | --- |
| Chromium | `c2280c1b4bb2d75f83b51c34662705d682fa6c5ed30f6b12be37e584b7cdfea9` | 14,322,187 / 8,022,929 |
| Firefox | `447cd592e662ee38099a01cec4a7194021d9a710164ff7a2f90012d78cca4c06` | 15,537,288 / 8,458,342 |
| WebKit | `a916ba800cdee0ede3d07b101695d1ddced52f9fee09dd896d24483d22982f02` | 12,754,823 / 7,029,432 |

All six roots stay below the unchanged **52,428,800-byte** cap. The configured toolchain is Playwright **1.64.1**. The headed Chromium executable reports Google Chrome for Testing **156.0.8078.4**; Linux Firefox reports **157.0**, separate from the Windows pairing below.

Fresh native zoom command, from `tests/e2e`: `xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-story13-6-b105c846-188r1aqs/native-zoom/native-zoom.mjs`. The inspected prior runner has SHA-256 `f43cb2981c84107652efe070854ad3ad1462e7745b53029cd88aec9e627a466e`. Its copy changes only the result output path, yielding script SHA-256 `9745ab562dac5670ae4011a960f444f52db4f5d27724cc83e894c188c09abce9`; actions, timings, thresholds and direct current-helper imports are unchanged. Native tabs zoom verifies **1280×900 / DPR1 → 320×225 / DPR4**, outer width **1280**, visual scale/CSS zoom **1**. All **32/32** pass, including the original 28 operations and four grid-focus light/dark × default/spaced cases. Result SHA-256 is `6ba78e918a64c258d79d46859c971c96377bae3d5ecae4a63ebc97517ba78885`.

Fresh native AT command, from the repository root:

```text
node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-b105-nvda-yq_55n78\native-current.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-story13-6-b105-nvda-yq_55n78'
```

Inventory verifies Windows OS **10.0.26300.0**, **NVDA 2026.2** and **Firefox 155.0.1**. The fresh owned profile/log combines the prior full runner with the already recorded focused-runner setup corrections: startup reactivation, DOM fixture selection, and explicit trigger focus followed by native Shift+Tab/Tab. Command activation and subsequent AT interaction use native Windows keys. Runner SHA-256 is `3a9c73dccc83f25f8b8c3ca05be20c9f38fc160df71f356f9376843ddaf8822c`. All **13/13** pass in this single run: eleven zero-field outcomes each generate one NVDA outcome speech record, retain trigger focus and remain silent during the measured **1.5-second** idle window; Settings exposes radio name/checked state and returns focus; the palette generates its selected option exactly once and returns focus. Summary SHA-256 is `f3d99a60f42478981ed03e7573d1b9844a26655e06937ddc5982623bbf42d9ce`. Raw speech/driver/profile artifacts stay in that external Windows directory. These are automated native AT observations, without a human listening claim.

The preflight finds no existing NVDA session or port-9516 listener. Cleanup verifies NVDA count **0**, both owned driver/Firefox PIDs absent and port 9516 free. The queried standard JAWS directory is absent; no connected macOS/VoiceOver or physical tablet/phone execution capability is available. No corresponding observation is added.

The exact-target Aspire Test baseline started with `ASPIRE_ENVIRONMENT=Test aspire start --no-build --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json -- --environment Test`. `aspire wait counter-web --timeout 45 --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` and exact-target describe confirm Counter Healthy; security is Unhealthy and dependent applications wait. The owned AppHost stopped before building. The standalone host command is `dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070`, with `ASPNETCORE_ENVIRONMENT=Test`, `Hexalith__FrontComposer__Specimens__Enabled=true`, `Hexalith__Shell__FormAbandonmentThresholdSeconds=5`, `Hexalith__Shell__TimeoutActionThresholdMs=5000`, `Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000`, `Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500` and `Logging__LogLevel__Default=Warning`.

The host log retains specimen outcome diagnostics and one `FrontComposerShell[13200]` startup `TaskCanceledException` during JS interop, without a correlated failed check. Clean-log/full-topology qualification is not claimed. All owned sessions are stopped and port **5070 is closed**; the unrelated Folders AppHost is preserved. The Shell suite's generated Pact handoff was archived externally and its tracked content/CRLF restored.

This closes the missing **local post-GEO complete three-spec/state-artifact and configured individual Shell verification**, with fresh current-runtime automated native results. Full-solution .NET/SourceTools, hosted Quality and full-topology evidence retain their earlier source attribution; no such gate was rerun or inferred. Final `python3 eng/validate-story-artifacts.py` and `git diff --check` pass. No remote operation, source/test/dependency/policy/gitlink/baseline change, staging, commit or push occurred; no evidence packet, manifest or runtime-identity record is committed.

The user delegated the available checks, supplying no human observation or waiver. Current-candidate human evidence, applicable changed-cue WebKit capability acceptance and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. The seven historical MAN-136 Owner-reported Pass records/waiver, eighteen unreported itemized boxes, original frozen criteria/baseline and unchecked acceptance tasks are preserved. Story/sprint remain **in-progress**.

## Owned-border geometry and unobscured grid focus — 2026-11-03

The full primary spec and both frontmatter context files were loaded. Current local candidate is committed root `fbe01c913cb863abb0fd4a317d370d36fff593b3` plus `git diff --binary HEAD -- src tests` SHA-256 `4e50ecd68e8b45f4f4db9feec43bc62a0331eafd474897d20f6e1efd5db46f0e`. [GEO1–GEO4](spec-13-6-verify-responsive-and-assistive-accessibility.md#remaining-owned-border-focus-geometry--2026-11-03) close the demonstrated engineering gap; original primary acceptance boxes and Story/sprint **in-progress** remain unchanged.

The helper now proves only a labelled generated grid's owned sticky table-header **border** is disjoint from one solid rectangular focus outline. It uses the outline's actual inner edge; genuine overlapping borders and unrelated interior/exterior paint still fail. Nested grids, rounded indicators, transformed headers, shadows, pseudos and unsupported composition retain the conservative pixel-evidence path. This is no general descendant/sticky exemption. The restored real-grid check exposed an actual missing right focus stroke in Firefox. The grid retains its 2px Fluent stroke at offset zero, with stable `--spacingVerticalXXS` (2px) top clearance funded from the existing 8px top padding. Content positions, normal flow, scroll-padding, row metrics, the 1px informative cue and tight row-link inset are preserved. Both exact old grid-offset assertions were updated; their geometry/contrast checks remain.

Final results on that source:

| Verification | Authoritative outcome |
| --- | --- |
| Complete responsive and specimen specs, Chromium | **270 passed**, zero skipped/failed/flaky; no retries |
| Same two specs, Firefox | **268 passed / two named skips**, zero failed/flaky; no retries |
| Same two specs, WebKit | **269 passed / one named skip**, zero failed/flaky; no retries |
| Aggregate | **807 passed / three skipped**, 810 total; all reports have zero runner errors |
| GEO / real-grid / narrow shared-asset selection, included above | **42/42**; 24 theme/media focus records have zero geometry failures, contrast at least **14.5801:1**, and unchanged layout before/after focus |
| Approved visual comparisons, included above | **18/18**; PNG bytes and thresholds unchanged |
| Actual native Chromium 400% zoom | **32/32**: original 28 operations plus four direct grid-focus cases, light/dark × default/spaced |
| Debug Counter build / focused Fluent conformance / TypeScript | Zero warnings/errors / **39/39** / exit **0** |

The Firefox skips are `review native-plane circular corner proof rejects supported bevel paint` and `story 11.5 scoped Fluent-root visual hooks are reachable in normal and reduced motion`. WebKit skips only the latter, Chromium-serialized motion pin; its bevel case executes. These skips do not stand for WebKit palette replacement. Current forced-color/media checks still distinguish actual Chromium/Firefox system paint from WebKit media-only behavior; no new WebKit system-paint pass or renewed owner disposition is inferred.

Exact browser command, from `tests/e2e`, once per engine: `BASE_URL=http://127.0.0.1:5070 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-fbe01c91-mqk1oksn/geo-funded-responsive-visual/<engine> FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-fbe01c91-mqk1oksn/geo-funded-responsive-visual/<engine>-html FC_A11Y_EVIDENCE_ENGINE=<engine> FC_A11Y_EVIDENCE_RUN_ID=<recorded-run-id> npx --no -- playwright test specs/responsive-accessibility.spec.ts specs/specimen-accessibility.spec.ts --project=<engine> --workers=4 --retries=0`. Exact expanded environment/run IDs, cwd, argv and exits are in that directory's `browser-commands.jsonl`. JSON report SHA-256: Chromium `5f17291bec871a2b797becbb1cfbcdd59a08fe23d1189bd83e2f11ae515d14ad`; Firefox `1dd451124f5c5cdbd863e538e1a485f3044ee1e723707da225f206ce3c8cc902`; WebKit `2a1703cba896bd7e0e6cdff5f0f2a4306ef6d493f921cc9601df327db760a004`.

The owned Debug Test host uses the documented fixture environment (specimens enabled, abandonment 5s, timeout action 5000ms, reconnect notice 30000ms, confirmation delay 6500ms): `dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070`. Builds used `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q`; Shell.Tests also built Debug with the same flags before the styling refinements, with zero warnings/errors. Final conformance: `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FluentConformanceTests' -parallel none`. TypeScript: `npm --prefix tests/e2e run typecheck`. Pre-edit Node evidence passed 29/29 on the committed base; it is separate from current browser proof.

The native command, from `tests/e2e`, is `xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-story13-6-fbe01c91-mqk1oksn/geo-native-zoom-synchronized/native-zoom.mjs`. It imports the **current repository `.ts` helpers directly**. The original 28 actions/timings/thresholds are unchanged; binding refreshes use a fresh output and assert/record the actual reference metrics. Four added cases directly verify the changed contour, with matching fixture/shell themes, label/token synchronization, geometry and contrast. Reference **1280×900 / DPR 1** becomes **320×225 / DPR 4** through Chromium's native tabs zoom API; outer width remains 1280 and visual scale/CSS zoom remain 1. Script SHA-256 is `f43cb2981c84107652efe070854ad3ad1462e7745b53029cd88aec9e627a466e`; result JSON SHA-256 is `e83b4e1111a7c4ef84b1faa148df270674ec7fbf14e2ce71d02a30390f1db716`. This is automated zoom, not human zoom/AT/device evidence.

All failed/interrupted attempts remain in local scratch, without substitution for a current pass:

| Retained attempt | Outcome and disposition |
| --- | --- |
| Committed-source full three-file baseline | Chromium **365/365**, artifact validator exit 0; Firefox **85 passed / two interrupted / 278 not run** when stopped to address GEO; WebKit not started. This precedes the correction. |
| `geo-red/`, then `geo-red-corrected/` | Anchored grep initially selected no tests; corrected selection gives **15 passed / three expected owned-border failures** on the unchanged helper. |
| `geo-focused/` | **19 passed / five failed**: a sticky-position fixture did not reach its intended ring overlap; the restored Firefox check exposed actual overlap. The corrected negative asserts its native bounding-box precondition. |
| `geo-final/` | **29 passed / four failed**; the proposed scrollbar-gutter assumption was invalid on Firefox/WebKit and was removed. No gutter exemption remains. |
| `geo-corrected/`, `geo-zero-offset/` | **30/36**, then **34/36**; outside-contour checks reached preceding count-text overlap. Stacking diagnostics did not fix the missing inset stroke. |
| `geo-spacing/`, `geo-all-focused/` | **36/36**, then **42/42** before padding compensation; these are superseded for final-source attribution. |
| `geo-responsive-visual/`, `geo-final-responsive-visual/` | Agent-stopped Chromium attempts: **254 passed / two stale-offset-pin failures / four interrupted / ten not run**, then **77 passed / two stale-pin failures / four interrupted / 187 not run**. An unused local also failed TypeScript with TS6133 and was removed before final qualification. Interrupted JSON may be incomplete; logs remain diagnostic only. |
| `geo-verified-responsive-visual/` | Chromium **264 passed / six 4px-height visual failures**; Firefox stopped during its visual phase. Funding clearance from existing padding resolves the layout delta without changing baselines. |
| `geo-native-zoom/` | **30/32**: all original 28 map cases pass; two added dark cases had mismatched fixture/shell themes. Their theme preconditions were corrected, and the distinct synchronized run is **32/32**. No product color correction was inferred. |

Before edits, the exact-target Aspire Test baseline started with `ASPIRE_ENVIRONMENT=Test aspire start --no-build --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json -- --environment Test`; `aspire wait counter-web --timeout 45 --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` and exact-target describe succeeded. The owned AppHost stopped before edits/builds. This establishes Counter readiness, not full-topology acceptance. Subsequent owned Test/native sessions stopped and port 5070 is closed.

Final `python3 eng/validate-story-artifacts.py`, `npm --prefix tests/e2e run typecheck` and `git diff --check` pass. An additional scoped check, `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md --base fbe01c913cb863abb0fd4a317d370d36fff593b3`, exits **1** because the historical monolithic record contains older checked tasks outside recognized sections and their paths are absent from the current delta. Its full diagnostic is retained in `/tmp/fc-story13-6-fbe01c91-mqk1oksn/story-current.log`; current GEO headings/File List are reconciled without changing legacy content or validator policy.

This final lane runs **two** browser specs. It does not claim a post-correction full three-file Story 13.6/state-artifact-validator or configured full .NET pass. Prior hosted/full-topology/native-AT records keep their original source attribution; hosted status was not queried under the active no-remote-operations workflow. Current-candidate human observations, independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions and applicable WebKit capability acceptance remain open. Historical MAN-136 Owner-reported Pass records/waiver, eighteen unreported itemized boxes and original frozen criteria are preserved. No staging, commit, push, dependency/policy/gitlink change or committed evidence packet was made.

## Informative reconciliation outline correction — 2026-11-03

The user selected the informative-outline recommendation with **“apply recommended”**. The [bounded implementation record](spec-13-6-make-reconciliation-outline-perceptible.md) applies that decision on base `f842711d67004200e3248b3c42b7d9bcb2146907` plus source/test patch SHA-256 `0536a29c4130ae950f3468ceaed596a0f614bb67910d011f7bff0573ee1d0737`. The grid now uses an opaque accessible Fluent stroke; the 700ms duration, static reduced-motion cue and stronger 2px keyboard focus remain. Supported forced colors use CanvasText. No component, setting, timer or announcement was added.

The unchanged product's corrected viewport probe fails at **1.7875:1 light / 1.4269:1 dark**. The fixed real-grid regression and existing contrast/motion journey pass **12/12** across Chromium/Firefox/WebKit, zero failures/skips/flaky cases/retries. Across 36 measurements, authored contrast is at least **4.5437:1 light / 7.0463:1 dark**; supported system-palette paint is **21:1**. Opacity, both flash endpoints, a single 700ms iteration with zero delay/no fill, and four media combinations are asserted. The grid itself must use automatic system adjustment and resolve its stroke to CanvasText before system paint is claimed. Twenty-four focused-outline measurements meet 3:1 and preserve the 2px priority ring. Debug Counter build has zero warnings/errors; TypeScript exits 0. Exact commands, diagnostic distinctions and raw paths are in the supporting record; report SHA-256 is `d73099661deabf701e27cc9322905ee151dc213f8228d145ba616081c7e6b150`.

The forced-color cue change triggers capability revalidation. New tests remeasure the changed cue, including actual system replacement on Chromium/Firefox and continued media-only behavior on local WebKit. WebKit authored contrast passes, but no WebKit system-palette paint or broader renewed owner disposition is inferred. Independent capability/platform acceptance retains its separate scope. The owned Test host stopped and port 5070 is closed.

The independent review strengthened target-specific system-paint, focused-contrast and bounded-animation assertions. An intermediate visibility check failed conservatively on owned sticky-header borders; the same failure reproduces with the old CSS. Old/new focused screenshots are byte-identical (SHA-256 `41f9c4311f7b73c58346d04103c7accbff4f867d43d97d144c8af2e030e8eee9`) and were visually inspected by the assistant; this is not a human AT observation. The pre-existing measurement limitation is deferred explicitly; no generic focus-geometry pass is claimed. Final results are under `/tmp/fc-13-6-outline-4x7rv22s/final/`, with exact commands and all six review dispositions in the supporting record.

This resolves the engineering outline-contrast finding locally under the user's functional decision; independent review is recorded in the supporting record. Existing hosted CI/Quality links below retain their earlier committed-source attribution and do not qualify this new patch. Human observations, current-patch hosted qualification and independent owner decisions remain open. Historical manual reports/waiver, frozen primary criteria, original incomplete tasks and Story/sprint **in-progress** are preserved.

## Delegated native AT and actual-zoom verification — 2026-11-03

The user delegated execution of the remaining available checks to the assistant and requested a pragmatic comparison of the outline options. Product/test source remains `f842711d67004200e3248b3c42b7d9bcb2146907`; the earlier exact-candidate [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559060371) and [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559059800) results below retain their scope. No product, test, dependency or frozen criterion changed.

The Counter sample was rebuilt with `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q`: exit **0**, zero warnings/errors. It ran with `ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true Hexalith__Shell__FormAbandonmentThresholdSeconds=5 Hexalith__Shell__TimeoutActionThresholdMs=5000 Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 Logging__LogLevel__Default=Warning dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070`.

| Command from the repository root | Result |
| --- | --- |
| `node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-native13-6-current-i_phfalw\native-current.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-native13-6-current-i_phfalw'` | **11 passed / two failed**. The first failure refused to send keys because the owned Firefox was not foreground; the second detected incorrect fixture-scenario selection before command activation. Settings, palette and nine zero-field outcomes passed. |
| `node.exe 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-native13-6-focused-0wr79v2x\native-focused.cjs' 'C:\Users\JeromePiquot\AppData\Local\Temp\fc-native13-6-focused-0wr79v2x'` | **Two passed / zero failed** for validation/unavailable. Fixture selection now uses a DOM click and explicit trigger focus/Tab synchronization; tested command activation still uses native Windows keys and real NVDA output. |
| `xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-nativezoom13-6-current-xogkeyci/native-zoom.mjs` | **27 passed / one failed**: `toolbar-operations-spaced` did not retain Activity-tab focus after ArrowRight. |
| Same Xvfb command with `FC_ZOOM_ONLY=toolbar-operations-spaced`, scripts in `/tmp/fc-nativezoom13-6-tabs-1-zxk27l4e/` and `/tmp/fc-nativezoom13-6-tabs-2-3i75nmhu/` | **One passed / zero failed each**, independent focused executions. |
| `xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-nativezoom13-6-confirm-e7dp5yqf/native-zoom.mjs` | Complete confirmation **28 passed / zero failed**. Only focus-event diagnostics were added; actions, requirements and timings are unchanged. |

Native Windows inventory confirms Firefox **155.0.1**, NVDA **2026.2** and OS version **10.0.26300.0**. Across the two native attempts, all **13 distinct behaviors** have passing executions: eleven fixed zero-field failures, Settings radio name/checked-state/return focus, and palette selected-option speech/return focus. Outcome checks require one generated speech record, retained trigger focus and no repeated outcome in the 1.5-second idle window. These are automated native AT observations, not human listening. JAWS is absent; no macOS/Safari or physical tablet/phone run was performed.

Both zoom runs measured a **1280×900 / DPR 1** reference and actual **400% browser zoom: 320×225 / DPR 4**, unchanged outer width 1280, visual scale 1 and CSS zoom 1. The 28 cases exercise the existing page/overlay/command/detail/lifecycle journeys with and without combined spacing. The initial focus failure remains recorded and unexplained; two focused runs and the complete confirmation did not reproduce it. No product defect or correction is inferred from those reruns. The host also emitted specimen diagnostics and a disposed-service startup exception during the session; this is not a clean-log or full-topology qualification.

Raw output remains in the six scratch directories identified above: two native AT directories and four zoom directories (initial, two focused, confirmation). The four primary directories retain their named summary, script and script hash in `candidate.json`; each focused zoom directory retains `native-zoom.json`, `native-zoom.mjs` and `runner.log`. Summary SHA-256: native initial `27c90d13e1584abfff9fe263851859c79f3d5f0e83c1e0bb9a3f106fb07ff4cb`; native focused `53db9a991322716de90c9d1a74bc0349a4f21761fb6cb306499c69267bdc8ede`; zoom initial `fec4bdad7912dc77ff9ac664f5afb369a15c8c4dcc74e5f2a55e36c405d16565`; zoom confirmation `bf9073b5bf587eb02135a88bf51405c93e1b7229d14fbfee2bfe4b9a69fe37fb`. Owned NVDA/Firefox/driver sessions and the sample host stopped; port 5070 is closed.

**Outline analysis:** retaining an informative outline calls for an opaque, measured Fluent stroke reaching 3:1 in light/dark and supported forced colors, while preserving the 700ms lifetime, static reduced-motion rendering and stronger keyboard focus. `--colorNeutralStrokeAccessible` exists in the selected Fluent package and is a candidate to measure, not a demonstrated contrast pass. Classifying the outline as decorative avoids a visual change but requires UX confirmation that other cues carry all meaningful information, including partial reconnects without the Reconnected notice. Recommendation: the bounded contrast correction, with existing tests and no new component, setting, timer or announcement. The user requested analysis and has not selected either option; no UX approval is inferred.

Delegating execution supplies no human observation or amendment to the frozen criterion. Historical seven MAN-136 Pass records/waiver, eighteen itemized boxes and acceptance tasks are preserved. Current human evidence, the UX outline decision and independent owner gates remain open; Story/sprint stay **in-progress**.

## Current committed-source checks and hosted reconciliation — 2026-11-03

The full primary spec and both frontmatter context files were loaded before this continuation. All specified production/test corrections, including the palette-absence assertion repair, are already committed at initially clean root `f842711d67004200e3248b3c42b7d9bcb2146907`. This root also includes the independent eager-form route-focus correction in `a1a9280b28e291ee5cfaf9b7b6e3b5e4500ae805` and its generator approvals; it is not product/test-byte-identical to the earlier qualified `25139b7f` candidate. No additional product correction was demonstrated by these focused checks. Only the primary spec and this result change here.

Fresh local verification on that committed source:

| Command and working directory | Result |
| --- | --- |
| From the repository root: `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` | **29 passed**, zero failed/skipped/cancelled. |
| From the repository root: `npm --prefix tests/e2e run typecheck` | Exit **0**. |
| From `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-20261103-check/browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-20261103-check/html npx --no -- playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper|default scanner|R4 |R5 |R6 |R7 ' --workers=2 --retries=0` | **108 passed**, 36 per engine; zero failed/skipped/flaky, zero retries. Nine scanner/impact checks and 99 measurement regressions. |

The Playwright command used literal `|` alternatives inside its single-quoted grep argument. The local raw browser log/report remain in `/tmp/fc-story13-6-current-20261103-check/`; `browser/playwright.json` SHA-256 is `bf4898c33b48251848dd5b98671f5c5d499a4b4890dee1a34787b275c632a24d`. These synthetic tests start no application runtime. No new local complete operator/visual/native-zoom/.NET/full-topology result is asserted.

A read-only inspection of already completed hosted runs found the exact current root qualified, superseding the previous section's pending hosted qualification for the palette-absence revision:

| Hosted workflow | Exact run | Recorded result on `f842711d67004200e3248b3c42b7d9bcb2146907` |
| --- | --- | --- |
| Quality | [41559059800](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559059800) | **Completed success**, attempt 1, all eight jobs successful. Linux browser summaries: Chromium **541 passed / four skipped**, Firefox **520 / 25**, WebKit **520 / 25**. Every Linux accessibility/artifact and visual validator step succeeded; the Windows accessibility-visual job and both validators succeeded. |
| CI | [41559060371](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559060371) | **Completed success**; shared build-and-test and dependency-governance successful, optional performance/Aspire jobs skipped by configuration. |
| CodeQL | [41559060437](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559060437) | **Completed success**. |
| Commitlint | [41559060480](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559060480) | **Completed success**. |
| Flaky Test Governance | [41559854973](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559854973) | **Completed success**, workflow-run event. |

The successful read-only commands were `gh run list --commit f842711d67004200e3248b3c42b7d9bcb2146907 --limit 15 --json databaseId,name,headSha,status,conclusion,event,url`; `gh run view <id> --json status,conclusion,headSha,url,jobs` for Quality/CI; `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/41559059800` for the Quality attempt; and `gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/<job>/logs` for Chromium `125665255373`, Firefox `125665255299` and WebKit `125665255366`. JSON/logs are retained in the same external scratch directory. An initial Quality/CI query added unsupported JSON field `attempt` and was rejected with `Unknown JSON field: "attempt"`; no conclusion was taken from those failed queries. The successful JSON SHA-256 values are `2110d71374798483b1a5b27a5fbd22125dcc70eade96e2f4d8abc804a9b4cace` (`hosted-runs.json`), `2119b7fa2e22bf234bbb6f4010b4cc3754ab75f758f99297875a1b193a301129` (`quality.json`), `6e915c83afa41dec0064ddb68676fdcc5d8fff07f87e828e51f8c7c96868f50f` (`quality-run.json`) and `5c801fd725cb4cc941a5252a0b06073ef17c52a7773dff272c580ee3ff872f46` (`ci.json`). No hosted run, rerun, publication or remote mutation was initiated.

This closes the current committed candidate's hosted-qualification gap. Earlier native-zoom, native NVDA, full-topology and manual reports retain their original tested-source attribution; the new hosted result does not renew them as human or current-native evidence. Current-candidate human observations, the UX reconciliation-outline Medium decision and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. The accepted WebKit capability limitation retains its recorded scope and revalidation terms; no actual WebKit system-palette paint is claimed. Seven historical MAN-136 Owner-reported Pass records/waiver, eighteen unreported itemized boxes, original frozen criteria and unchecked acceptance tasks are unchanged. Story 13.6 and sprint remain **in-progress**.

## Palette absence verification repair — 2026-11-03

The [supporting follow-up](spec-13-6-repair-palette-absence-verification.md) repairs the remaining deferred toolbar assertion on base `9724c7e5db5f744366f10e7ee175723cf1ae134c`. `page-toolbar.spec.ts` now waits for shell interactivity, checks the stable `fc-palette-search` test ID and opens/closes the real palette as a positive control, retaining trigger-focus checks. The final test patch is `git diff --binary 9724c7e5db5f744366f10e7ee175723cf1ae134c -- tests/e2e/specs/page-toolbar.spec.ts | sha256sum` = `1fc2568632de51ae36c2e7bd6e7b9390370c5453e9edc4b8990a097fb0b3e092`. Product code and runtime semantics are unchanged. The native palette correction's supporting record now also documents its six hosted locator failures and the subsequent passing follow-up, without altering the original acceptance receipt.

On the Debug Test host built by the required Aspire baseline, the final complete toolbar spec passed **21/21** across Chromium, Firefox and WebKit, with zero failures/skips/flaky cases and no retries. From `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-13-6-palette-absence-Wioczt/browser-final FC_E2E_HTML_REPORT_DIR=/tmp/fc-13-6-palette-absence-Wioczt/browser-final-html npx --no -- playwright test specs/page-toolbar.spec.ts --workers=3 --retries=0`; `npm run typecheck` also exited 0. The initial 21/21 run under `browser/` used patch `42628741103c840c6c27605664f8b7a5cd79e478e78a6bde10df06c427b2732c`, before the review's interactivity wait. A separate real-palette control (`node /tmp/fc-13-6-palette-absence-Wioczt/assertion-control.cjs`) passed **3/3 engines**: the former zero-searchbox assertion passes while open, the replacement zero-count assertion rejects that open state, and the replacement passes after Escape. Raw logs/JSON remain under that local scratch directory. The Aspire observation showed Counter Healthy, security Unhealthy and other applications waiting; it establishes no full-topology success. That owned Aspire session was stopped before the focused Test host started. The supporting spec records the exact standalone host command; both owned hosts are now stopped and port 5070 is closed.

This closes the deferred assertion/annotation work locally. Historical hosted runs below qualify only their recorded test bytes; this changed test needs its own hosted run after publication. Current-candidate human observations, the UX reconciliation-outline decision and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. Historical MAN-136 reports/waiver, eighteen itemized boxes, primary frozen criteria and original incomplete tasks are preserved. Story 13.6 and sprint remain **in-progress**.

## Locator follow-up hosted qualification — 2026-11-03

**Candidate:** `25139b7f59e5ae5465d1665825e24c638928ccb2` (`test(palette): locate native combobox in browser regressions`) is the first commit carrying the locator follow-up named in the [failed-qualification record](#failed-current-candidate-hosted-qualification-and-pending-human-acceptance--2026-11-02), so it is the next qualification candidate. It replaces the six `getByRole('searchbox')` locators in `form-abandonment-guard.spec.ts`, `overlay-initialization.spec.ts` and `route-contract.spec.ts` with `getByTestId('fc-palette-search').locator('input')`. `git diff-tree -r --name-only 9b386d4f0b305c06f2a7b00e949506970fda4ba2 25139b7f59e5ae5465d1665825e24c638928ccb2` lists only those three spec files and three `_bmad-output` documents. Product code, samples, root build inputs and gitlinks are therefore identical to the user-accepted palette correction `9b386d4f`; the tests differ by those six locator lines.

The runs were read with `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/<id>` (head SHA, attempt, conclusion) and `gh run view <id> --json jobs`. Per-engine counts come from each browser job's Playwright summary in `gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/<job>/logs`. Every run is `completed` on attempt 1. The recorded rule applies: a run qualifies only when it is completed and every required job concludes success.

| Hosted workflow | Exact run | Head SHA | Final result |
| --- | --- | --- | --- |
| Quality | [41526310096](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41526310096) | `25139b7f` | **Success**: all eight jobs succeeded — build-and-test, committed-files, debug-gated-tests, Epic 9 live acceptance, Windows accessibility-visual and the Linux browser regressions. Chromium job 125554820007: 541 passed / 4 skipped. Firefox job 125554820101: 520 passed / 25 skipped. WebKit job 125554819981: 519 passed / 25 skipped / **1 flaky**. |
| CI | [41526310723](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41526310723) | `25139b7f` | **Success**: build-and-test and dependency-governance succeeded; aspire-tests and performance-tests were skipped by configuration. |
| CodeQL | [41526310516](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41526310516) | `25139b7f` | **Success**. |
| Commitlint | [41526310636](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41526310636) | `25139b7f` | **Success**. |
| Flaky Test Governance | [41527938278](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41527938278) | `25139b7f` | **Success**, workflow-run event. |

**Qualification result.** Quality 41526310096 satisfies the rule, so hosted qualification of the current candidate is **closed**. The WebKit flaky case is `route-skip-links.spec.ts:196` (`opening a shell dialog preserves its own Tab flow`): `#fc-settings-heading` stayed inactive after the Settings click on the first attempt and retry #1, then passed on retry #2. That test and its Settings focus path are outside both the palette correction and the locator follow-up. It is recorded here rather than counted as a first-attempt pass; no quarantine or flake-policy decision is implied.

**Corroborating documentation-only run.** `bd54321b716f5cd553e930b78f88578367cbeff4` adds only five documents on top of `25139b7f` (`git diff-tree` lists no other path). Its [Quality 41533833435](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41533833435) succeeded on all eight jobs, with Chromium 541 / 4 skipped, Firefox 520 / 25 skipped and WebKit 520 / 25 skipped, and no failed or flaky case. [CI 41533833875](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41533833875), [CodeQL 41533833816](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41533833816), [Commitlint 41533833807](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41533833807) and [Flaky Test Governance 41535141453](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41535141453) also succeeded, all on attempt 1. On identical product and test bytes, the WebKit `route-skip-links.spec.ts:196` case passed on its first attempt.

**Scope.** The native NVDA automated checks and the user's acceptance receipt were bound to `9b386d4f`; `25139b7f` carries identical product bytes, and the receipt itself is unchanged. Hosted success is automated evidence only. It supplies no human observation, no WebKit system-palette paint and no independent owner decision. In the agent's assessment, the accepted WebKit limitation's revalidation trigger has still not fired: the follow-up changes only test locators. `page-toolbar.spec.ts:83` still asserts zero `searchbox` elements and passes vacuously, so this pass proves that `/` on the focused trigger keeps the palette closed only through the preceding trigger `toBeFocused()` check. Repairing that assertion and annotating `spec-13-6-fix-native-palette-option-announcements.md` with the hosted failure remain open in the deferred-work ledger. The [walkthrough](../../docs/accessibility-verification/story-13-6-manual-walkthrough.md) now names `25139b7f` as the current candidate.

**Remaining gates:** human observations, recorded against the commit actually built and served, across the required pairings and devices; the UX-owned reconciliation-outline Medium decision; and independent OI-16, G-4, FLUENT-APP-1, Product and Architect decisions. The deferred `page-toolbar.spec.ts:83` repair and palette-fix annotation are tracked separately. The seven historical `MAN-136-*` Owner-reported Pass records, their waiver and the eighteen unchecked itemized boxes are unchanged. Primary Story 13.6 and sprint row `13-6-i-ux-e-verify-responsive-and-assistive-accessibility` remain **in-progress**. No product code, test, threshold, baseline, dependency, CI, gitlink or manual template changed.

## Failed current-candidate hosted qualification and pending human acceptance — 2026-11-02

**Candidate:** `9b386d4f0b305c06f2a7b00e949506970fda4ba2`, the user-accepted palette correction. `git diff --binary 73ff2f87e430d983661c42674c0e433b4b76045f 9b386d4f0b305c06f2a7b00e949506970fda4ba2 -- src tests ':(exclude)tests/e2e/specs/palette-native-accessibility.spec.ts' | sha256sum` reproduces the recorded corrected-source patch `2b3856a5444f06bc686db4d2a22e751660910c1053a1cfb0387cd99a153ddcfa`. That commit's `tests/e2e/specs/palette-native-accessibility.spec.ts`, checked out under its `eol=crlf` attribute, hashes to the recorded regression `5db5d934b2dd3fdbc467e5c099f85222693422958258f5e02311e2307393b3c3`; `git show 9b386d4f0b305c06f2a7b00e949506970fda4ba2:tests/e2e/specs/palette-native-accessibility.spec.ts | sed 's/$/\r/' | sha256sum` reproduces it, while the committed LF blob hashes to `7b96eeff13d2c4ea4625e479e298446dd57f092d1adddb068e88d6fd52c8d2ec`. Both recipes assume default `git diff`/`git show` configuration (no external diff or textconv). The committed candidate therefore carries the natively verified correction bytes. Pushed HEAD `b547cd80df1dbdb73f6e3538812db38e156f304e` equals `origin/main`; `git diff --quiet 9b386d4f0b305c06f2a7b00e949506970fda4ba2 HEAD -- src tests samples .github eng` exits **0**, and `git diff --stat` shows only three `_bmad-output` documents changed (this result, the palette-fix record and the primary spec). HEAD runs therefore execute the candidate's product/test bytes.

Per the user's **wait and record** decision, the final hosted runs were read with `gh run list --commit <sha> --json databaseId,name,headSha,status,conclusion` and `gh run view <id> --json headSha,status,conclusion,jobs`. Every run below is `completed` on attempt 1 (`gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/<id> --jq .run_attempt`), and its reported head SHA is the commit shown. A run qualifies only when it is completed and every required job concludes success; CI's aspire-tests and performance-tests are optional jobs skipped by configuration. A skipped, timed-out, cancelled or failed required job disqualifies its run.

| Hosted workflow | Exact run | Head SHA | Final result |
| --- | --- | --- | --- |
| CI | [41500720122](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41500720122) | `9b386d4f` | **Success**: build-and-test and dependency-governance succeeded; aspire-tests and performance-tests were skipped by configuration. |
| CodeQL | [41500719770](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41500719770) | `9b386d4f` | **Success**. |
| Commitlint | [41500720020](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41500720020) | `9b386d4f` | **Success**. |
| Flaky Test Governance | [41501942753](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41501942753) | `9b386d4f` | **Success**, workflow-run event. |
| Quality | [41500719150](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41500719150) | `9b386d4f` | **Cancelled**: only committed-files and debug-gated-tests completed (success). Build-and-test, Epic 9 live acceptance, Windows accessibility-visual and the three Linux browser regressions were cancelled, consistent with the workflow's cancel-in-progress group after the HEAD push. Not qualification. |
| CI | [41502328076](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41502328076) | `b547cd80` | **Success**: build-and-test and dependency-governance succeeded; aspire-tests and performance-tests were skipped by configuration. |
| Quality | [41502327525](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41502327525) | `b547cd80` | **Failure**: build-and-test, committed-files, debug-gated-tests, Epic 9 live acceptance and Windows accessibility-visual succeeded. Linux browser regressions failed: Chromium job 125473712753 (535 passed / 6 failed / 4 skipped), Firefox job 125473712329 (514 / 6 / 25) and WebKit job 125473712302 (514 / 6 / 25). In each failed job, both `Run all specs for selected browser engine` and `Validate selected Linux accessibility engine and run` concluded failure. Counts come from each job's Playwright summary log. |
| Commitlint | [41502327939](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41502327939) | `b547cd80` | **Success**. |
| CodeQL | [41502327991](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41502327991) | `b547cd80` | **Success**. |
| Flaky Test Governance | [41504006564](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41504006564) | `b547cd80` | **Success**, workflow-run event. |

**Failed qualification and new blocker.** Each Linux engine fails the same six tests on the first attempt and both configured CI retries: `form-abandonment-guard.spec.ts:95`, `overlay-initialization.spec.ts:50` and `route-contract.spec.ts:43`, `:122`, `:148` and `:304`. Their `getByRole('searchbox')` locators report element(s) not found. In `9b386d4f`'s `fc-keyboard.js`, `sync()` assigns `role="combobox"` when the palette registers, regardless of focus: on the native input where ARIA element reflection exists, otherwise on the `fluent-text-input` host. `page-toolbar.spec.ts:83`'s zero-searchbox assertion therefore now passes vacuously; only its preceding trigger `toBeFocused()` assertion still guards that case. The six failing specs were outside the accepted correction's focused 21-check browser selection; the user's acceptance receipt is unchanged and remains bound to its reported verification. This plan records the failure without fixing it; no test or product file changed. HEAD Quality's successful build-and-test, committed-files, debug-gated-tests, Epic 9 live acceptance and Windows accessibility-visual jobs are job-level evidence only; neither Quality run qualifies. Correcting those locators and the vacuous zero-count assertion is a separate follow-up. Because it must change test bytes, the commit carrying it becomes the next qualification candidate and needs its own successful hosted Quality run. Documentation-only pushes before that follow-up are expected to fail the same six Quality tests. The successful CI, CodeQL, Commitlint and Flaky runs do not substitute for Quality's browser, accessibility-visual or WebKit coverage. The `d1236f9e` hosted successes remain attributed to that earlier source.

**WebKit disposition.** The accepted bounded WebKit capability limitation keeps its recorded scope: no WebKit system-palette paint pass is claimed, and Chromium/Firefox actual-paint proof and every WebKit check remain required. The user's decision named `d1236f9e`; the conclusion that its recorded revalidation trigger has not fired for `9b386d4f` is the agent's assessment under that trigger, not a new user decision. The palette diff adds a listbox `id`, native combobox attributes and cross-shadow ARIA element references; it changes no class, stylesheet or forced-color cue, and the `tests/e2e` Playwright lock is unchanged. The failed hosted WebKit job leaves the WebKit browser checks unqualified for this candidate; it does not alter the capability disposition.

**Host state.** On 2026-11-02, while this record was prepared, no process listened on port 5070 and a request to `http://127.0.0.1:5070/` was refused. The standalone Debug Test host described in earlier sections is no longer running. On request it is rebuilt as a Debug build, as in the earlier preparation command, and started at `http://127.0.0.1:5070` with the `webServer` environment in `tests/e2e/playwright.config.ts` (`ASPNETCORE_ENVIRONMENT=Test`, `Hexalith__FrontComposer__Specimens__Enabled=true` and its other settings). The [walkthrough](../../docs/accessibility-verification/story-13-6-manual-walkthrough.md) now asks resumed human runs to record the commit actually built and served, currently `9b386d4f0b305c06f2a7b00e949506970fda4ba2`.

**Human criterion.** Per the user's **close as pending** choice, the current-candidate human observation criterion is recorded **pending**. No new human observation, tester identity, version set or Pass was supplied. Agent-run native NVDA results remain automated evidence. The seven historical `MAN-136-*` Owner-reported Pass records and the 2026-10-27 supporting-details waiver are unchanged, and the eighteen itemized checklist boxes remain unchecked. Later observations receive their own dated record bound to the commit actually built and served, with date, versions, journeys and outcomes.

**Remaining gates:** human observations, recorded against the commit actually built and served, across the required pairings and devices (Windows Firefox/NVDA, Windows Chrome/JAWS, macOS Safari/VoiceOver, physical tablet and phone fallback, and the applicable zero-field and native-zoom sequences); a successful hosted Quality run on the commit carrying the separate locator follow-up, the next qualification candidate; the UX-owned reconciliation-outline Medium decision; and independent OI-16, G-4, FLUENT-APP-1, Product and Architect decisions. This supporting acceptance plan completes its documentation scope only. Primary Story 13.6 and sprint row `13-6-i-ux-e-verify-responsive-and-assistive-accessibility` remain **in-progress**. No product code, test, threshold, baseline, dependency, CI, gitlink or manual template changed.

## User acceptance of the native palette correction — 2026-11-02

The user replied **“I accept”** after the report of the native NVDA/Firefox correction, successful automated checks and local commit `9b386d4f0b305c06f2a7b00e949506970fda4ba2`. This records the user's acceptance of that implementation and its reported verification: 72 focused .NET tests, 21 browser checks, and the corrected native NVDA selected-result announcement/focus check.

The acceptance is bound to that exact commit and the preceding native verification record. Automated AT findings retain their automated classification. The original candidate-specific human observation requirement remains pending, alongside hosted CI for this commit; the seven historical Owner reports and their existing waiver retain their original scope. Primary Story 13.6 and sprint remain **in-progress**. This receipt supplies no additional independent gate or release-role decision.

## Agent-run native NVDA checks and palette correction — 2026-11-02

At the user's request, the assistant drove **native Windows Firefox 155.0.1 with installed NVDA 2026.2**, using isolated Firefox/NVDA profiles, native Windows keystrokes and NVDA's input/output speech log. Windows reported Windows 11 26H2, build 10.0.26300.9818. These are **automated assistive-technology observations**, not human listening or a new manual Pass.

The initial source matched product candidate `d1236f9e5f21c0a000e1081846a2ea6308a35fd3`. Across bounded runs, **13 distinct checks have passing executions**: all eleven fixed zero-field failure outcomes, a distinct new validation attempt, and Settings radio name/checked-state changes with return focus. Each fixed failure generated its outcome once, retained trigger focus and produced no repeated outcome during a 1.5-second idle observation. The separate first validation probe observed three idle seconds. Settings requires two Escape presses in this NVDA sequence: the first exits NVDA focus mode; the second closes the dialog and restores the trigger. This is recorded behavior, not a one-Escape claim.

The native palette test exposed a real gap: Down Arrow selected `ConfigureCounterCommand`, while the focused shadow input lacked the host's combobox/controlled-listbox/active-option associations and NVDA spoke only the query. The unchanged-source final targeted audit was **1 passed / 1 failed** (Settings passed, palette speech failed). A separately saved **runtime-only** element-reference experiment passed the palette speech check; this was hypothesis evidence, not a shipped fix.

The implemented correction retains FluentTextInput, the existing palette live-status owner and current visual styling. Its existing keyboard module now assigns native combobox semantics and actual `ariaControlsElements`/`ariaActiveDescendantElement` references across the shadow boundary. It clears stale references for empty/replaced results, observes delayed/replaced native inputs and disconnects observers during teardown. Engines without reflection keep the former host compatibility path; that fallback test does not establish corrected speech on those engines.

**Corrected source attribution:** base `73ff2f87e430d983661c42674c0e433b4b76045f` plus tracked `src tests` patch SHA-256 `2b3856a5444f06bc686db4d2a22e751660910c1053a1cfb0387cd99a153ddcfa` and the new browser regression SHA-256 `5db5d934b2dd3fdbc467e5c099f85222693422958258f5e02311e2307393b3c3`. Documentation is outside those hashes. The final Debug sample was rebuilt and restarted before verification.

| Corrected-build check | Result |
| --- | --- |
| Counter Debug build and Shell test-project Debug build, `--no-restore -m:1 -v:q` | Exit 0, zero warnings/errors |
| Direct Shell xUnit: `-class '*FcCommandPaletteTests' -class '*FcPaletteResultListTests' -class '*FluentConformanceTests' -parallelMode none` | **72 passed**, zero failed/skipped |
| Three-engine native-relationship/lifecycle/fallback, existing active toolbar/palette/Settings states, and 320×225 palette spacing/axe checks | **21 passed**, zero failed/skipped/flaky, zero retries |
| Native Firefox/NVDA on the implemented correction, without runtime semantic mutation | **1 passed**: verified native Down Arrow, exactly **one** selected-result speech record, and return focus after two Escape presses |
| Independent Blind Hunter review and fix follow-up | Seven findings dispositioned and addressed; no additional actionable finding in the source/test follow-up |

The browser command from `tests/e2e` was `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-palette-final-browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-palette-final-browser-html npx playwright test specs/palette-native-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --grep 'palette keeps|palette synchronizes|palette retains|active toolbar, tabs, palette and settings states|palette at 320 by 225 reflow' --workers=3 --retries=0`. Its reports remain local. This focused selection does not claim a complete 1,085-case rerun or complete state-artifact validation.

The final native command was `node.exe '\\wsl.localhost\Ubuntu-26.04\tmp\fc-nvda-final-production.cjs' "$(wslpath -w "$(cat /tmp/fc-nvda-final-production-root)")"`. The source script SHA-256 is `ab980a7e24266ec4c653ba217038dc95dbb2f2dd02fa1e85d41f3e61c6449a5b`; final summary SHA-256 `cdd50c03cfa44e23c8387f12b89561b32aa4240bee3b971cdecf5a17b142eaeb`; bounded palette log SHA-256 `a339bba7728dcd76dc516fdc78d722743e33990beb58364c55de369abbf9414b`. `/tmp/fc-nvda-final-production-root` locates the local raw output. Initial source observations are located by `/tmp/fc-nvda-final-root`, `/tmp/fc-nvda-focused-root` and `/tmp/fc-nvda-audit-root`; the runtime proposal by `/tmp/fc-nvda-palette-proposal-root`.

Earlier diagnostics remain separate: the first wide probe was aborted after browse-cursor positioning failures; an intermediate matrix was **11 passed / 3 failed**; its focused follow-up was **1 passed / 2 failed**. NVDA input traces established that early overlay failures received numpad6/numpad2 rather than extended arrow keys. Corrected native key encoding and actual Tab traversal resolved those harness issues. A case-sensitive mode-log comparison was another corrected probe defect. The original Chromium browser regression failed on missing native combobox semantics; the initial focused Shell run was **71 passed / 1 failed** on an old host-markup expectation, then passed after the expectation was updated. None of these failed/interrupted attempts is promoted to passing evidence.

The required Aspire baseline was started with the exact FrontComposer AppHost; its Counter resource was observed Healthy while other resources were still starting/waiting. That owned AppHost was stopped before editing; no complete full-topology health success is claimed. Owned native NVDA/Firefox/driver sessions were closed after the checks. The standalone Debug Test host remains available for the walkthrough.

**Remaining acceptance:** the seven historical Owner reports and waiver are unchanged. Current-candidate human observations, other AT/browser pairings, physical-device observations, a complete hosted qualification of this new patch, and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. Earlier green hosted CI/Quality links apply to `d1236f9e`, not this correction. Primary Story/sprint stay **in-progress**. No manual template, approved PNG, dependency, policy or gitlink changed.

## Accepted WebKit capability disposition and guided manual session — 2026-11-02

The user answered **“1 do recommended”**, explicitly accepting the recommended bounded WebKit platform limitation for product candidate `d1236f9e5f21c0a000e1081846a2ea6308a35fd3`. The open High finding requesting actual system-palette replacement on the recorded unsupported WebKit environments is now **dispositioned — accepted capability limitation for this candidate**. Actual Chromium/Firefox palette proof and every existing WebKit media, semantic, contrast, keyboard and visual check retain their requirements and original attribution. WebKit media-only results are not promoted to actual system-palette paint. This is a story-specific applicability decision; it does not grant broader release or independent owner approvals. Revalidate when WebKit/Playwright/platform replacement capability changes, or when an implementation changes a forced-color cue.

For the separate human criterion, the user answered **“2 I dont know how to do this”** and selected **Windows with Firefox (NVDA)** for guided assistance. No new dated candidate-specific observation, Pass or manual completion is supplied by those statements. The [guided walkthrough](../../docs/accessibility-verification/story-13-6-manual-walkthrough.md) starts with a real person listening to one generated zero-field failure sequence, then covers the remaining checklist and pairings. The seven historical Owner-reported Pass records and waiver remain unchanged. Current-candidate manual acceptance, original incomplete acceptance tasks and Story/sprint **in-progress** status remain open.

Preparation at checkout `73ff2f87e430d983661c42674c0e433b4b76045f`: `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Debug --no-restore -m:1 -v:q` exited **0**, zero warnings/errors. The local Test host started at `http://127.0.0.1:5070` with specimen routes enabled and the existing deterministic outcomes. An HTTP request returned **200** and confirmed the scenario heading, validation selector and **Run accessibility specimen** trigger. These are setup checks, not heard-speech evidence. The server remains running for the interactive guided session. Product/test inputs match the hosted candidate; documentation decisions do not assert new automated suite results.

## Current native zoom operation map and semantic repair — 2026-11-02

Current local source is committed root `712929978917bd00f644d8a7e20c93c0ff75cb3b` plus `git diff --binary HEAD -- src tests | sha256sum` = `3ff6fd26e5922ddde7f8e55de1a0596e7b4fc6e971473925a50058213df86dd7`. The browser-affecting `src tests/e2e` patch digest is `42fbb52cb5e49907f31cae161f7c62e7b6e9fe630e2449c3f6741c4a0f69d6d5`; the separate hamburger bUnit assertion was updated after the Chromium browser run began and does not change that browser source.

A headed Chromium probe with viewport emulation disabled used a temporary Manifest V3 extension's `chrome.tabs.setZoom(4)` in automatic per-tab mode. It reapplied the browser setting after each navigation, since Chrome resets that tab's zoom on navigation. The reference was **1280×900 CSS pixels / DPR 1**; actual 400% browser zoom measured **320×225 CSS pixels / DPR 4**, outer width still **1280**, visual viewport scale **1**, and computed CSS zoom **1**. This distinguishes native browser zoom from the 320px viewport-reflow tests. `xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-zoom-probe.mjs` exited **0: 28 passed / zero failed** against the rebuilt Release Test host. The exact scratch script is SHA-256 `410ab2a8d4a0ed0c3c3c1ac528261958b1c8f902d4694cef28fecce25b82eda3`; its 28-row raw result `/tmp/fc-story13-6-native-zoom-current.json` is SHA-256 `bf9073b5bf587eb02135a88bf51405c93e1b7229d14fbfee2bfe4b9a69fe37fb`.

| Actual 400% zoom journey | Default | Combined text spacing |
| --- | ---: | ---: |
| Home | Pass | Pass |
| Navigation and tabs | Pass | Pass |
| Type specimen | Pass | Pass |
| Data formatting | Pass | Pass |
| Page toolbar | Pass | Pass |
| Command palette | Pass | Pass |
| Settings dialog | Pass | Pass |
| Toolbar search, filter, view menu and tabs | Pass | Pass |
| Navigation drawer, theme menu and account menu | Pass | Pass |
| Zero-field validation | Pass | Pass |
| Projection detail and missing-item recovery | Pass | Pass |
| Lifecycle rejection | Pass | Pass |
| Destructive validation and alertdialog | Pass | Pass |
| Abandonment warning | Pass | Pass |

All 28 rows appear individually in the raw JSON. Each named group is one sequential journey, so a `Pass` covers its listed operations in order; it is not a separate independent result for every operation within that group.

The probe applies the existing text-spacing, clipping, bounded-overflow, target-size and blocking WCAG 2.2 axe helpers. It checks focus on the page routes and palette/validation journeys. For the vertically scrolled toolbar view, theme and account menus, it measures every active menu item directly at **at least 24×24 CSS pixels** and runs the clipping, overflow and axe checks; the global target helper reports offscreen background controls as clipped while those menus are open, so those three menu states do not claim a clean global target inventory. The base page, filter, drawer and other active states retain the full helper inventory. This is automated Chromium evidence, not heard speech or physical-device observation.

The first current-source fourteen-case probe passed **10/14**: palette exposed a serious unnamed listbox and critical invalid listbox child, while Settings exposed critical unsupported ARIA attributes on three Fluent radio hosts. Runtime-only semantic mutations made those cases **14/14**, then the production changes did the same. The palette listbox now has an English/French localized name and its category headings stay visually present while naming their groups without invalid exposed listbox children. Density radios now expose `role=radio` and a checked state that updates with selection. The expanded native map initially passed **25/28**: opening the drawer exposed Fluent's invalid `aria-expanded` on a plain wrapper; the other failure was the global target helper counting vertically scrolled background controls during the view menu. The wrapper now has `role=group` while retaining one named inner button, and the operation probe measures active menu targets directly. The final map is **28/28**. The initial failed logs remain `/tmp/fc-story13-6-native-zoom-extended.log` and the focused diagnostics under `/tmp/fc-story13-6-native-*-diagnostic*.log`.

A browser regression at **320×225 reflow** failed **12/12** before the palette/Settings correction, then the final palette, Settings and open-drawer checks passed **18/18** across Chromium, Firefox and WebKit, with both spacing modes, zero retries. It asserts the named listbox and group, changing density-radio checked states, supported expanded-drawer semantics, no page overflow/text clipping and no blocking WCAG 2.2 finding. The existing focused Shell bUnit selection initially passed **30/33** because three tier cases explicitly asserted the old roleless wrapper; after updating that assertion to require a group with one inner button, it passed **33/33**. `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 -v:q` and `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 -v:q` each exited **0** with zero warnings/errors. `npm --prefix tests/e2e run typecheck` and `git diff --check` exited **0**.

The full configured three-file browser/visual selection ran from `tests/e2e` on the unchanged `src tests/e2e` digest above, with the rebuilt Release Test host and locked Playwright **1.64.1**. For each `engine` in Chromium, Firefox and WebKit, the command was `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_A11Y_EVIDENCE_RUN_ID=fc136-zoom-20261102-$engine FC_A11Y_EVIDENCE_ENGINE=$engine FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-zoom-browser/$engine-results FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-zoom-browser/$engine-html npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=$engine --workers=4 --retries=0`. The configured list, HTML, JUnit and JSON reporters were retained. With the same four environment values for each engine, `node scripts/validate-a11y-artifacts.mjs` exited **0** and reported “Accessibility artifact validation passed.” The per-engine JSON reports are in the corresponding `*-results/playwright.json` paths.

| Engine | Browser result | Artifact validator | JSON unexpected / flaky |
| --- | ---: | --- | ---: |
| Chromium | **363 passed**, zero skipped | Passed | 0 / 0 |
| Firefox | **361 passed / 2 named skips** | Passed | 0 / 0 |
| WebKit | **361 passed / 2 named skips** | Passed | 0 / 0 |

Aggregate: **1,085 passed / zero failed / four named skips / zero flaky**, 1,089 total, with zero retries. Firefox skips its native-bevel paint proof and the Chromium-scoped Story 11.5 hook; WebKit skips overflow-clip-margin and that same hook. The configured visual baselines passed on all engines with approved PNG bytes and thresholds unchanged. These skips do not stand for WebKit actual system-palette proof: its forced-colors media checks execute, while Linux WebKit retains authored paint. The Test host stopped and port 5070 is closed.

Actual WebKit system-palette paint still needs a supported platform. The unchanged manual criterion's required execution details remain unresolved against the Owner's supporting-details waiver: all seven MAN-136 Owner-reported Pass records stay completed and the eighteen itemized boxes remain unreported. Current-candidate hosted qualification and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. Original acceptance tasks and Story/sprint status stay **in-progress**.

### Windows WebKit capability check and manual-criterion reconciliation

The Windows 11 host behind WSL was tested with native Playwright **1.64.1 / WebKit 27.2 (revision 2370)**. `powershell.exe -NoProfile -Command` ran `/tmp/fc-windows-webkit-palette-probe.cjs` under Windows Node 26.1.0: both headless and headed WebKit reported `forced-colors: active` after emulation but kept authored foreground/background/border red/green/blue values. A separate self-restoring native OS probe ran `/tmp/fc-windows-webkit-high-contrast-run.ps1` with Windows high-contrast flags **126 (off) → 127 (on) → 126 (restored)**; the OS Window/WindowText/Highlight colors changed from **16777215/0/14120960** to **2105376/16777215/15786894** and returned to their original values. During native high contrast, both headless and headed WebKit contexts with `forcedColors: null` reported forced-color media **false** and kept the same authored paint. The OS state was restored and verified. Exact stdout is `/tmp/fc-windows-webkit-emulation.log` (SHA-256 `7b218f4cdf4e36676c804e1ab3433779915f408dec0bcca14e70920e08ea48d8`) and `/tmp/fc-windows-webkit-native.log` (SHA-256 `a7455802ed035980f60b39d70620405928a4e8975fcfec15fb5a579476c22015`); the corresponding script hashes are `a39f73df1a96b1472090030a04e1d9e18d7d386bb2f12ba11c1752462314dc72`, `ea377c46784fdd91fdb60dcc8c85f04729917e21846ac88ec8b78cabe5369516` and `b8274a4d52d5523133e7aa476ac2699e34c83e495313497be8f6f85f5de4e0da`. This Windows WebKit runner cannot supply actual system-palette paint evidence. It establishes a browser/platform capability limit, not an application pass or failure. AE-07 still needs a WebKit environment that demonstrably replaces authored colors with system paint, followed by the changed-surface measurements.

The frozen manual matrix requires **dated human observations tied to the candidate and versions**, and the original acceptance criterion requires candidate-valid proof. The seven `MAN-136-*` records remain **completed — Owner-reported Pass; supporting details waived** under the 2026-10-27 amendment. Their reported confirmation was received against checkout `39cab9cb19f15e6d743db63708a1a137df348233`; actual tested build, versions, execution dates and itemized speech/device/zoom observations were not supplied. The waiver expressly covered supporting report details and did not expressly amend the frozen candidate/version criterion. This candidate changes palette, Settings and drawer accessibility semantics after those reports; the existing records themselves name material implementation change as a revalidation trigger. Therefore the seven Pass records remain valid as historical Owner reports, while the **current-candidate manual acceptance criterion remains unproven**. Its resolution requires either new dated candidate-specific human confirmation with the requisite version/observation evidence, or an explicit Owner amendment to that frozen criterion and revalidation scope. On 2026-11-02 the user explicitly selected **keep the frozen criterion open for new observations** when offered a narrow amendment for commit `d1236f9e5f21c0a000e1081846a2ea6308a35fd3`. No new observation, Owner approval or renewed Pass is inferred, and the eighteen itemized boxes remain unchecked.

## Exact-candidate hosted CI results — 2026-11-02

The code/test candidate was committed and pushed as **`d1236f9e5f21c0a000e1081846a2ea6308a35fd3`**. `gh run list --commit d1236f9e5f21c0a000e1081846a2ea6308a35fd3 --limit 30 --json databaseId,name,headSha,status,conclusion,event,createdAt,updatedAt,url` returned the exact head SHA for all five runs below. `gh run view 41472861714 --json status,conclusion,headSha,jobs,url` and the corresponding `41472862527` query show their final jobs. The captured JSON remains local at `/tmp/story-13-6-hosted-runs.json` (SHA-256 `a2cb6c5934bca15aeb4c96e3b94d2def840c4085885e828f54c6c83491b7859d`), `/tmp/story-13-6-hosted-quality.json` (`578471757f316281c29822d91ebec8a499f6e2fee7002b3246cb0a79a1fd6a94`) and `/tmp/story-13-6-hosted-ci.json` (`9e951408b840f0f36f808ae47896512621996aae75a2bfac392feecddfdc871b`).

| Hosted workflow | Exact run | Final result |
| --- | --- | --- |
| CI | [41472862527](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41472862527) | **Success**: shared build-and-test and dependency-governance succeeded; optional performance and Aspire jobs were skipped by configuration. |
| Quality | [41472861714](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41472861714) | **Success**: all eight jobs succeeded — build-and-test, committed-files, debug-gated-tests, Epic 9 live acceptance, Windows accessibility-visual, and Linux Chromium/Firefox/WebKit browser regressions. The Windows accessibility artifact validator and each Linux engine's accessibility and visual validators succeeded. |
| Commitlint | [41472862205](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41472862205) | **Success**. |
| CodeQL | [41472862153](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41472862153) | **Success**. |
| Flaky Test Governance | [41475911648](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41475911648) | **Success**, workflow-run event bound to the same head SHA. |

Quality's Linux WebKit success is a browser/visual result with forced-color media checks; it does **not** provide actual system-palette paint. The hosted candidate also does not supply new dated human observations or separate OI-16/G-4/FLUENT-APP-1/Product/Architect decisions. The detailed 28-row map, Windows capability diagnosis and the user's explicit choice to keep manual acceptance open were recorded after the hosted source/test commit; they are evidence-only documentation additions and do not alter the product/test bytes that ran. Story/sprint remain **in-progress**.

## Current-patch complete browser verification — 2026-11-02

The current local candidate is committed root `bdb4d0cea24cc58b44f1a61f384620def007f986` plus `git diff --binary HEAD -- src tests | sha256sum` = `375a6150e13c92540603297268074747f2d3e6bf462fde30974962de0cea63a2`. With the rebuilt Release Counter Test host and locked Playwright 1.64.1, the full three-file accessibility selection passed on all three engines. The selection was `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project="$engine" --workers=4 --retries=0` from `tests/e2e`, using the repository's configured list, HTML, JUnit and JSON reporters. For each engine, `FC_A11Y_EVIDENCE_RUN_ID=fc136-current-fc-story13-6-current-evidence-MiJVgF-$engine`, `FC_A11Y_EVIDENCE_ENGINE=$engine`, `FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-evidence-MiJVgF/$engine-results` and `FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-evidence-MiJVgF/$engine-html` were set for the browser run and `node scripts/validate-a11y-artifacts.mjs`.

| Engine | Playwright | Artifact validator | JSON stats |
| --- | --- | --- | --- |
| Chromium | Exit 0, **357 passed**, zero skipped | Exit 0, “Accessibility artifact validation passed.” | 357 expected, zero unexpected/flaky |
| Firefox | Exit 0, **355 passed / 2 named skips** | Exit 0, same message | 355 expected, two skipped, zero unexpected/flaky |
| WebKit | Exit 0, **355 passed / 2 named skips** | Exit 0, same message | 355 expected, two skipped, zero unexpected/flaky |

Aggregate: **1,067 passed / zero failed / four named skips / zero flaky**, 1,071 total, with zero retries. Firefox skips the supported bevel-paint proof and the Chromium-scoped Story 11.5 hook; WebKit skips overflow-clip-margin and the same Story 11.5 hook. The earlier diagnostic Chromium run also passed **357/357**, but an explicit `--reporter=dot` suppressed the required HTML/JUnit/JSON artifacts, so its validator exited **1** for missing files. It is not the accepted evidence run. Logs and reports remain local under the directory above. `python3 eng/validate-story-artifacts.py` and `git diff --check` pass; the Playwright-owned Test host stopped and port 5070 is closed.

This current-patch suite refreshes the automated AE browser/visual evidence, including the new pointer regression, without changing approved images or thresholds. It does not execute the complete native 400% zoom operation map, actual WebKit system-palette paint, or human assistive/device observations. The seven Owner-reported MAN-136 Pass records and supporting-details waiver remain intact, while eighteen itemized boxes remain unreported. The frozen manual acceptance interpretation, current-candidate hosted CI link and independent OI-16/G-4/FLUENT-APP-1/Product/Architect gates remain open. Story and sprint stay **in-progress**.

## Zero-field pointer-focus correction — 2026-11-02

The deferred R8-B1 visible-trigger risk reproduced on committed base `bdb4d0cea24cc58b44f1a61f384620def007f986` with only a new browser test: a real WebKit pointer click whose mousedown default focus move was canceled left focus away from the visible zero-field trigger in both themes (**0 passed / 2 failed**, zero retries). The test records that the trigger was unfocused at click time. A plain WebKit pointer click focused it naturally (**2/2 passed**) and did not exercise the failing branch. The failing fixture models a Safari-style nonfocusing pointer activation; it is automated WebKit evidence, not a physical Safari observation. Red reports and traces remain outside the checkout at `/tmp/fc-story13-6-pointer-red2-results` and `/tmp/fc-story13-6-pointer-red2-html`.

The hidden generated form now accepts an optional visible attempted-control ID, and the inline zero-field renderer supplies its existing trigger ID. The scope-unavailable branch uses that ID for its single attempted-control focus call; ordinary visible forms keep their prior submit fallback and existing in-form focus preservation. The bUnit assertion now checks the visible trigger and the three affected generated-text approvals reflect only the new parameter/focus expression and consequent render sequence numbers. No feedback, speech-owner, admission, typography, threshold, palette, dependency, policy or approved PNG was changed.

Current local source identity is committed root `bdb4d0cea24cc58b44f1a61f384620def007f986` plus `git diff --binary HEAD -- src tests | sha256sum` = `375a6150e13c92540603297268074747f2d3e6bf462fde30974962de0cea63a2`. The exact checks below ran against the rebuilt Release Counter Test host and the installed lockfile Playwright **1.64.1**. Playwright owned and stopped that host; port 5070 is closed.

| Check | Result |
| --- | --- |
| `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1 -v:q` | Exit 0, zero warnings/errors |
| Direct Debug xUnit `-class '*CommandFormEmitterTests' -class '*CommandRendererEmitterTests' -parallelMode none`, with `DiffEngine_Disabled=true` | **103 passed**, zero failed/skipped/errors; an intermediate run caught the old source-text assertion (**102 passed / 1 failed**), then the corrected assertion passed |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 -v:q`; direct Debug xUnit `-class '*ZeroFieldFailureAccessibilityTests' -parallelMode none` | Build exit 0, zero warnings/errors; **21 passed**, zero failed/skipped/errors |
| `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 -v:q`; `npm --prefix tests/e2e run typecheck` | Both exit 0; build zero warnings/errors |
| From `tests/e2e`: `FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-pointer-final-results FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-pointer-final-html npx playwright test specs/accessibility-state-matrix.spec.ts --grep 'generated zero-field (admission:|scope-unavailable pointer activation)' --workers=2 --retries=0 --reporter=list` | **12 passed** across Chromium, Firefox and WebKit, both themes; zero failed/skipped/flaky and zero retries |
| Same directory: `FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-zero-field-final-results FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-zero-field-final-html npx playwright test specs/accessibility-state-matrix.spec.ts --grep 'generated zero-field' --workers=4 --retries=0 --reporter=dot` | **78 passed** across the three engines; zero failed/skipped/flaky and zero retries |

The earlier **1,061 passed / four named skips** complete Story 13.6 run remains valid for its original `9cb085b9…` source and is not a complete rerun of this patch. The opt-in UX review's R8-B1/zero-field pointer finding is locally corrected with focused proof; its three High findings remain open: actual WebKit system-palette paint, complete current-candidate actual-zoom operation mapping, and the frozen manual criterion's interpretation against the Owner waiver. Seven MAN-136 records retain completed Owner-reported Pass with supporting details waived, while eighteen itemized boxes remain unreported. No human speech/device observation or OI-16/G-4/FLUENT-APP-1/Product/Architect approval is inferred. Story and sprint remain **in-progress**.

## Approved opt-in UX reviewer gate — 2026-11-02

The user approved running the optional Story 13.6 UX reviewer gate and selected all three lenses: requirements rubric, accessibility, and Fluent UI/visual ownership. The review used clean root `99740e0fd88b4b96e02467a5fd981480ed821f0b`; `git diff --quiet 9cb085b926f45b5d494c67e3f57be48aa2644bcc HEAD -- src tests samples .github eng` exits **0**, so the complete browser run's product/test source remains unchanged. The three current Playwright JSON reports confirm **355/353/353 passes, four named skips, zero unexpected failures and zero flaky cases**. The detailed reviews and synthesized Markdown/HTML reports remain local scratch at `/tmp/fc-story-13-6-ux-review-20261102-99740e0f/`; this owning result records their conclusions without adding a committed evidence packet.

The opt-in gate is **performed, with acceptance still open**. The rubric found 0 Critical / 3 High / 2 Medium, accessibility found 0 / 1 / 2 / 1 Low, and Fluent/visual found 0 / 1 / 1 / 1 Low. After deduplication, the parent dispositions are **zero Critical, three High, three Medium, and two Low**. No reviewer demonstrated a new product failure or granted UX, Product, Architect, OI-16, G-4 or FLUENT-APP-1 approval.

| Finding | Severity | Disposition |
| --- | --- | --- |
| AE-07: actual WebKit system-palette paint | High | **Open evidence gap.** Linux WebKit's 116 state records execute forced-color media assertions but retain authored paint and contain no system-palette measurements. Obtain actual supported-platform WebKit paint evidence for the changed state matrix or a separately authorized scope decision. This is not an observed product paint failure. |
| AE-02: all-operation actual 400% zoom mapping | High | **Open evidence gap.** The fourteen earlier native Chromium cases and completed Owner-reported Zoom Pass are retained, but current candidate evidence does not itemize every AE-01 operation and overlay at native zoom. Map and execute any missing journeys or obtain a scoped owner decision; viewport reflow remains distinct. |
| Manual criterion versus supporting-details waiver | High | **Open acceptance interpretation.** Preserve all seven `completed — Owner-reported Pass` records and the 2026-10-27 supporting-details waiver. The unchanged canonical criterion still asks for actual pairing, covered journeys/surfaces and outcomes, while those itemized execution facts were not supplied. Resolve through a narrow owner decision or candidate-bound receipt; do not invent observations or reopen the waived metadata request. |
| Reconciliation outline meaning/contrast | Medium | Engineering's supplementary classification of the transient roughly 1.6:1 outline remains provisional. UX ownership must decide whether it conveys material state; if yes, use a measured Fluent stroke meeting 3:1 in both themes and forced colors. |
| Current-candidate hosted CI link | Medium | The local 1,061-pass evidence and historical hosted c2 runs keep distinct identities. The PRD-derived evidence policy asks for a current-candidate CI run link; record one or an explicit policy disposition before completion. No remote run was triggered by this review. |
| Zero-field Safari/WebKit pointer-focus risk | Medium | Retain existing R8-B1 deferral. Run its WebKit pointer-activation fixture and correct the visible focus target if it reproduces loss; the current 1,061 passes do not settle that path. |
| Counter sample count-slot legacy token | Low | Pre-existing sample-style debt remains with its owning sample/UI maintainer; this review does not alter product visuals or approved baselines. |
| WebKit skip attribution | Low | **Corrected below.** The WebKit skips are overflow-clip-margin and a Chromium-scoped Story 11.5 visual-hook case; Firefox skips bevel paint and the same hook. Forced-color media checks executed in WebKit and are independent of all four skips. |

The canonical UX contract remains `draft` because its separate source-owned gaps and status decisions are not settled by this story review. Story 13.6 and sprint remain **in-progress**; the original unchecked matrix/result/Evidence tasks, frozen intent, manual waiver, and independent owner gates retain their status. The current review disposes every Critical/High finding as open with its required proof or owner decision; it does not count an open disposition as acceptance.

## Complete current-source browser verification — 2026-11-02

At clean product/test source root `9cb085b926f45b5d494c67e3f57be48aa2644bcc`, the complete configured Story 13.6 selection passes with the `tests/e2e/package-lock.json` toolchain. `npm ci` in `tests/e2e` restores **@playwright/test 1.64.1**; `npx playwright install chromium firefox webkit` supplies its browser revisions. Neither command changes tracked dependency files. The existing Test host was rebuilt with `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 -v:q` (exit **0**, zero warnings/errors). From `tests/e2e`, each engine runs `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=<engine> --workers=4 --retries=0`, with the exact engine, run ID and external report roots below. The configured JSON, JUnit and HTML reporters remain enabled.

| Engine | Run ID suffix and external root prefix | Passed | Failed | Skipped | Flaky | Artifact validator |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Chromium | `fc136-20261102-locked-chromium-9cb085b9`; `/tmp/fc-story13-6-20261102-locked-chromium` | 355 | 0 | 0 | 0 | Exit 0 |
| Firefox | `fc136-20261102-locked-firefox-9cb085b9`; `/tmp/fc-story13-6-20261102-locked-firefox` | 353 | 0 | 2 named | 0 | Exit 0 |
| WebKit | `fc136-20261102-locked-webkit-9cb085b9`; `/tmp/fc-story13-6-20261102-locked-webkit` | 353 | 0 | 2 named | 0 | Exit 0 |

For each row, `FC_A11Y_EVIDENCE_RUN_ID` is the shown run ID, `FC_A11Y_EVIDENCE_ENGINE` is the engine, `FC_E2E_OUTPUT_DIR` and `FC_E2E_HTML_REPORT_DIR` are the root prefix plus `-results` and `-html`, and the redirected command log is the prefix plus `.log`. With the same environment, `node scripts/validate-a11y-artifacts.mjs` exits **0** for every engine. Aggregate: **1,061 passed / 0 failed / 4 named skips / 0 flaky**, 1,065 total, with zero retries. Each report contains **116** owned state files. All **18** Linux specimen visual comparisons pass without changing approved images or thresholds. Each results/HTML root is below the unchanged 52,428,800-byte cap; the largest pair is Firefox at 15,495,883/8,436,389 bytes. `npm run typecheck` exits **0**, and `node --test scripts/a11y-state-evidence.test.mjs` passes **29/29** under the restored toolchain. Playwright stopped its owned Test host after each engine; port 5070 is closed.

The earlier full attempt under locally installed Playwright **1.63.0** is retained separately at `/tmp/fc-story13-6-20261102-full2-*`. That stale installation disagreed with the tracked 1.64.1 lockfile: Chromium passed 355/355, Firefox passed 353 with two skips, and WebKit passed 347 with two skips but failed all six visual comparisons by 40 or 49 pixels. Its WebKit artifact validator exited 1 on those six unsuccessful outcomes. The six mismatches do **not** reproduce with the lockfile toolchain; the older attempt is diagnostic history, not configured qualification. The earlier interrupted Chromium attempt remains separately recorded below.

This automated result does not establish actual WebKit **system-palette paint** on a supported platform. WebKit forced-color media checks executed but retained authored paint; its two skips concern overflow-clip-margin and a Chromium-scoped Story 11.5 visual hook, while Firefox skips bevel paint and the same hook. Those four skips are separate from the palette evidence gap. At the time of this browser run, the user kept the optional UX review **pending**; the subsequently approved reviewer gate above supersedes that pending state. Seven MAN-136 records remain completed Owner-reported Pass with supporting details waived; eighteen itemized boxes remain unreported. Full-topology, hosted and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions keep their own source attribution and status. Original acceptance checkboxes and Story/sprint **in-progress** status are unchanged.

## Current committed-source focused verification — 2026-11-02

At clean root `9cb085b926f45b5d494c67e3f57be48aa2644bcc`, the existing Story 13.6 scanner and measurement checks pass without product or test changes. `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` passes **29/29**; `npm --prefix tests/e2e run typecheck` exits **0**. From `tests/e2e`, `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-20261102-synthetic-rerun-results FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-20261102-synthetic-rerun-html npx playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper|default scanner|R4 |R5 |R6 |R7 ' --workers=2 --retries=0 --reporter=dot` passes **108/108**, 36 per engine, with zero failures, skips, flaky cases or retries. The selection starts no application runtime and does not replace the complete operator-surface, visual, native-zoom, .NET, full-topology or hosted gates.

The first attempt of the same browser selection, using the configured reporter and separate `/tmp/fc-story13-6-20261102-synthetic-results`, exited **1**: 74 browser-independent cases passed and 34 browser-launch cases failed because Chromium headless-shell revision 1243 was absent from the local Playwright cache. Its `playwright.json` records that single launcher error. `npx playwright install chromium firefox webkit` exited **0** and populated the cache for the already installed Playwright package; the rerun above is the passing observation. No package declaration, dependency selection or repository source changed.

An additional current-source check rebuilt the owned Test host with `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 -v:q`: exit **0**, zero warnings/errors. From `tests/e2e`, `FC_A11Y_EVIDENCE_RUN_ID=fc136-20261102-chromium-9cb085b9 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-20261102-full-chromium-results FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-20261102-full-chromium-html npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=chromium --workers=2 --retries=0` was deliberately interrupted after **129 passed / 2 interrupted / 224 not run**; exit **130**, zero observed failures before interruption. Its JSON/HTML and failure traces remain at the named external paths. This is an incomplete run and supplies no full-gate or artifact-validator pass. The Playwright-owned Test host stopped; port 5070 is closed.

The seven MAN-136 records retain **completed — Owner-reported Pass**, with supporting details waived by the Owner. The eighteen detailed boxes remain unreported itemized observations. Actual WebKit system-palette paint, the opt-in UX review or explicitly authorized skip, and independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions remain open. Earlier complete browser, native zoom, runtime and hosted evidence retains its original source attribution. Original acceptance checkboxes and Story/sprint **in-progress** status are unchanged.

## Approved root catalog repair and local activation - 2026-10-29

**Story 13.6 and sprint remain in-progress. The approved catalog amendment is implemented and independently reviewed.** This section supersedes earlier pending-amendment/current-root diagnoses while preserving their dated evidence. Owner approval permits the one-row catalog/three-line guard and normal FrontComposer-only audit, with all existing package versions unchanged.

Root implementation `f54ce542db8675d685c50408b390158152283106` selects Builds `81225bcb6151c38ae891abd9110af17385e1b3b9`, following source `11ef2cab441433c5fe25a2fb3ddcfc282e3a7f0e` directly from root `81ab0492`. Only the root gitlink and two exact policy bindings changed. Independent parent audit preserves all 307 prior selected versions, 302 unrelated package rows, 150 unrelated family decisions, every other root selection/policy field and consumer configuration hash. All 17 Platform mappings/source hashes/global trust remain unchanged.

Both exact Parties `492a8cc1` standalone Release/NuGet paths now restore/build with **zero warnings/errors**: normal uninitialized-nested fallback using root `81225bcb`, and its own selected `326af3d2` catalog. All three direct UI consumers resolve Contracts.UI **4.7.1** in both. Owner validators plus **297 fixture scenarios**, **169 Python checks**, **76 host/governance tests** and clean Debug Shell/serialized AppHost builds pass. Initial invocation corrections and the first AppHost copy-retry warning remain recorded. Exact commands/counts/revisions and evidence limits are in [the bounded repair record](spec-13-6-repair-root-catalog-fallback.md#implementation-verification--2026-10-29) and external `/tmp/fc-root-catalog-implementation-20261029-403g00hg/results.jsonl`.

Actual local activation `e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e` and its committed-base `f54ce542` transition both validate: **69 edges/eight selectors**, zero selector changes/affected modules in that later record. Earlier `cde40df7` to `f54ce542` refusal is preserved. This proves local delayed activation against the actual amended base, not a new hosted result or qualification from an older published base; no push/publication occurred. Parent receipts are `/tmp/fc-story13-6-approved-parent-wfbpz9j_/activation-checks.json` and sibling exact graph/transition JSON.

The current existing forced-color capability probe executed **3/3** engines with no retries: Chromium/Firefox replace authored colors with system black/white; Linux WebKit reports forced-colors active but leaves authored red/green. Actual WebKit system-palette paint remains open, as does the opt-in UX reviewer gate or explicit authorized skip. It is a capability record, not a passing every-surface palette gate. Earlier complete browser/native/hosted/runtime results retain their original candidates. All seven MAN-136 reports remain completed Owner-reported Pass with supporting details waived; eighteen itemized observation boxes and original matrix/result/Evidence acceptance tasks remain untouched. Independent OI-16/G-4/FLUENT-APP-1/Product/Architect decisions retain their separate meanings.

All three independent review layers completed. Current E1/B3 are resolved, documentation clarifications are applied, and four nonblocking agent-context/pre-existing follow-ups are recorded in deferred work. The bounded repair and earlier supporting correction are done; this does not transition primary accessibility acceptance or its remaining WebKit/UX requirements.

## Current source verification and pending catalog amendment — 2026-10-29

> **Historical checkpoint:** The [approved repair and local activation](#approved-root-catalog-repair-and-local-activation---2026-10-29) supersede this checkpoint’s pending-amendment and unresolved-fallback status. The observations below retain their original dates, inputs and limits.

**Story 13.6 and sprint remain in-progress.** All specified accessibility corrections are present at root `cde40df71ebcef6c6c74c25a5a5644bebef3fc17`. This continuation loaded the full primary spec and both frontmatter context files, retained the implementation, and refreshed bounded evidence. The starting working tree contained only the independently regenerated epic context. No product, test, dependency or policy change is applied here.

This section supersedes the earlier leading checkpoint's *current* dependency diagnosis: the approved Parties catalog correction and delayed activation are already committed, and current graph validation passes. The dated 2026-10-28 checkpoint below remains historical. The independent sibling-root fallback regression E1 remains reproduced and unresolved; semantic graph success alone does not establish every package import path.

| Current command / scope | Observed outcome |
| --- | --- |
| `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` | **29 passed**, zero failed/skipped/cancelled, Linux |
| `npm --prefix tests/e2e run typecheck` | Exit **0** |
| `python3 eng/validate-story-artifacts.py`; `git diff --check` | Both exit **0**; frozen text, all existing checkboxes, eighteen itemized observation boxes and CRLF preserved |
| From `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-20261029-95mkw08q/browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-20261029-95mkw08q/html npx playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper\|default scanner\|R4 \|R5 \|R6 \|R7 ' --workers=2 --retries=0` | **108 passed**, 36 per engine; zero failed/skipped/flaky and zero retries. Nine scanner/impact cases and 99 measurement regressions. |
| `python3 eng/dependency_graph.py --root . validate --commit cde40df71ebcef6c6c74c25a5a5644bebef3fc17` | Exit **0**, `ok: true`, **69 raw edges / eight selectors / all 17 Platform mappings** |
| `python3 eng/dependency_graph.py --root . diff --event push --event-base 0b469e03c5ae81cf6756f7cd8f2f78df7074514c --candidate cde40df71ebcef6c6c74c25a5a5644bebef3fc17` | Exit **0**, `ok: true`, no graph changes; uses that committed event-base policy, without candidate-policy substitution |

These checks used the current committed implementation, with documentation-only working changes. They start no application runtime. Complete operator-surface, visual, native-zoom, configured .NET, full-topology and hosted outcomes retain their original candidate attribution in the evidence inventory below. Historical [CI 40586087034](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586087034) and [Quality 40586086551](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586086551) qualify their exact earlier `c2b925ba` candidate only; no current hosted execution or whole-runtime equivalence is asserted.

### Reproduced fallback and concrete unapplied proposal

Current root Builds remains `81ab049208678bd6616828824729c0ddac02f7d1`; Parties remains `492a8cc1417c625cdfb7fd4c946686ef88adab9d`, selecting its separately corrected Builds `326af3d248974b5f3849ff667e97a5d61e3e3f30` as nested metadata. With that nested checkout uninitialized, the normal Parties wrapper instead imports sibling root Builds, whose catalog still lacks Contracts.UI.

From `references/Hexalith.Parties`, the current command `dotnet restore Hexalith.Parties.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true --artifacts-path /tmp/fc-story13-6-current-20261029-95mkw08q/fallback-current-artifacts` exits **1** with **three NU1010 errors**, for UI, AdminPortal and ConsumerPortal. The same command with a separate `fallback-proposal-artifacts` directory and `-p:Hexalith2BuildPackageProps=/tmp/fc-story13-6-current-20261029-95mkw08q/root-catalog-proposal/Props/Directory.Packages.props` exits **0**. The existing wrapper/property and all other settings are retained. This is restore proof for a proposed input, not an adopted catalog, standalone build or passing current fallback.

The historical proposal scratch is unavailable in this environment, so a concrete replacement was reconstructed outside tracked checkouts at `/tmp/fc-story13-6-current-20261029-95mkw08q/root-catalog-proposal/`. `builds-root-catalog.patch`, `README.txt` and the full proposed source are reviewable there. The patch adds exactly one Contracts.UI row using existing `$(HexalithFrontComposerVersion)` **4.7.1**, preserves every prior catalog byte/version, BOM and CRLF, and adds three lines to the existing authoritative-package contract for required identity/family alignment. Running `pwsh -NoProfile -File Tools/test-authoritative-package-catalog.ps1` from that scratch proposal passes **51 identities / four shared versions**; running the identical script from sibling `authoritative-missing-ui` and `authoritative-wrong-family` fixtures exits **1** with the expected missing-identity and `0.0.0` versus `4.7.1` diagnostics. The initial scratch constructor's failed exact-line assertion is retained separately; no tracked source changed.

E1 requires the Owner to amend the bounded catalog spec's frozen **“Keep root Builds `81ab049208678bd6616828824729c0ddac02f7d1`”** constraint. The parent has requested the concrete catalog/guard successor, normal FrontComposer-only revision-bound incremental audit, necessary root selector/projection bindings and seals, local immutable commits and later-base activation/verification; no push/publication is proposed. That amendment remains **pending**. No audit/feed refresh, root pin/policy change, local commit or dependent implementation is performed while it is pending. B3's guard is verified only as part of the proposal; B4/B10 retain their separate recorded dispositions.

Raw commands, cwd, exits and logs are in `/tmp/fc-story13-6-current-20261029-95mkw08q/results.jsonl`; browser output, fallback logs, proposed source and positive/negative contract logs remain under that same external scratch root. No committed evidence packet, manifest, replay lane or runtime-identity record is added. The root/Parties working trees and dependency selections remain preserved.

The [AE-01–AE-09 and changed-surface inventory](#current-committed-source-acceptance-audit--2026-10-28) retains its bounded historical sources; this synthetic selection does not close the complete current matrix. Actual WebKit system-palette paint and the required opt-in UX review or explicitly authorized skip remain unresolved. All seven MAN-136 records remain **completed — Owner-reported Pass; supporting details waived**; the eighteen unchecked observation boxes record unreported itemized evidence and do not reopen the waived metadata request. OI-16, G-4, FLUENT-APP-1 and Product/Architect decisions remain independent acceptance gates, without inferred approval or treating their mere openness as a new implementation requirement. Original baseline/frozen intent, original matrix/result/Evidence checkboxes and in-progress status are preserved.

## Current committed-source acceptance audit — 2026-10-28

**Story 13.6 and sprint remain in-progress.** The full primary spec and both frontmatter context files were loaded. All specified production/test corrections are present at the initially clean root `29f865e9a663d05179e0f23cb5f87f0feec858b3`; this continuation reconciles evidence only. The original story baseline `3c8679628ebdb8995afc09f712a04fcde623695c`, frozen intent, acceptance checkboxes and historical records are preserved.

Fresh Linux checks run on that exact committed source:

| Command / scope | Observed outcome |
| --- | --- |
| `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` | **29 passed**, zero failed/skipped/cancelled |
| `npm --prefix tests/e2e run typecheck` | Exit **0** |
| `python3 eng/validate-story-artifacts.py`; `git diff --check` | Both exit **0** after documentation reconciliation; original checkbox lines/frozen text and CRLF verified unchanged |
| From `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-20261028-MkfxR8/browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-20261028-MkfxR8/html npx playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper\|default scanner\|R4 \|R5 \|R6 \|R7 ' --workers=2 --retries=0` | **108 passed**, 36 per engine; zero failed/skipped/flaky, zero retries. Nine scanner/impact cases and 99 R4–R7 measurement regressions. |
| `python3 eng/dependency_graph.py --root . validate --commit 29f865e9a663d05179e0f23cb5f87f0feec858b3` | Exit **1**, `ok: false`: current committed Parties catalog-inheritance refusal below |
| `python3 eng/dependency_graph.py --root . diff --event push --event-base c49412ed567ac47e10a1cceec100d2c41505cf45 --candidate 29f865e9a663d05179e0f23cb5f87f0feec858b3` | Exit **1**, `ok: false`: the earlier active-base policy refuses `github.com/hexalith/hexalith.agents` |

Current-object validation and the previous-base transition are separate gates. The former consumes the policy committed at `29f865e9`; it no longer stops at the earlier c494 Platform diagnosis. Its first failure is Parties `9239c531f3b0cbb4a998c419e537463b597ff14b`, `Directory.Packages.props:19`: `<PackageVersion Include="Hexalith.FrontComposer.Contracts.UI" Version="$(HexalithFrontComposerVersion)" />`. That repository selects Builds `abc0e1be8069d1c7a567515a13bb015adc407536`, whose catalog sets the family to **4.7.1** but omits Contracts.UI. Root Builds `81ab049208678bd6616828824729c0ddac02f7d1` also omits it. Three Parties UI projects consume this package in NuGet mode. The inheritance rule rejects the local declaration even though it uses the family property; removing it alone would leave package-mode restore without its central version.

The smallest proposed correction is an owning Builds catalog entry using the existing family property, then an immutable Parties successor selecting that catalog and removing its local declaration, followed by root selection and exact graph/standalone verification. This is an **unapplied dependency amendment**, not a passing-gate claim or authorization to change revisions/policy. Further refusals can follow this first failure. The parent prepared XML-only review patches at `/tmp/fc-story13-6-catalog-proposal-9mzn3fr6/{builds.patch,parties.patch,README.txt}`; XML parsing and preservation of the existing 4.7.1 family/other catalog rows pass, but dependent immutable revisions, consumer seals and AD-12 activation remain unapproved. The prior-base transition still exercises the earlier policy and cannot be qualified by the candidate's new policy.

The acceptance inventory below links the existing surface lanes and preserves their source limits. The [complete 1,061-pass/four-skip browser run and eighteen unchanged visual comparisons](#automated-gap-closure--2026-10-25), [fourteen automated native Chromium zoom cases](#native-zoom-correction-and-verification--2026-10-25), [exact c2 hosted CI/Quality](#ci-execution-repair-and-current-hosted-verification--2026-10-27) and [original full-topology development proof](#canonical-mtls-full-topology-proof--2026-10-28) retain their original candidates. None was rerun or promoted to a whole-runtime pass at `29f865e9`.

| Acceptance requirement | Evidence owner and current boundary |
| --- | --- |
| AE-01, 320px reflow | Responsive/specimen/state-matrix journeys retain the complete-run evidence above; new synthetic checks do not exercise all current application surfaces. |
| AE-02, actual 400% browser zoom | Fourteen earlier native Chromium cases remain automated actual zoom; viewport resizing remains reflow. MAN-136-ZOOM retains the Owner-reported Pass/waiver. No new zoom run occurred. |
| AE-03, combined text spacing | Responsive helper and integrated lanes own all four simultaneous overrides, clipping and truthful row sizing; the 108 new cases verify bounded measurement regressions. |
| AE-04–05, targets and whole focus visibility | Existing responsive/state lanes retain inventories, exception provenance and keyboard/occlusion proof. New cases exercise helper regressions, not a new every-surface inventory. |
| AE-06, contrast | Existing themed lanes own 4.5:1/3:1 checks. New computed-paint regressions pass without reducing thresholds or replacing pixel evidence where required. |
| AE-07, forced colors | Chromium/Firefox proof retains its original attribution; actual **WebKit system-palette paint remains open**. A media query alone does not satisfy it. |
| AE-08, reduced motion | Integrated specimen/responsive/state evidence is retained. The sweep remains engineering-classified supplementary under R7-A1, without inferred UX approval. |
| AE-09, semantics, keyboard and speech | New scanner cases retain `wcag22aa`; integrated keyboard/announcement tests remain source-attributed. DOM mutations are not heard speech. Seven manual reports remain completed under the Owner waiver. |

| Changed operator surfaces | Existing journey coverage to retain |
| --- | --- |
| Shell, Home, navigation, tabs and toolbar | `responsive-accessibility.spec.ts` page/short-viewport journeys; `accessibility-state-matrix.spec.ts` shell chrome, rails and active toolbar/tab states |
| Projection, detail and missing-item recovery | State matrix generated loaded/loading/empty/error/detail/filter-hidden and scope-cancellation journeys; responsive real generated-row and physical-instance recovery checks |
| Command forms and zero-field outcomes | State matrix real generated validation/policy/zero-field outcomes, wrapped feedback, safe-copy/single-owner/next-attempt checks |
| Lifecycle and fresh-row presentation | State matrix actual lifecycle wrappers, rejection/Dismiss, connection/stale/recovery and fresh-cue states; original unchanged Epic 9 live proof keeps its development-only attribution |
| Palette, Settings, destructive dialogs and abandonment | Responsive overlay/keyboard/short-viewport journeys and state matrix active palette/Settings/destructive/abandonment journeys |

Raw logs/reports are external scratch at `/tmp/fc-story13-6-current-20261028-MkfxR8/`; no committed packet, manifest or runtime-identity record is added. No application runtime was started for these synthetic checks. There is no new configured .NET, complete browser/visual, full-topology or hosted result. The last full-topology proof used the original runner; later refusal/identity/diagnostic guards retain their separately tested limits. Hosted links above concern exact historical c2, not the current root.

All seven MAN-136 reports remain **completed — Owner-reported Pass; supporting details waived**. The eighteen unchecked boxes remain unreported itemized observations, not a renewed metadata blocker. Actual WebKit system-palette paint, current strict dependency qualification and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain open. No approval or authorized UX skip is invented; the original matrix/result/Evidence acceptance tasks remain unchecked. This continuation changes only this result and the primary spec; no implementation, dependency, policy, baseline, commit, push or publication is performed.

## CI execution repair and current hosted verification — 2026-10-27

**The bounded CI adoption repair is verified on `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4`. [CI40586087034](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586087034) and [Quality40586086551](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586086551) both succeed, attempt1.** All four configured CI projects execute with fresh passing reports, all eight required Quality jobs succeed, and the authenticated downloaded live bundle passes unchanged final-mode validation with all checksummed files intact. The final code/workflow root is distinct from the later five-record documentation-only descendant.

Our `610917884f727d5d9686a5c0fb097f4ec9fac930` repair preserves hidden proof files during upload after the preceding d174 bundle omits a checksummed `.last-run.json`. The current external c2 successor preserves that repair and independently corrects some feature lifetimes. Its runtime changes, earlier dependency advances and Cursor's CancelLabel manifest/duplicate-summary declarations retain external attribution. Our two API-summary partials pass full documentation validation. Every earlier failed/canceled attempt below retains its exact attribution. This authorized continuation and all review dispositions are recorded in the [completed bounded adoption spec](spec-13-6-adopt-reviewed-ci-workflow.md).

The caller consumes immutable derived Builds `6952b59c3486ee50d32056c3fee07a07e8120bd5` with a 45-minute outer budget. Its only execution change from approved CAD is explicit Release restore, matching the existing Release build. Caller-only `3211b8f9` followed the matching already-active policy in external `a54b5c8d`; this 695 authorization was bundled externally, while the two earlier policy activations were separate policy-only commits. Current caller Git-blob SHA-256 is `d31112aaddb3b5c1d8a7ecd4f63a3fc47bf4e902e5dbe3dff1f3915872499fc6`, and the complete 15-source authorization digest is `ea262ffb85a10749eb853dd40690b260acaaba352f8fe84f9e83c1e07aad3a0f`. Actual c2 closure equals its draft-evaluator and matches exactly one authorization in the earlier active610 policy. All previous authorizations remain. Graph diff610→c2 passes with zero changes/affected modules. Fresh canonical acquisition of the preceding unchanged dependency selections succeeds across 11 repository identities; no nested submodule was initialized or updated.

Both live helpers explicitly release successfully acquired lifecycle locks after exact owned cleanup, even with an inherited surviving child. Real lock probes require contention throughout cleanup; all six early-unlock mutants fail. The rejection helper permits one additional pass only after recognized nonzero transport errors plus HTTP 000 at every advertised endpoint. Each of the four routes still requires a completed 401/403; all other HTTP outcomes, partial responses, redirects and unrecognized errors fail. Quality accessibility/browser/live outer budgets are 45 minutes; no test, authentication or approved visual threshold changes.

Real execution of the four configured projects exposed existing source/catalog drift. The correction adds the seven existing dialog parameters, seven Feedback parameters and two adopter inventory classifications, with usage/input/announcement-ownership guidance. The validator and runtime APIs are unchanged. External commit `f02b490f746050186ad7fdf92bdc6877e360b22c` published those three documentation files; our later dd commit publishes the reviewed representative transport tests. External `/pushall` dependency advances, EventStore 3.128.2 override and dialog/keyboard changes are preserved and are not credited to this implementation. Current selected root Builds remains external `f914752c`; execution pin `6952b59c` is distinct. Test-only Builds follow-up `f00c4ef7fb0d7b08068eaeeb4b062d5e5ee4babf` is published on `fix/frontcomposer-13-6-release-restore`; its execution workflow Git blob is identical to `6952b59c`. The earlier missing Memories-selected Builds `7f740a82` source was reviewed/published unchanged and reacquired canonically.

| Current exact-candidate gate | Qualified outcome |
|---|---|
| CI / four configured projects | **585 passed**, zero failed/skipped: CLI157, Contracts233, Contracts.UI10, Testing185. Authenticated report/source/handoff digests, exact identities, unique summaries/counters, nonzero terminal counts and report freshness all pass. Optional disabled Aspire/performance jobs remain skipped. |
| Quality default unit/bUnit | **6,283 passed**, zero failed/skipped, eight distinct expected TRX modules and eight coverage files. Downloaded artifact digests and independent terminal/freshness checks pass. |
| Quality main required gates | Compilation/isolation, CLI/tooling, governance543, analyzer build proofs3, consumer contract3, live provider69, full Docs, library ApiCompat and internal packaged-adopter smoke all pass. These selections overlap the default lane and are not summed as unique tests. |
| Debug / accessibility | Debug103 passes; accessibility/keyboard/media/zoom/visual355 and browserless3+1 pass, with their artifact/legacy gates successful. |
| Live proof / authentication | One real browser test; all39 manifest-listed files including hidden status file survive upload with matching digests; unchanged final-mode validator exits0. All four routes return actual401 on endpoint1. |
| Chromium / Firefox / WebKit | Chromium530 passed/1 capability skip/no flaky; Firefox509 passed/22 skips/no flaky; WebKit508 passed/22 skips/**1 recovered flaky case**. Existing retry/skip policy is unchanged. |

WebKit's recovered case is `specs/route-skip-links.spec.ts:196:3`, “opening a shell dialog preserves its own Tab flow”. A successful retry does not establish zero-flake parent acceptance or actual WebKit system-palette paint. Advisory palette passes4; performance has27 passed/1 skipped; quarantine selects zero tests. Their existing advisory semantics are preserved and supply no broad performance/quarantine qualification.

Authenticated live artifact12771574151 has ZIP SHA-256 `1f3e2fb4c86e6d8efda15c57e8fa0c3525594c833a55438e0c0771a1df9dbd55`. Its exact candidate/run/job/attempt binding, complete39-file set, every manifest digest and unchanged manifest bytes pass independent replay. CI and Quality report/source/coverage artifact IDs and hashes are recorded in the adoption spec; raw authenticated metadata/downloads, reports and validator outputs remain external `c2-*` scratch.

Earlier bounded local checks retain their stated source attribution:

| Command / exact scope | Observed result |
|---|---|
| Build the four configured projects Release with warnings as errors; execute the actual immutable `6952b59c` MTP unit body in fresh scratch; independently qualify its four TRX | **585 passed**, zero failures/skips: CLI 157, Contracts 233, Contracts.UI 10, Testing 185. Actual `failure-count=0`; unique summaries/counters, terminal state, expected identities and coherent nonzero counts verified. This is local `5b6b55ca` plus the documentation patch evidence, not hosted proof. |
| Parent `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --disable-build-servers -m:1 -warnaserror`; direct Release DLL with `-class '*FcDocComponentDocumentationContractTests' -class '*DocsSiteValidationTests'` | Zero warnings/errors; **155 passed**, zero failures/errors/skips/not-run, including the 13 hosted documentation failures. |
| `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` | Exit 0, zero blocking failures. Two exact formerly failing CLI publication-isolation tests also pass 2/2. |
| From `tests/e2e`: `npm run test:epic-9-evidence`; focused bearer helper | **186 Node + 31 Python passed**; focused **42/42**, zero failures/skips. |
| In the isolated Builds source: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` | **383 assertions passed**; real Restore/Build execute in Release on both LF/CRLF workflow inputs with pinned SDK selection. Original Restore fails the executable configuration fixture twice. |
| Source-closure/handoff/graph contract tests, before later code/test corrections | 11/11/102 passed; original selected Builds 371 assertions passed. Governance 86 and Debug Shell zero-warning/error build retain their earlier repaired-code attribution before external dependency advances. |

| Hosted candidate / run | Exact outcome retained |
|---|---|
| cdb83ad initial [CI 40551439815](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439815), attempts 1/2 | Both exceed the actual 15-minute source-initialization cap; no tests/reports. |
| Same cdb [Quality 40551439079](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439079) | Main succeeds with 6,241 default tests, zero failed/skipped; Debug 103 passes; Chromium 526 passes / one capability skip. Accessibility/Firefox/WebKit exceed their old budgets. Live browser/artifact proof passes, then sequential rejection helper rejects the still-held lock. Overall failure. |
| 8d2b47 [CI 40558898116](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40558898116) | NU1109 in whole-solution default restore: required EventStore 3.128.2 versus central 3.128.0. Explicit Release solution restore succeeds locally; direct UI Debug and Release restores both pass. |
| Same `8d2b47bd` [Quality 40558896919](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40558896919) | Governance hits initially unpublished Builds `7f740a82`. Live proof passes after lock repair; first command probe exhausts two transport timeouts, later three paths return 401. No four-route pass. |
| `3211b8f9` and `0fc5f4ef` CI/Quality | Confirmed canceled after subsequent external main publications; their partial results do not qualify successors. |
| 5b6b55 [CI 40569513928](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40569513928) | All four execute: CLI 155 passed / 2 failed; Contracts 233, Contracts.UI 10, Testing 185 pass. The final blocking gate correctly rejects one failing project; catalog drift is the two-test cause. |
| Same `5b6b55ca` [Quality 40569513323](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40569513323) | Main fails 13 SourceTools cases on the same nine catalog findings. Accessibility/visual 355 and Debug 103 pass. Live acceptance passes one browser test, artifact/integrity validation and all four actual 401 responses on endpoint 1. Browser matrix outcomes remain unfinished at the recorded 19:59 UTC check; subsequent correction publication supersedes this failing run. |
| Corrected dd [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575108068) / [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575109420) | CI test job succeeds with **585 passed / zero failed/skipped** across all four projects; artifact12768196099 digest and unique/terminal/fresh TRX independently verified. Quality live proof passes one browser test plus artifact/integrity checks and all four first-endpoint401 responses; Debug103 passes. Overall CI succeeds, including dependency governance. Quality main fails before the default lane on missing FcFreshnessIndicator/FcLiveRegion type summaries. Every other required job succeeds: accessibility355; Debug103; Chromium530 passed/1 capability skip; Firefox508 passed/22 skipped/1 recovered flaky case; WebKit509 passed/22 skipped/no flaky cases. This is preceding-candidate evidence, not qualification of4164ad04. |

Additional exact-candidate outcomes: d174 [CI40582483259](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582483259) succeeds with585 fresh passing reports plus source/handoff qualification; Debug103 and one live browser test pass, and all four routes return actual401 (command endpoint2 after endpoint1 timeout; remaining routes endpoint1). Authenticated live artifact12770429158 matches its uploaded digest but omits exactly `playwright-results/.last-run.json` from the39-entry checksum manifest, so unchanged offline validation exits1. Remaining Quality jobs are canceled by the necessary610 upload repair; no overall Quality pass is claimed. Earlier4164 runs were canceled by Cursor's d174 publication.

Seven independent review/reconciliation rounds completed; all46 prior findings, C2-V1 and required artifact verification are recorded individually in the adoption spec. External c2 resolves three groups and partly resolves two. Fifteen follow-up groups remain: eleven independent external feature issues/verification gaps, including the two partial groups and new source-inspected fallback-predicate test gap, plus four pre-existing upstream issues: option-like optional hook names, duplicate TRX summaries, nonterminal counters and Release restore alignment in disabled Aspire/performance lanes. Qualification independently rejects malformed summaries/counters, and disabled lanes are not represented as executed. Exact commit messages we used pass their owning pinned commitlint versions. Raw logs, retained source stores and reports remain external at `/tmp/story13-6-ci-adoption-approved.KFDful/`; no evidence packet or manifest is committed. GitHub API quota temporarily prevented retrieval until 19:56:48 UTC; later authenticated results above supersede that temporary visibility blocker.

All seven MAN-136 records remain **completed — Owner-reported Pass; supporting details waived**. Story/sprint remain **in-progress** for actual WebKit system-palette paint, ordinary full-topology qualification and separate OI-16/G-4/FLUENT-APP-1/UX/Product/Architect acceptance. The eighteen detailed boxes remain unreported observation inventory and do not reopen the waived metadata requirement. Original unfinished parent acceptance tasks and historical evidence retain their scope.

## Initial approved CI adoption — 2026-10-27

The user approved source review, policy-first CI adoption, commits/pushes and hosted verification (“yes I approve”). This authorizes the bounded [adoption spec](spec-13-6-adopt-reviewed-ci-workflow.md). The preparation records below precede this approval; their partial-source and unapplied-pin statements are historical.

Policy-only commit `d11d85a3c61f651cc3645782f99eca0e7fd3a189` was published and confirmed on origin before caller-only commit `cdb83ad634168a418c04b7d0c2326bf8fada68f1`. The latter changes only the shared CI literal to already selected, published Builds `cad925260713718895282c69bcbdb8bc72e7b6a2`. No gitlink, package selection, release pin, test threshold, approved baseline or authentication enforcement changed.

Fresh isolated fetches acquired the exact Builds and external action commits. The existing collector proves the complete current 14-source and proposed/actual 15-source closures. Actual caller Git-blob SHA-256 is `2a7770eba09aaf4097fc560cc7a9a8f1d9f0254fbb8288afb12a513f8c554816`, reusable blob SHA-256 `cdef6dce9bf5cf6c35347e737e56bef5e8a055e0d2d748cc2ce3ff99381c631d` and CI policy authorization digest `c9a3729f8b3aeccf8bc56f64792387b4686c11ea62209e33418fc51e5be3e259`. The published pin candidate matches exactly one authorization in its actual earlier policy commit; the old closure remains authorized. Existing graph validation/diff, `draft-evaluator` and local `create-ci` contract validation pass. The latter uses the actual hosted run coordinate but is local contract evidence, not a hosted result.

All three independent reviews completed and every finding is dispositioned in the adoption spec. Fresh checks pass: 11 source-closure tests, 11 handoff tests, 102 graph tests, 371 selected-Builds assertions, story-artifact validation, diff/staged-file hygiene and pinned commitlint. An additional execution of the actual MTP loop with coherent failed TRX and exit 2 invokes all four projects, records one failure and makes the actual final gate exit 1. Three existing upstream limitations are recorded in the deferred-work ledger: option-like optional hook filenames, duplicate TRX summary acceptance and nonterminal counters. Hooks are disabled for this caller; this candidate's hosted reports will receive independent uniqueness/terminal-state checks before qualification.

[CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439815) and [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439079) were automatically triggered on exact candidate `cdb83ad634168a418c04b7d0c2326bf8fada68f1`, attempt 1. They are pending/in progress at this reconciliation; no hosted pass, all-four-project execution, complete Quality result or live rejection outcome is claimed yet. Exact commands, raw logs, immutable source stores and local closure/probe outputs remain external scratch at `/tmp/story13-6-ci-adoption-approved.KFDful/`.

All seven MAN-136 records remain completed under the Owner's supporting-details waiver. Story/sprint remain in-progress for their independent acceptance requirements, including actual WebKit system-palette paint, ordinary full-topology qualification and separate OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions. The original matrix/result/Evidence tasks and existing deferrals remain open. Shared-workflow adoption is now published; hosted qualification remains pending.

## Current tracking reconciliation — 2026-10-27

The implementation, test/configuration trees and root gitlinks at `7ce37d1cfd62a7b0efc80ea7f7304a61d1308c8f` are unchanged from `d1574a8f65f0927ee00b2a06ca1ad21d35f811ed`; the intervening commits change only this result and the primary spec. The earlier **108 browser/helper passes**, **29 Node passes** and successful TypeScript check below retain their exact original attribution and were not rerun for these documentation changes. No new runtime, native-zoom, visual, .NET, live-readiness or hosted result is claimed.

The current sprint comment now matches the [Owner amendment](#story-specific-manual-acceptance-amendment): all seven existing MAN-136 records are **completed — Owner-reported Pass; supporting details waived**. Story and sprint remain **in-progress**. The eighteen detailed observation boxes remain an inventory of unreported observations; waived supporting metadata is not a blocker. Hosted source closure/publication, active-base policy and workflow-pin adoption, exact-candidate hosted verification, actual WebKit system-palette paint, ordinary full-topology qualification and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain separate requirements. The original matrix/result/Evidence tasks and existing deferrals remain open.

Validation: `python3 eng/validate-story-artifacts.py` and `git diff --check` pass. Only the sprint comment, primary spec and this result change; no implementation or dependency selection changes. Raw verification output remains external scratch at `/tmp/fc-story13-6-tracking-20261027-jzkl4y_a`.

### Prepared CI adoption handoff

The local proposal (`proposal.md`) and exact later caller patch remain external scratch at `/tmp/story13-6-ci-adoption-review-6pujd1k5/`. They propose the existing selected Builds `cad925260713718895282c69bcbdb8bc72e7b6a2` in place of consumed `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. Current policy has one exact authorization for the current caller blob and zero for the proposed caller blob. Local Builds `origin/main` tracking names the selected commit; fresh remote availability was not checked.

The supplied local object-store map allows inventorying six owned workflow/action blobs and six direct external action coordinates for each Builds revision. The exact collector stops for both at `github.com/actions/checkout: repository is not present in the trusted exact-object store map`. This partial inventory is not a complete source closure or publication proof. The proposal includes consumed-to-proposed execution-source differences and requires exact source acquisition/review, an earlier policy-only authorization, later caller-pin activation against the actual active base, then complete hosted CI/Quality on the resulting candidate. No policy authorization or pin was applied. Those separately reserved operations require approval; the current Build implementation step prohibits remote operations.

## Owner amendment and current-source verification — 2026-10-27

**The seven manual records are qualified as completed with Owner-reported Pass under the dated amendment below. Story 13.6 and sprint remain in-progress for their separate acceptance gates.** All implementation tasks through the reviewed verification repair are already present at clean root `d1574a8f65f0927ee00b2a06ca1ad21d35f811ed`; this continuation changes only this result and the primary spec.

### Story-specific manual acceptance amendment

Jérôme Piquot, the previously identified Owner, replied to the supporting manual-details request: **“No need to complete. All Ok today”** on 2026-10-27. This renews his all-seven Pass confirmation and explicitly waives completing the requested supporting execution metadata for the existing `MAN-136-NVDA`, `MAN-136-JAWS`, `MAN-136-VO`, `MAN-136-ZOOM`, `MAN-136-TABLET`, `MAN-136-PHONE` and `MAN-136-ZERO-FIELD` records. Their current Story 13.6 canonical qualification is therefore **`completed` — Owner-reported Pass; supporting details waived by the Owner**. They are no longer blocked solely for those details. This narrow human amendment supersedes the earlier missing-details blocker and the manual template's supporting-field requirement for these seven existing reports; it does not change the template or authorize the same waiver for future runs.

Actual execution date/build/tester, software versions, device models, measurements and itemized heard-speech/geometry observations remain **not supplied**. “Today” dates the confirmation and waiver; it is not substituted for an unreported execution date. The eighteen detailed observation boxes remain unchecked because no itemized observations were supplied; they remain an honest evidence inventory and do not reinstate the waived metadata blocker. Earlier dated `blocked`/`unperformed` statements are historical and superseded for these seven records. The Owner's report is human-supplied evidence; no automated run is represented as a manual observation.

This amendment supplies no new hosted result, WebKit system-palette observation, ordinary full-topology qualification or separate OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decision. Those independent requirements, the original matrix/result/Evidence acceptance tasks and R8-B1's existing deferral remain open. The [reviewed local repair](#independent-review-and-patched-local-completion--2026-10-26) retains its exact earlier candidate and reduced-topology scope.

### Fresh bounded automated checks

The full primary spec and both frontmatter context files were loaded before work. The following new checks ran against unchanged committed test/helper inputs at root `d1574a8f65f0927ee00b2a06ca1ad21d35f811ed` on Linux; documentation reconciliation followed them. Selected Builds is `cad925260713718895282c69bcbdb8bc72e7b6a2`, while shared CI still consumes `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. These browser/helper checks and the parent's selected-Builds contract run do not establish execution of the consumed shared workflow. The selected repair still needs separately authorized publication/source-closure and active-base policy/pin-adoption review, followed by hosted checks on the resulting exact candidate. The prepared caller-pin-only scratch patch is unapplied and is not approval, adoption or complete source-closure proof.

| Command / scope | Result |
| --- | --- |
| `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` | **29 passed**, zero failed/skipped/cancelled |
| `npm --prefix tests/e2e run typecheck` | Exit **0** |
| `python3 eng/validate-story-artifacts.py`; `git diff --check` | Both passed after documentation reconciliation |
| Parent: `python3 eng/dependency_graph.py --root . validate --commit d1574a8f65f0927ee00b2a06ca1ad21d35f811ed` | Exit **0**, `ok: true`; `/tmp/story13-6-ci-adoption-proposal-r15xkm2x/current-graph-validation.json` |
| Parent, from selected Builds: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` | Exit **0**, **371 assertions passed**; `/tmp/story13-6-ci-adoption-proposal-r15xkm2x/builds-platform-contracts.log` |
| From `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-20261027-fx4huj/browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-20261027-fx4huj/html npx playwright test specs/a11y-helper.spec.ts specs/responsive-accessibility.spec.ts --grep 'a11y helper\|default scanner\|R4 \|R5 \|R6 \|R7 ' --workers=2 --retries=0` | **108 passed**: 36 each Chromium/Firefox/WebKit; zero failed/skipped/flaky, zero retries. Includes nine scanner/impact cases and 99 R4–R7 measurement regressions. |

Raw output and reports remain local scratch at `/tmp/fc-story13-6-current-20261027-fx4huj/`; no packet, manifest, runtime-identity record or replay lane was added. No production source, threshold, approved baseline, dependency selection or workflow pin changed. No application runtime was started for these synthetic checks. Complete operator-surface, native-zoom, visual, configured .NET and live-readiness results retain their earlier exact source attribution; they were not rerun or promoted to this commit. Hosted CI was neither queried nor triggered. Existing [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562358) and [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562097) links concern historical root `9c4166e4b7cb45e2512e2b6db887a071f35ce244`, not this candidate. No commit, push, publication or remote action occurred.

## Independent review and patched local completion — 2026-10-26

**The approved verification repair is complete and independently reviewed. Parent Story 13.6 remains in-progress.** This entry supersedes the earlier local candidate for the files changed during review; all earlier results and failures remain historical evidence.

All three required review layers completed against the full baseline diff. The repair spec's Review Triage Log records every finding separately: one shared-store misconfiguration claim was refuted by the generated component configuration and selected platform's non-null local-path wiring; fourteen findings, including two duplicate reports, were addressed in twelve correction groups. No new findings were deferred. Existing story deferrals and independent acceptance requirements remain unchanged.

The reviewed patch requires confirmed Docker absence before deleting private recovery configuration, handles cancellation and child timeouts through bounded owned cleanup, and acquires the lifecycle lock before the initial source build. Both health validators require all nine selected platform checks while accepting additional healthy checks. Ordinary persistent Keycloak retains its pre-existing import/credential path; generated credentials remain confined to fresh realms, and disposable verification refuses persistent reuse. Malformed responses produce bounded diagnostics, missing cached images identify their public prerequisites without pulling images, and messages claim retained diagnostics only when a log was written. All four shared CI loops now also reject failed TRX summaries and inconsistent outcome/error counters. New regressions exercise the activation caller's persisted-marker checks and the C# health gate's phase decisions.

| Final reviewed-candidate command / check | Result and evidence |
| --- | --- |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | **31 passed**, no failures/skips/errors; `/tmp/story13-6-parent-review-host.log` |
| From `tests/e2e`: `npm run test:epic-9-evidence` | **163 Node + 31 Python passed**; `/tmp/story13-6-parent-review-tooling.log`. Includes actual signal/timeout cleanup fixtures, lock contention, uncertain Docker cleanup, malformed responses and activation success/rejected persisted readback. |
| From Builds: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` | **335 assertions passed**; `/tmp/story13-6-parent-review-builds.log` |
| Focused Shell/AppHost compilation after review fixes | Zero warnings/errors; `/tmp/story13-6-review-shell-build.log` and `/tmp/story13-6-review-apphost-build.log` |
| `python3 eng/verify-isolated-readiness.py` | **Exit 0**; `/tmp/story13-6-reviewed-live.log`; artifacts `/tmp/story13-6-isolated-readiness-jsjhgpva/` |
| Complete source AppHost build inside the runner | Zero warnings/errors, 1:40.73; `source-build.log` in that artifact root |
| Maintenance backup, quiescence, activation and readback | Real native backup checksum/key inventory verified; activation **HTTP 200**; persisted schema **1**, writer **2**, exact selected commit and timestamp `2026-10-26T19:14:14.2424999+00:00`; `maintenance-result.json` |
| Normal health/readiness and invalid bearer | Direct `/health` and `/ready` **HTTP 200 / Healthy**, all nine checks Healthy, ordinary workers restored; four first-attempt **HTTP 401** responses; `health-final.json` and `invalid-bearer.log` |
| Unchanged Epic 9 proof with explicit development/allow-dirty mode | **One Chromium test passed** (23.5s); candidate integrity and artifact validation passed. Source build zero warnings/errors (1:32.31); `epic9.log` and `epic9/apphost-source-graph-build.log`. |
| Parent postflight | All 14 implementation/test input hashes unchanged; backup hash matches reference; no owned AppHost, infrastructure container or private realm directory remains; private configuration removed and pre-existing unrelated AppHost path/PID preserved; `parent-verification.json` |

Candidate: root `5b8a860744667fd09848d6a21bf592df2f592322` plus the reviewed working patch. Selected EventStore remains clean at `aa46e2526ab33b2f4b220478c8acc097f7ad01b0`. Builds is `09af6ef818beb27490c87c00ed83dd581a7e6d8d` plus its two reviewed file changes. `/tmp/story13-6-reviewed-source-manifest.json` records fourteen input hashes (including those Builds files), SHA-256 `198ba74bca0f64e448d1bd2a949790caf583b3a2e81f53f3c9dcba795a2aa43d`. All were stable through the final run. The retained native backup is `/tmp/story13-6-isolated-readiness-jsjhgpva/pre-cutover.rdb`, SHA-256 `0fca7ec4fafcaf3b1136675723f3c959e43241b944f3aaf118d72fd854bf9059`. Provider identities remain Dapr **1.18.7**, Redis **7.4.11** and **state.redis/v1**, with exact image IDs recorded in the maintenance result.

A parent postflight initially compared complete process lists, which differed because another external AppHost started during verification. Comparing the pre-existing exact path/PID confirms it was preserved; the additional unrelated AppHost was also left untouched. This was a postflight-comparison correction, not an infrastructure cleanup failure.

Earlier unchanged documentation (64), governance (86) and migration (3) checks retain their passing evidence and were not rerun. Shell/Python syntax, story artifact validation and both repository diff checks pass. No commit, push, dependency/pin change or hosted rerun was performed; shared CI still consumes `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. This local reduced gateway-plus-Counter proof does not qualify the ordinary full topology, hosted execution, publication or production cutover. Seven Owner-reported manual Pass records retain their missing execution details; eighteen detailed human boxes and the original parent acceptance/independent-owner gates remain open.

## Approved isolated readiness — 2026-10-26

**The approved local host/readiness tasks now pass. Story 13.6 remains in-progress for its separate hosted, manual and independent acceptance requirements.** This entry supersedes the full-health blocker in the earlier local-identity result below and preserves every earlier failed attempt. The successful topology is explicitly the isolated EventStore gateway plus Counter, with owned Redis, placement, scheduler and fresh Keycloak. It does **not** certify the unchanged ordinary Tenants/Parties/Admin topology or a production cutover.

The opt-in `FrontComposerVerification` branch uses the selected platform's existing component-path and Dapr endpoint APIs. Ordinary development composition is unchanged. `eng/verify-isolated-readiness.py` creates private configuration and owned loopback infrastructure, verifies exact container names/labels/images/IDs/bindings, and shares the existing AppHost ownership lock. Maintenance alone disables projection workers; both subsequent verification lifecycles omit that override and restore the selected gateway's ordinary workers. Counter waits for the gateway's direct component health/readiness through an Aspire health check. Redirects are refused for credential/control requests and application health checks; rejected or changed AppHost identities cannot authorize mutation or cleanup. The watched component directory contains only the three component files; access-control configuration and SQLite runtime files remain outside it.

Before activation, the runner rechecks clean selected EventStore `aa46e2526ab33b2f4b220478c8acc097f7ad01b0`, requires the inactive marker to be the **only** unhealthy component, and verifies the fresh store has no domain/control data. Dapr creates one infrastructure stream for the selected gateway's sole subscription: length and entries-added are zero, generated/deleted IDs are `0-0`, and its sole expected consumer group has zero pending/progress/lag. Unknown keys, prior/deleted messages, other groups and existing markers fail closed. This exact invariant is checked before backup, after backup and immediately before activation; it is not a claim that Redis has zero keys.

A real Redis `SAVE` backup was checked with `redis-check-rdb` (valid checksum, exactly the inventoried one key), copied to private scratch and verified against the container file's SHA-256. Existing authenticated administrator access invoked the existing writer-v2 activation endpoint with the actual source commit, immutable backup reference, verified writer/retry quiescence and no-downgrade acknowledgement. HTTP **200** was followed by persisted readback: schema **1**, writer protocol **2**, exact selected commit, activation timestamp `2026-10-26T18:37:14.9614653+00:00`. No shared store or pre-existing marker was altered. The retained backup is `/tmp/story13-6-isolated-readiness-47bq6vic/pre-cutover.rdb`, SHA-256 `87f6dd08df00cc4d2c630b388a4e63e48f4d85a3f5b6a054771a67513b001ba6`.

| Final command / check | Result and local diagnostics |
| --- | --- |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-dependencies --no-restore -m:1` | Zero warnings/errors; `/tmp/story13-6-readiness-tests-build-fixed.log` |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | **25 passed**, zero failures/skips/errors; `/tmp/story13-6-readiness-host-tests-final.log` |
| From `tests/e2e`: `npm run test:epic-9-evidence` | **163 Node + 19 Python passed**, including 25 rejection-helper cases; `/tmp/story13-6-readiness-tooling-tests-final.log` |
| `python3 eng/verify-isolated-readiness.py` | **Exit 0**; outer `/tmp/story13-6-isolated-readiness-verified-parser.log`; owned run artifacts `/tmp/story13-6-isolated-readiness-47bq6vic` |
| Runner's source AppHost Debug build, using the existing proof's source-graph flags | Zero warnings/errors, 1:47.83; `source-build.log` in that artifact root |
| Owned maintenance and post-activation checks | Fresh workload client exchange **200**, expected client/audience/operation; real backup and authenticated activation/readback pass. Subsequent direct `/health` and `/ready` are **200 / Healthy**, all nine components Healthy, ordinary workers restored. `maintenance-result.json`, `health-maintenance.json`, `health-final.json` and `invalid-bearer.log` |
| Existing `eng/verify-apphost-invalid-bearer.sh`, invoked with the private verification environment | **Four actual HTTP 401 responses**, each on its first endpoint; `invalid-bearer.log` |
| Unchanged `eng/run-epic9-live-proof.sh`, invoked with `FC_EPIC9_ARTIFACT_ROOT=/tmp/story13-6-isolated-readiness-47bq6vic/epic9` and `FC_EPIC9_REQUIRE_CLEAN=false` | **One Chromium test passed**, zero failures/skips (23.6s); its fresh source build also has zero warnings/errors (1:31.86). Existing HEAD/status integrity and artifact validation pass; `epic9.log` and `epic9/runtime-metadata.json` |
| Existing artifact validator: `node scripts/validate-epic9-artifacts.mjs /tmp/story13-6-isolated-readiness-47bq6vic/epic9 --candidate 5b8a860744667fd09848d6a21bf592df2f592322 --allow-dirty` | Passed, invoked by the unchanged proof; no proof-format change |

The run uses root `5b8a860744667fd09848d6a21bf592df2f592322` plus the frozen local implementation patch, in explicit **development/allow-dirty** mode. The twelve changed implementation/test input hashes are in `/tmp/story13-6-readiness-source-manifest.json`, manifest SHA-256 `a460473411b7b8e03766a7b17fe6ba1433fe095d8a986973b6e5314454cefa86`; every input remained unchanged through the run. This is successful local verification, not clean committed/hosted or release acceptance. The later evidence reconciliation is separate. External edits to `deferred-work.md` and the Story 15.5 compatibility spec are preserved and not attributed to this repair.

Actual provider identity is recorded in `maintenance-result.json`: Dapr runtime **1.18.7**, Redis **7.4.11**, state component **state.redis/v1**, Redis image ID `sha256:57e4255cc971b1c8d6f8a6ca283ac01bc3ecdd914273ef6bfaede87aaecd6541`, and placement/scheduler image ID `sha256:0ac4fadf0016be85065cdebab952a8350e468cf9283d26777b0307a14a10aecc`. Built EventStore dependency metadata identifies Dapr .NET SDK **1.18.10**; proof metadata records .NET SDK **10.0.402**, Aspire CLI **13.6.1** and Node **v26.4.0**. During the final proof lifecycle, a read-only observation correlated the exact sidecar PID and gateway parent and checked every running `--resources-path`/`--config` argument against this run's private directories; `/tmp/story13-6-readiness-sidecar-binding-proof.log` contains only the sanitized result. Aspire redacts those arguments and omits the secret from ordinary discovery. An optional discovery-only secret comparison was therefore unavailable, not an observed mismatch; fresh credential exchanges and the existing renderer/wiring tests remain the credential evidence. No secret, token, raw process argument, state key or payload was logged by these diagnostics.

Owned cleanup passed for all three lifecycles. The three exact infrastructure container IDs are absent, private configuration is removed, generated realm directories returned from zero to zero, and the retained backup still matches its hash; `cleanup-verification.json` records these checks. `epic9/apphost-postflight.json` contains only two unrelated AppHosts, which were preserved. No further live runs are required for this candidate.

Earlier isolated attempts remain failed history: `/tmp/story13-6-isolated-readiness-a2qliwxn` hit a Docker Desktop reserved high port; `c9vczrs8` exposed Dapr configuration/SQLite files in the watched component directory, causing SIGHUP restart and actor shutdown; `3rul1isk` and `6q4gl_8a` stopped on incorrect diagnostic import-mount assumptions. The selected Aspire Keycloak overload copies realm files with `WithContainerFiles`, so the diagnostic now reads the fixed copied file only inside the exact owned container. `_gaapioi` stopped before backup on JSON object versus flat-pair XINFO representation. The actual read-only `verify_pristine_store` diagnostic subsequently passed with producer-shaped metadata (`/tmp/story13-6-readiness-json-diagnostic.log`), before the final full run. All these attempts cleaned owned resources; none performed activation. Initial obsolete API and analyzer build failures are retained separately from the passing logs above.

The unchanged Docs **64**, governance **86**, migration **3** and Builds **287** results retain their earlier attribution and were not rerun. Selected Builds remains `09af6ef818beb27490c87c00ed83dd581a7e6d8d`; consumed shared-CI workflow remains `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. Publication/active-base pin adoption, full hosted qualification, the ordinary complete topology, seven Owner-reported manual Pass records' qualification gaps and independent approvals remain separate. No commit/push, pin/dependency change, hosted rerun or manual observation was performed. Parent story/sprint remain **in-progress**; independent Build review follows this local completion.


## Approved local workload identity — 2026-10-26

**The local invalid-bearer check and existing Epic 9 proof now pass. Full EventStore health remains unqualified, so the host task and Story 13.6 remain in-progress.** This entry supersedes the earlier missing-workload-credential blocker below; it does not erase those failed attempts or claim hosted acceptance.

The approved amendment binds `eventstore` through the selected platform's `WithEventStoreWorkloadClientCredentials` API and an Aspire secret parameter whose default is not published. `LocalWorkloadRealm` renders a private per-run copy of the checked-in realm with a generated 256-bit secret and a confidential service-account client. Its grants are explicit audience/operation scopes, with browser/password grants disabled. Existing user clients remain structurally identical and the checked-in realm is unchanged. Persistent realm reuse, secret overrides and conflicting client/scope definitions fail without replacing user data. The rendered directory/file use Unix 0700/0600 and are removed on owned shutdown. Adding explicit workload scopes initially suppressed Keycloak's built-in user scopes; the final renderer preserves their creation when the original omitted `clientScopes`, while retaining an explicit existing `CreateDefaultClientScopes` setting. The fresh existing-client `openid profile email` exchange now passes.

Continuation began at `3668b603bf9cd176da86a2f3e020fd8fab2b58af`. External commit `a545702318164b45985c45c8dc00ce61e0c3b79a` recorded unrelated Story 15.5 work during verification; the first successful Epic 9 proof used that candidate plus the then-dirty host amendment. External commit `5b8a860744667fd09848d6a21bf592df2f592322` subsequently recorded that amendment. The **final** proof used `5b8a860744667fd09848d6a21bf592df2f592322` plus only the two-file scope-preservation source/test patch (saved at `/tmp/story13-6-workload-scopes-source.patch`, SHA-256 `a9f2ed700f843e856ff524a90650b66fd9b31268e736de21fb5c71a3079bd575`). It ran in explicit development/allow-dirty mode and passed the existing HEAD/status integrity check. These local observations do not establish a clean committed or hosted candidate. This evidence reconciliation follows the run and is not part of its captured candidate.

| Amendment command / check | Observed result and local log |
| --- | --- |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-dependencies --no-restore -m:1` | Exit 0, zero warnings/errors; `/tmp/story13-6-workload-tests-build-scopes.log` |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | **22 passed**, zero failures/skips/errors; `/tmp/story13-6-workload-host-tests-scopes.log` |
| `HexalithFrontComposerFromSource=true dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` | Exit 0, zero warnings/errors; final `/tmp/story13-6-workload-apphost-build-scopes.log` (2:39). The earlier corrected-overload build is `/tmp/story13-6-workload-apphost-build-fixed.log`; `/tmp/story13-6-workload-apphost-build.log` retains the initial CS1660 failure and is not the passing log. |
| From `tests/e2e`: `npm run test:epic-9-evidence` | **161 passed**, including 23 rejection-helper cases; `/tmp/story13-6-workload-tooling.log`. This amended-run tooling result precedes the final host-only scope correction; tooling inputs are unchanged. |
| `./eng/verify-apphost-invalid-bearer.sh` | Final exit 0: **four actual HTTP 401 responses** in five attempts. The first command-submit HTTPS attempt timed out with curl 28/HTTP 000; transport-only fallback received 401, and the other three routes received 401 on their first endpoint. `/tmp/story13-6-workload-live-bearer-scopes-final.log`. |
| `FC_EPIC9_ARTIFACT_ROOT=/tmp/story13-6-workload-proof-scopes-final-20awVH FC_EPIC9_REQUIRE_CLEAN=false ./eng/run-epic9-live-proof.sh` | Run after `umask 077` and `mktemp -d /tmp/story13-6-workload-proof-scopes-final-XXXXXX`: exit 0, **one Chromium test passed** (22.9s), source-graph build zero warnings/errors (2:24), candidate integrity and artifact validation passed. `/tmp/story13-6-workload-proof-scopes-final.log`. |
| From `tests/e2e`, invoked by the unchanged proof: `node scripts/validate-epic9-artifacts.mjs /tmp/story13-6-workload-proof-scopes-final-20awVH --candidate 5b8a860744667fd09848d6a21bf592df2f592322 --allow-dirty` | Passed; existing proof format retained. The earlier passing packet `/tmp/story13-6-workload-proof-3x3YF9` remains attributed to dirty candidate `a545702318164b45985c45c8dc00ce61e0c3b79a`. |
| `bash /tmp/story13-6-owned-health-check.sh` with the rebuilt final host | Workload client-credentials exchange **200**, expected client/audience/operation verified in memory; wrong secret **401**; existing public user client with `openid profile email` **200**. Exit **1** solely at the final full-health assertion described below. `/tmp/story13-6-workload-live-consistency-scopes.log`. No token or credential value is logged. |

Both final owned live runs cleaned their FrontComposer AppHost; the proof's `apphost-postflight.json` is empty. Generated `hexalith-frontcomposer-keycloak-*` directories were absent after each owned shutdown. The diagnostic cleanup correlates the exact AppHost path/PID and preserves an unrelated Projects-workspace host observed during that run. Immediate sequential starts twice encountered the still-held lifecycle lock and exited 2 before starting another topology; read-only ownership checks followed by retry after release succeeded. Those bounded failures remain in `/tmp/story13-6-workload-live-bearer-scopes.log` and `/tmp/story13-6-workload-proof-scopes.log`. An earlier scratch probe rejected Keycloak's HTTPS discovery URL before exchange; that was a probe error, corrected in scratch, not a product result.

### Remaining full-health prerequisite and read-only handoff

The final diagnostic's `aspire wait security --status healthy` and `aspire wait eventstore --status healthy` succeeded. `aspire describe` recorded Keycloak `Running`/`Healthy` with its readiness check healthy, and EventStore `Running`/`Healthy` with an empty `healthReports` object. However, the direct application `/health` response was **HTTP 503 / Unhealthy**. The point-in-time component report is retained privately at `/tmp/story13-6-workload-health-body.json`; bounded messages are:

| Unhealthy component | Observed diagnostic |
| --- | --- |
| `dapr-sidecar` | Dapr sidecar is not responsive. |
| `dapr-actor-placement` | Placement disconnected, runtime status INITIALIZING; actor host not ready. |
| `projection-delivery-writer-protocol` | Projection delivery writer protocol v2 is not active. |

`self`, Dapr state store/pubsub/config store/app-channel-token, and projection-delivery unresolved-conflict checks were Healthy. The snapshot does not establish whether the Dapr startup conditions persist. It does establish that full application health was not achieved by this check; Aspire's resource label, passing authentication responses and passing Counter proof do not substitute for it. The host task and fresh-realm matrix row remain open; the separate invalid-bearer task is complete.

Selected EventStore `aa46e2526ab33b2f4b220478c8acc097f7ad01b0` supplies the follow-up map: `src/Hexalith.EventStore.Aspire/AspireDaprLocalServiceEndpoints.cs` resolves the existing `Dapr:PlacementHostAddress` / `Dapr:SchedulerHostAddress` settings passed by FrontComposer `Program.cs`; `src/Hexalith.EventStore/HealthChecks/DaprSidecarHealthCheck.cs` and `DaprActorPlacementHealthCheck.cs` own those diagnostics. `src/Hexalith.EventStore.Server/Projections/ProjectionDeliveryWriterProtocolHealthCheck.cs` requires the current store-global marker. The existing `POST /api/v1/admin/projections/delivery-writer-protocol/activate` action in `AdminProjectionRebuildController.cs` and `docs/operations/projection-delivery-v2-cutover.md` require an authenticated administrator, exact commit, backup reference, quiescent writers and explicit cutover attestations. No writer-protocol activation or cutover-state mutation was performed. The existing `tests/Hexalith.EventStore.IntegrationTests/Security/ProjectionDeliveryWriterProtocolTestLease.cs` and associated cutover policy/fixture are a reuse option for separately scoped disposable readiness rehearsal, not permission to invoke them against the current shared store. Follow-up validation must demonstrate the actual component/readiness response and retained auth/proof outcomes without weakening any check.

`python3 eng/validate-story-artifacts.py` and `git diff --check` in root and Builds pass after reconciliation.

The prior Docs 64, governance 86, migration 3 and Builds 287 passing checks remain attributed to their earlier inputs and were not rerun as unchanged lanes. Selected Builds is now external `09af6ef818beb27490c87c00ed83dd581a7e6d8d`, containing the shared repair; FrontComposer still consumes workflow pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. Publication, complete active-base adoption review and hosted acceptance remain separate. No commit/push, dependency change, pin adoption or hosted rerun was performed by this continuation. All seven Owner-reported manual Pass records, their qualification gaps, original parent acceptance tasks and story/sprint in-progress status remain unchanged.

## Hosted-verification repair — local implementation, 2026-10-26

The authorized verification repairs are implemented locally. **Story 13.6 remains in-progress: the live invalid-bearer check fails, and the shared workflow repair has not been published or adopted.** The approved repair spec's frozen intent and original baseline `9c4166e4b7cb45e2512e2b6db887a071f35ce244` are unchanged. Work began at root `0f40ab06304e22c30c467ffb2279333897a79572`; external commits recorded approval at `2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb` and then unrelated Story 15.5 documentation at `2f1dd0e128273706634374d7d66a0878662bd62f` during the checks. These results describe the local working changes across that interval, not a clean committed or hosted candidate. The latter external commit changed no repair implementation file. Builds remains at `1783c0a377615063f3222c1d28f3ab513af4e72f` plus changes to exactly its two approved files; the root Builds gitlink and consumed CI workflow pin remain unchanged. No commit, push, dependency update, nested submodule initialization or hosted rerun was performed for this repair.

Quality now restores the pinned local .NET tools before Gate 2b. The governance test requires that ordering and retains the blocking full DocFX validation command. Documentation exclusion matching still uses DocFX 2.78.5, including with `-SkipDocFx`; unavailable matcher inputs produce a bounded prerequisite diagnostic without a cache path or PowerShell location traceback. Exact and recursive exclusions fail, unrelated exclusions pass, and the empty-cache regression fails with the prerequisite message. Story 15.4's selected-catalog identity and publication rules are retained. The three stale migration phrases now assert executable/versioned guides, CLI-supported versus manual-only classification, and explicit `--dry-run` / `--apply`; all three original test cases and ownership, metadata, API and version guards remain.

`eng/verify-apphost-invalid-bearer.sh` owns the rejection check separately from the unchanged Epic 9 proof. It retains both Aspire readiness waits and the four original routes, shares the proof lifecycle lock, correlates AppHost path/PID, and verifies owned cleanup. It disables inherited curl configuration/proxies for discovered loopback application endpoints, reports only bounded exit/status diagnostics, and permits fallback only after transport failure without an HTTP response. Redirects, wrong HTTP outcomes, incomplete 401 responses, discovery failure and failed cleanup all fail. Only a completed HTTP 401/403 can pass. This does not assert that proxy environment variables caused the historical hosted failure.

All four Builds Tier 1/Tier 2 VSTest/MTP loops isolate test-child stdin and require a freshly produced, parsed TRX with consistent nonzero executed results after every invocation, including exit zero. A file timestamp marker uses the same filesystem clock as the report. Existing aggregation of ordinary test failures and fail-fast infrastructure behavior remain. The runtime harness consumes stdin in every fake child and exercises four-project execution, missing/stale/malformed/zero-result reports, inconsistent counters, failed results with a zero exit, and same-basename stale evidence.

| Local command / check | Observed result |
| --- | --- |
| `dotnet tool restore` | Exit 0; pinned DocFX 2.78.5 and dotnet-stryker 5.0.0 restored |
| `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1` | Exit 0; zero warnings/errors |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*DocsSiteValidationTests'` | Final run: 64 passed / 0 failed / 0 skipped / 0 errors; earlier 63/1 run exposed the subsequently corrected PowerShell location-path leak |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1` | Exit 0; zero warnings/errors |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*CiGovernanceTests'` | 86 passed / 0 failed / 0 skipped / 0 errors |
| From `tests/e2e`: `npm run test:story-10-2` | 3 passed / 0 failed / 0 skipped |
| From `tests/e2e`: `npm run test:epic-9-evidence` | 161 passed / 0 failed / 0 skipped, including 23 new rejection-helper checks; prior proof/validator inventory retained |
| From Builds: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` | Exit 0; 287 assertions passed |
| `HexalithFrontComposerFromSource=true dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` | Exit 0; zero warnings/errors; the same source-graph build settings as the existing Epic 9 runner |
| `./eng/verify-apphost-invalid-bearer.sh` | **Exit 1**; four failed paths, each with two curl exit-28 / HTTP-000 transport attempts; zero HTTP 401/403 observations |
| `aspire ps --format Json --non-interactive --nologo` after the live attempt | `[]`; owned AppHost cleanup verified |
| `bash -n eng/verify-apphost-invalid-bearer.sh`; `python3 eng/validate-story-artifacts.py`; `git diff --check` in root and Builds | Passed |

The live failure persists after the complete source rebuild. During that exact owned run, `aspire describe eventstore --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --non-interactive --nologo` showed EventStore `Finished`; its private `aspire logs eventstore ... --format Json --tail 30` capture contains `OptionsValidationException`: `Authentication:WorkloadIssuer:ClientId` and `Authentication:WorkloadIssuer:ClientSecret` must be configured when JWT Authority is set. The host's `Program.cs` wires `WithJwtBearerSecurity` but never `WithEventStoreWorkloadClientCredentials`; its shipped local realm has three clients, all with service accounts disabled. Aspire's readiness command had returned success before the endpoint probes, so the later finished backend and advertised proxies must not be treated as actual HTTP rejection. This is the demonstrated current local startup prerequisite and resulting unreachable backend, not proof of the earlier hosted transport cause. No authentication setting was supplied, disabled or altered to make the check pass. Correction requires a separately reviewed authentication-scope amendment.

Raw build/test logs, private Aspire discovery/log captures and read-only GitHub run metadata remain under `/tmp/story13-6-*`. They are local diagnostics, not committed evidence packets. The full DocFX publication build, complete browser matrix, complete default bUnit lane and a successful live authentication check were not established by this focused run.

### Shared workflow publication and pin-adoption review preparation

The prepared repair is the working diff in `references/Hexalith.Builds/.github/workflows/domain-ci.yml` and `Tools/test-domain-workflow-test-platforms.ps1` against Builds `1783c0a377615063f3222c1d28f3ab513af4e72f`, supported by the 287-assertion runtime suite. FrontComposer still consumes `.github/workflows/domain-ci.yml@4eb33928a1d8c7775f97221cf9edc171db0cb5f8` in `.github/workflows/ci.yml`, independently of its Builds gitlink. That consumed revision predates additional existing workflow/test changes at Builds HEAD; adopting HEAD plus this patch needs review of the complete consumed-to-proposed revision diff, not merely this repair.

Separate publication review must choose and publish an exact Builds revision containing the repair, rerun its workflow contract suite on that revision, and review its complete source closure. For FrontComposer adoption, prepare the resulting exact caller/reusable/action identity and blob/closure hashes through the existing `eng/dependency_handoff.py ... draft-evaluator --stage ci` workflow, authorize the necessary evaluator tuple in `eng/dependency-graph-policy.json` through an earlier active-base policy revision, and only then activate the caller pin in a later candidate. Use the actual event base for `eng/dependency_graph.py ... diff` and the hosted handoff; an unpublished local intermediate policy commit cannot substitute for the hosted active base. A root gitlink change does not adopt the CI workflow, and no gitlink/catalog or release-workflow change is implied. No future publication SHA, evaluator authorization or successful hosted result is fabricated here.

After authorized publication/adoption, hosted acceptance still requires nonzero fresh passing results for Cli.Tests, Contracts.Tests, Contracts.UI.Tests and Testing.Tests individually, plus complete Quality execution including docs/default bUnit, Chromium and the four live 401/403 responses. Existing `HOST-136-CI-PROJECTS`, `HOST-136-QUALITY-GOVERNANCE`, `HOST-136-CHROMIUM-DOCS` and `HOST-136-EPIC9-REJECTION` therefore remain open for hosted closure.

Read-only inspection of the previously linked baseline runs now shows [CI 40231562358](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562358) completed **success** and [Quality 40231562097](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562097) completed **cancelled**, with build-and-test failed. Both ran on `9c4166e4b7cb45e2512e2b6db887a071f35ce244`, not the repair. These statuses establish no missing-project completeness or repair acceptance; prior candidate/attempt evidence remains unchanged. No rerun was requested.

All seven MAN-136 records retain **Pass — Owner-reported** with their existing formal qualification gaps. The eighteen detailed human-observation boxes, remaining independent owner gates, original parent acceptance tasks and story/sprint **in-progress** status are unchanged.

## Hosted dependency publication and verification — 2026-10-25

**The selected Parties revision is published and hosted submodule checkout now passes. Story 13.6 remains in-progress.** The approved “apply recommended” decision authorizes only the existing Parties commit publication and failed-job reruns on FrontComposer `cfa9febc2942666265f23f87b26112c846f625ad`. That exact root selects Parties `d915a5893715328ef5bdf6a740888b6d212257ca`; no dependency selection, source, workflow, test or baseline changed. This documentation reconciliation is local and is not part of the hosted candidate. The original story baseline remains `3c8679628ebdb8995afc09f712a04fcde623695c`.

Before publication, the target branch was absent. A nonrecursive fetch confirmed Parties remote main at `986a369ec26f6c5194593eb7bce12955eda54149`, with one selected local commit versus ten main commits since common parent `1ef274ea0471643d83f924147e85ac8326cf6a47`. The existing commit's exact full message passed the owning pinned commitlint 21.2.2 with zero problems/warnings. The explicit, non-forced refspec created only `refs/heads/fix/story-13-6-probe-archive` on its existing origin. A post-push `ls-remote` and a fresh isolated bare-store fetch both resolve exactly to `d915a5893715328ef5bdf6a740888b6d212257ca`. Both main branches and all root gitlinks remain unchanged.

### Hosted attempts on the exact candidate

The original [CI attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/attempts/1) and [Quality attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/attempts/1) remain failed history. CI build, Quality build-and-test and Epic 9 stopped before tests with `upload-pack: not our ref d915a5893715328ef5bdf6a740888b6d212257ca` and submodule initialization exit 128. CI dependency-governance was skipped. Quality's other outcomes are preserved below, all on root `cfa9febc2942666265f23f87b26112c846f625ad`.

| Quality attempt 1 job | Actual outcome |
| --- | --- |
| [Windows accessibility-visual](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420497) | Success: `npm run test:a11y` 355 passed, no failures/skips/flaky cases; accessibility evidence Node suite 29 passed; artifact validation passed |
| [Linux WebKit](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420590) | Success: 505 passed / 0 failed / 22 skipped / 0 flaky; artifact validation passed |
| [Debug-gated tests](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420559) | Success: 103 passed / 0 failed / 0 skipped |
| [Committed-files guard](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420566) | Success |
| [Linux Chromium](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420576) | Failure: 525 passed / 1 failed / 1 skipped / 0 flaky; migration-document wording assertion, including two failed retries; artifact validation also fails on the unsuccessful test outcome |
| [Linux Firefox](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420634) | Cancelled during browser installation before tests; downstream artifact checks fail without reports. No passing test lane is claimed |

`gh run rerun --failed` succeeded for both runs. [CI attempt 2](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/attempts/2) is **successful** on the exact candidate: [build-and-test](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/job/121238731951) reports **157 Cli.Tests passed / 0 failed / 0 skipped**, and [dependency-governance](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/job/121239807438) passes with zero graph changes and zero affected modules. Its optional performance and Aspire jobs are **skipped**, not passed. The build log explicitly records checkout of Parties `d915a5893715328ef5bdf6a740888b6d212257ca`. The shared step receives four project paths, but the execution log and [blocking-test-results artifact](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/artifacts/12608301299) contain only the Cli.Tests run and its single 157-test TRX. Execution of the configured Contracts.Tests, Contracts.UI.Tests and Testing.Tests projects is not evidenced by this CI attempt; workflow success does not establish those missing runs.

[Quality attempt 2](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/attempts/2) completes **failure** on the same candidate, with five successful jobs and three failed jobs. Root submodule initialization passes in build-and-test and Epic 9. [Firefox](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121238739143) succeeds with **505 passed / 0 failed / 22 skipped / 0 flaky**, and accessibility artifact validation passes. Build-and-test, Epic 9 and Chromium fail independently as detailed below. GitHub carries the four original successful Quality jobs into the rerun with their original execution timestamps; those passes remain attempt-1 execution evidence and are not claimed as fresh executions.

The first [Quality build failure](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121238739036) is Gate 2b, **537 passed / 1 failed / 0 skipped**. `DocsSiteValidationTests.FluentContingencyPublicationCoversReviewRegressionsAsync("UnrelatedDocfxExclusion")` expects validator exit 0 but receives 1 with `docs/docfx.json: must publish the Fluent contingency page at its documentation path.` The test adds only `reference/**/*.md` to exclusions; the page is at the documentation root. `Test-DocFxExclusion` loads pinned DocFX 2.78.5's `Docfx.Glob.dll`, while the workflow restores tools only in the later Gate 2d. An isolated execution of that unchanged function returns False/exit 0 with the installed matcher and fails on the missing `docfx/2.78.5/tools` path with an empty package cache. This supports a missing-prerequisite/order explanation; the hosted validator's generic catch suppresses the original exception, so the actual runner cache state is not proven. No source or workflow correction is included. The subsequent quarantine summary fails without its skipped lane's TRX evidence, and coverage summary reports `No coverage files found`; those are consequences of the earlier gate failure. Gate 3a/default bUnit and later sequential gates are skipped and remain unqualified.

The [Epic 9 attempt-2 job](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121238738882) also fails independently, after successful `npm run test:epic-9-evidence` (**138 passed / 0 failed / 0 skipped**) and `./eng/run-epic9-live-proof.sh` (**1 browser test passed**, exact-candidate artifact validation passed). The later `AppHost rejects invalid bearer tokens` step restarts the AppHost, observes healthy security/eventstore resources, then receives curl status `000` for all four checks: command submit, command status, query and projection-hub negotiation. The required 401/403 rejection responses were not observed; the silent request loop does not expose the underlying endpoint/transport cause. This is an unresolved security-verification failure, not evidence that invalid credentials were accepted. Artifact upload succeeds. The successful live proof does not turn this job green or supply independent owner acceptance.

The [Chromium attempt-2 job](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121238739153) repeats the original **525 passed / 1 failed / 1 skipped / 0 flaky** outcome, including two failed retries of the same assertion; the artifact validator again rejects the unsuccessful outcome. The Chromium blocker is independent of Parties publication: `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts:70` requires the literal `migration stubs` in `docs/how-to/migration-guides.md`, whose current body does not contain it. A focused browserless local reproduction on unchanged candidate inputs reports **0 passed / 1 failed / 0 skipped / 0 flaky**, zero retries, with the same error. This task records the demonstrated documentation-contract mismatch; it does not modify the test or migration guidance. R8-B1 remains deferred.

### Follow-up handoff and dependency retention

These stable local references remain open. Required maintainer roles below are **unassigned**; no person or approval is inferred. Closure requires recorded evidence for the actual follow-up candidate/run and does not close manual or independent acceptance gates.

| Reference | Required maintainer role (unassigned) | Next action and closure condition |
| --- | --- | --- |
| `HOST-136-CI-PROJECTS` | Shared CI workflow maintainer | Diagnose why only Cli.Tests executed from the four-project input. Close when an authorized hosted run records nonzero execution and passing TRX/counts for Cli.Tests, Contracts.Tests, Contracts.UI.Tests and Testing.Tests individually. |
| `HOST-136-QUALITY-GOVERNANCE` | FrontComposer docs/CI maintainer | Check DocFX prerequisite availability before Gate 2b and expose the underlying validator failure; retain the current inference limit. Close when the unchanged publication regression contract, including `UnrelatedDocfxExclusion` and exclusion negatives, passes and Quality build-and-test completes its required later gates. |
| `HOST-136-CHROMIUM-DOCS` | FrontComposer docs/test maintainer | Reconcile migration guidance with its required contract wording. Close when the named historical-label assertion and complete Chromium job/artifact validator pass without weakening the assertion. |
| `HOST-136-EPIC9-REJECTION` | AppHost/security CI maintainer | Diagnose endpoint reachability after restart and capture actual HTTP responses. Close when all four existing invalid-bearer probes return 401/403 and the complete Epic 9 job passes while retaining the live-proof checks. Status 000 is not an authentication acceptance result. |
| `DEP-136-PARTIES-RETENTION` | Parties repository maintainer, coordinated with FrontComposer maintainer | Keep `refs/heads/fix/story-13-6-probe-archive` and selected object `d915a5893715328ef5bdf6a740888b6d212257ca` reachable while published FrontComposer revisions select it. Do not prune or move that branch while this retention condition applies. This continuing obligation closes only when no published revision depends on this object's availability; no retention assignment or additional remote action is claimed here. |

### Firefox/WebKit skip scope

The exact Firefox attempt-2 job `121238739143` and WebKit attempt-1 job `121219420590` logs each contain **22 skipped test identities**: 20 restricted to Chromium, one reserved for the live-proof host, and one unsupported engine capability. The inventory below comes from those archived job logs; reasons come from the matching tracked skip conditions at `cfa9febc2942666265f23f87b26112c846f625ad`. Paths are under `tests/e2e/specs/`; line numbers identify each source test, not a newly executed check.

| Count per engine | Source test identities / scope | Existing skip reason |
| --- | --- | --- |
| 3 | `adopter-historical-label-cleanup.spec.ts:70,88,113` — wording, version/API facts, internal provenance | Documentation coverage runs once in Chromium. Firefox/WebKit success cannot clear the Chromium wording failure. |
| 5 | `cli-text-output-parity.spec.ts:22,140` — inspect/migrate parity; `diagnostic-catalog-inspect.spec.ts:19,66,90` — severity, invalid input, malformed-sidecar redaction | CLI coverage runs once in Chromium. |
| 4 | `hfcm9002-production-emission-decision.spec.ts:36,48,67,116` — decision, normal-build claims, synthetic input and hostile-path redaction | Repository-governance coverage runs once in Chromium. |
| 6 | `level-3-field-slot-overrides.spec.ts:69`; `level-4-full-view-overrides.spec.ts:69`; `override-accessibility-safety-diagnostics.spec.ts:33,56`; `policy-gated-command-authorization.spec.ts:293,325` | Dedicated override/diagnostic/rejection-host coverage runs once in Chromium; these skips provide no equivalent Firefox/WebKit behavior proof. |
| 1 | `command-form-generation.spec.ts:104` — invalid editors expose state/error on the focusable control | Chromium accessibility-tree evidence, Story 13.3 BH3-08. |
| 1 | `specimen-accessibility.spec.ts:362` — Story 11.5 scoped visual hooks under normal/reduced motion | Computed-style pins and reduced-motion emulation are Chromium-serialized. |
| 1 | `epic-9-fresh-row-acceptance.spec.ts:13` — generated create/update convergence | Exact AM-21 counts require Aspire live-proof timing; the general stub-server lane lacks `PLAYWRIGHT_SKIP_WEBSERVER`. The separate Chromium live proof passes; this supplies no Firefox/WebKit live proof. |
| 1 | Firefox: `responsive-accessibility.spec.ts:91`, supported bevel paint; WebKit: `accessibility-state-matrix.spec.ts:448`, `overflow clip margin` case | Firefox cannot paint `corner-shape:bevel`; WebKit lacks expanded `overflow-clip-margin`. Their skipped negative controls are not passes; independently executed controls retain only their own scope. |

Raw inventories remain in `attempt-2-job-121238739143.log` and `attempt-1-job-121219420590.log` under the scratch directory below. These 22 skips per engine are separate from the historical four skips in the narrower 1,065-case accessibility selection; they do not establish manual, native-zoom or actual WebKit system-palette observations.

The Windows hosted result supplies automated execution for its stated Chromium and Node scope. It does not supply manual Windows AT observations or other unexecuted platform combinations. WebKit's actual system-palette paint, live-backend acceptance, manual qualification and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independently open. All seven existing MAN-136 records retain **Owner-reported Pass** with canonical qualification `blocked`; no additional date/build/tester/version/device/measurement/speech facts were supplied. All eighteen detailed observation boxes and the original matrix/result/Evidence acceptance tasks remain unchecked. The prior 1,061 browser passes/four skips, 1,411 SourceTools passes and 3,785 Shell passes keep their original candidate attribution; this continuation does not relabel them as new hosted results.

### Publication and diagnostic commands

Hosted commands retain their checked-in scope. The shared CI step uses `dotnet test "$proj" --no-build --configuration Release --results-directory "TestResults/${name}" --report-xunit-trx --report-xunit-trx-filename "${name}.trx"`; the single observed project is `tests/Hexalith.FrontComposer.Cli.Tests`. Quality Gate 2b executes `dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter-trait "Category=Governance" --filter-not-trait "Category=GovernanceBuild" --ignore-exit-code 8 --results-directory ./TestResults/governance --report-xunit-trx`. Its Linux browser command from `tests/e2e` is `npx playwright test --project=<engine> --workers 2`; the configured CI retries remain two. These are hosted commands, not additional local executions.

From Parties, after inspecting its clean working tree, branch, remotes, history and tracked guidance:

```bash
git -c submodule.recurse=false fetch --no-tags --no-recurse-submodules origin main
git rev-list --left-right --count d915a5893715328ef5bdf6a740888b6d212257ca...FETCH_HEAD
git ls-remote origin refs/heads/fix/story-13-6-probe-archive
git -c push.followTags=false -c submodule.recurse=false push --porcelain origin d915a5893715328ef5bdf6a740888b6d212257ca:refs/heads/fix/story-13-6-probe-archive
git ls-remote origin refs/heads/fix/story-13-6-probe-archive
```

The commit-message check ran from `/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Parties`. The following reproduces the actual Git/CLI argv and byte-preserving message capture; all commands exited 0, `--version` returned `@commitlint/cli@21.2.2`, and validation reported zero problems/warnings (`existing-message.txt`, `commitlint.log` in scratch). No message or commit was changed.

```bash
git show -s --format=%B d915a5893715328ef5bdf6a740888b6d212257ca > /tmp/fc-13-6-hosted-9cc_j2d7/existing-message.txt
/home/administrator/projects/hexalith/frontcomposer/node_modules/.bin/commitlint --version
/home/administrator/projects/hexalith/frontcomposer/node_modules/.bin/commitlint --edit /tmp/fc-13-6-hosted-9cc_j2d7/existing-message.txt --verbose
```

The isolated availability check ran from `/home/administrator/projects/hexalith/frontcomposer`; each exact command below exited 0, and the last printed the selected full SHA. Output is retained in `parties-fetch-verification.log`. For a fresh reproduction, substitute a newly created directory from `mktemp -d /tmp/fc-13-6-availability.XXXXXX` for the recorded bare-store path throughout; reusing an existing store would not prove fresh acquisition. This is a fetch recipe, not another publication.

```bash
git init --bare /tmp/fc-13-6-hosted-9cc_j2d7/parties-availability.git
git -C /tmp/fc-13-6-hosted-9cc_j2d7/parties-availability.git -c submodule.recurse=false fetch --no-tags --no-recurse-submodules --depth=1 https://github.com/Hexalith/Hexalith.Parties.git d915a5893715328ef5bdf6a740888b6d212257ca
git -C /tmp/fc-13-6-hosted-9cc_j2d7/parties-availability.git rev-parse FETCH_HEAD
```

From FrontComposer:

```bash
gh run rerun 40210145439 --failed --repo Hexalith/Hexalith.FrontComposer
gh run rerun 40210145120 --failed --repo Hexalith/Hexalith.FrontComposer
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120
python3 eng/dependency_graph.py --root . validate --commit cfa9febc2942666265f23f87b26112c846f625ad
```

The general run API commands above are preserved as execution history; they can return a later attempt in future. Attempt-specific job inspection also executed from the FrontComposer cwd with exit 0:

```bash
gh api 'repos/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/attempts/2/jobs?per_page=100'
gh api 'repos/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/attempts/2/jobs?per_page=100'
```

Their exact replies are archived as `40210145439-attempt-2-jobs.json` and `40210145120-attempt-2-jobs.json` in `/tmp/fc-13-6-hosted-9cc_j2d7/`. The terminal general-run replies were preserved byte-for-byte as `40210145439-attempt-2-run.json` (SHA-256 `e3142076e5964b4efef43fd0b6ae7642fadf7414cc6a719585a999f9e8309ef0`) and `40210145120-attempt-2-run.json` (SHA-256 `90e3140a2bfb4d3b16b9f35ca14824993e55b6a8df0dd569ab905479a553036a`). Both state `run_attempt: 2`, the exact candidate above and `status: completed`, with CI `success` and Quality `failure`. These archived replies, fixed job IDs and attempt links own this record; later reruns cannot replace its attribution. No workflow was rerun for this review correction.

The committed graph command exits 0: **51 edges and seven semantic selectors**, without a policy override. From `tests/e2e`, the diagnostic command is:

```bash
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-13-6-hosted-9cc_j2d7/docs-reproduction/results FC_E2E_HTML_REPORT_DIR=/tmp/fc-13-6-hosted-9cc_j2d7/docs-reproduction/html npx playwright test specs/adopter-historical-label-cleanup.spec.ts --project=chromium --workers=1 --retries=0 --grep 'keeps current adopter-facing docs'
```

Final documentation checks: `python3 eng/validate-story-artifacts.py` and `git diff --check` pass. Both frozen intent blocks, all seven existing manual records and eighteen boxes, all 69 tracked PNG/Verify files, and all eight root gitlinks retain their original contents/identities. R8-B1 remains deferred. The DocFX diagnostic below is causal evidence, not a fresh SourceTools suite run.

The DocFX probe originally copied the unchanged tracked `Test-DocFxExclusion` function from `eng/validate-docs.ps1`, with `$ErrorActionPreference = 'Stop'` and `$RepoRoot = $args[0]`. This recipe reconstructs the same probe from the exact commit, using the matching checkout/`.config/dotnet-tools.json` (DocFX 2.78.5). Both original invocations ran from `/home/administrator/projects/hexalith/frontcomposer`; `dotnet nuget locals global-packages --list --force-english-output` returned `/home/administrator/.nuget/packages/`. No package installation or update was performed.

```bash
python3 - <<'PY'
from pathlib import Path
import subprocess
scratch = Path('/tmp/fc-13-6-hosted-9cc_j2d7')
scratch.mkdir(parents=True, exist_ok=True)
source = subprocess.check_output(['git', 'show', 'cfa9febc2942666265f23f87b26112c846f625ad:eng/validate-docs.ps1'], text=True)
newline = chr(10)
function = source[source.index('function Test-DocFxExclusion('):source.index(newline + 'function Assert-FluentContingencyPublication(')]
(scratch / 'docfx-exclusion-probe.ps1').write_text(newline.join([
    "$ErrorActionPreference = 'Stop'", '$RepoRoot = $args[0]', function,
    "Test-DocFxExclusion 'fluent-ui-v5-contingency.md' @('reference/**/*.md')", '']))
empty = scratch / 'empty-nuget-cache'
empty.mkdir(exist_ok=True)
assert not any(empty.iterdir()), 'The negative case requires an empty cache.'
PY
NUGET_PACKAGES=/home/administrator/.nuget/packages/ pwsh -NoProfile -File /tmp/fc-13-6-hosted-9cc_j2d7/docfx-exclusion-probe.ps1 /home/administrator/projects/hexalith/frontcomposer
NUGET_PACKAGES=/tmp/fc-13-6-hosted-9cc_j2d7/empty-nuget-cache pwsh -NoProfile -File /tmp/fc-13-6-hosted-9cc_j2d7/docfx-exclusion-probe.ps1 /home/administrator/projects/hexalith/frontcomposer
```

The tested page was exactly `fluent-ui-v5-contingency.md`, with the sole exclusion `reference/**/*.md`. The installed pinned matcher returned **False, exit 0**; the empty-cache invocation returned **exit 1**, unable to find `empty-nuget-cache/docfx/2.78.5/tools`. Explicit `NUGET_PACKAGES` makes the extracted function's `Invoke-Process` fallback unnecessary in both cases. Raw script/output remain in scratch (`docfx-exclusion-probe.ps1`, `docfx-installed-probe.log`, `docfx-empty-probe.log`). The hosted catch still hides its underlying exception: these controlled outcomes support the missing-prerequisite/order explanation but do not prove the hosted runner cache state.

Raw publication, commitlint, remote-fetch, graph, hosted API/job logs and the failed local diagnostic remain external scratch under `/tmp/fc-13-6-hosted-9cc_j2d7/` or the linked CI runs. No new commit, main update, documentation publication, dependency update or nested-submodule initialization occurred.

## Human-reported verification records — 2026-10-25

**Owner-reported result: Pass for all seven manual records.** Jérôme Piquot, previously identified as Owner, confirmed: “add all seven manual records, all is OK”. This follow-up explicitly applies the reported Pass to NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari, actual 400% browser zoom, physical tablet, physical phone and zero-field feedback. Each existing stable record below now retains that confirmation individually.

The earlier statement, “create all the records, everything is ok”, remains the initial overall report. That 2026-10-25 follow-up supplies an all-seven result confirmation. It does not supply execution dates/builds, tester identities, software versions, device models, measured geometry or itemized speech observations. Those fields remain explicitly not supplied. The [2026-10-27 Owner amendment](#story-specific-manual-acceptance-amendment) renews the all-seven Pass confirmation and waives completion of those supporting details. **Current formal qualification is `completed` under that story-specific amendment; the reported results are Pass.** The earlier missing-details blocker is superseded.

The following shared fields apply to every record below:

| Field | Recorded value |
| --- | --- |
| Task / story | 13.6 |
| Release branch | `main` |
| Candidate reference | Checkout when recording the all-seven confirmation: `39cab9cb19f15e6d743db63708a1a137df348233`; exact tested runtime/build not supplied |
| Date | Initial report and all-seven confirmation received 2026-10-25; renewed confirmation and waiver received 2026-10-27; actual execution date not supplied |
| Reporting human / record owner | Jérôme Piquot, Owner |
| Actual per-run tester | Not supplied for each pairing/device; supporting detail waived by the Owner; the assistant is not entered as tester |
| Reported result | Pass for each of the seven named gates, attributed to “add all seven manual records, all is OK” and renewed by “No need to complete. All Ok today” on 2026-10-27 |
| Canonical status | `completed` — Owner-reported Pass; supporting execution details waived by the dated Story 13.6 amendment |
| Reviewer / approval reference | Jérôme Piquot's [named Owner approval](#named-owner-approval--2026-10-25) and the direct all-seven confirmation plus [2026-10-27 amendment](#story-specific-manual-acceptance-amendment); no additional approving role is inferred |
| Evidence reference | Exact initial and follow-up human statements quoted in this section; no per-run notes, screenshots or recordings supplied |
| Sanitization | This record contains the supplied name/role/report and test references; no raw AT transcript, tenant data, payload, secret or attachment is included |
| Release impact | These seven manual records are qualified under the Owner amendment; Story 13.6 remains in-progress for independent acceptance gates |
| Former blocker reference | `MAN-136-REPORT-DETAILS-2026-10-25`, superseded by the [Owner amendment](#story-specific-manual-acceptance-amendment) |
| Decision / next action | Retain each confirmed Pass and the supporting-details waiver; no further completion of those details is requested |
| Reopen / revalidation trigger | A material implementation/candidate change requiring revalidation or a subsequent Owner decision; the waived supporting details alone do not reopen these reports |

### Required execution details

The Owner has confirmed Pass for all seven records, including actual zoom and zero-field feedback, and on 2026-10-27 waived completing the supporting execution details requested under the existing [manual log template](../../docs/accessibility-verification/manual-log-template.md). Those details remain not supplied and are no longer a qualification blocker for these seven existing reports. The eighteen detailed evidence checkboxes remain unchecked because no itemized measurements, exception inventories or speech observations were supplied; the story-specific waiver prevents that inventory from reinstating the same metadata blocker.

### MAN-136-NVDA

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-NVDA` |
| Acceptance criteria | AE-01–AE-09 and the manual-evidence acceptance criterion |
| Required pairing / mode | NVDA with Firefox on Windows |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Common operator journey and the missing-item, command-failure and direct/silent/new-message speech sequences. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual Windows, Firefox and NVDA versions; test date/tester; itemized keyboard, speech, spacing, theme and overlay observations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-JAWS

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-JAWS` |
| Acceptance criteria | AE-01–AE-09 and the manual-evidence acceptance criterion |
| Required pairing / mode | JAWS with Chrome on Windows |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Common operator journey and the missing-item, command-failure and direct/silent/new-message speech sequences. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual Windows, Chrome and JAWS versions; test date/tester; itemized keyboard, speech, spacing, theme and overlay observations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-VO

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-VO` |
| Acceptance criteria | AE-01–AE-09 and the manual-evidence acceptance criterion |
| Required pairing / mode | VoiceOver with Safari on macOS |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Common operator journey and the missing-item, command-failure and direct/silent/new-message speech sequences. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual macOS and Safari versions, VoiceOver version if separately reported; test date/tester; itemized reading, keyboard, speech and overlay observations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-ZOOM

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-ZOOM` |
| Acceptance criteria | AE-02, with AE-01/03–09 journeys repeated as applicable |
| Required pairing / mode | Actual 400% browser zoom from a measured 1280 CSS-pixel reference |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Navigation, forms, grids/detail, palette and Settings at actual browser zoom, including complete target/focus visibility and bounded labelled scrolling. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual OS/browser versions; test date/tester; measured 100% reference width and zoom geometry; exercised routes/overlays. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-TABLET

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-TABLET` |
| Acceptance criteria | AE-01–AE-09 and the real-device/manual-evidence acceptance criterion |
| Required pairing / mode | Physical tablet, touch-adapted tier |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Touch navigation, target activation, fields, toolbar/popovers, grid/detail, palette/dialogs, lifecycle/recovery and rotation. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual tablet model, OS/browser versions, orientations and viewport; test date/tester and exercised operations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-PHONE

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-PHONE` |
| Acceptance criteria | AE-01–AE-09 and the real-device/manual-evidence acceptance criterion |
| Required pairing / mode | Physical phone, functional-fallback tier |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Essential operations, detail-exposed row actions, readable statuses and bounded two-dimensional scrolling; functional fallback rather than full daily-use support. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual phone model, OS/browser versions, orientations and viewport; test date/tester and exercised operations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

### MAN-136-ZERO-FIELD

| Field | Recorded value |
| --- | --- |
| Stable gate id | `MAN-136-ZERO-FIELD` |
| Acceptance criteria | AE-09, Story 13.6 approved zero-field decision and manual-evidence acceptance |
| Required pairing / mode | Generated zero-field command using an actually exercised supported AT/browser pairing |
| Canonical status | `completed` — Owner-reported Pass; supporting details waived by the [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) |
| Operating system / browser / AT or device actually used | Exact versions/model not supplied; the record's required pairing or mode is covered by the Owner's all-seven Pass confirmation |
| Specimen route / flow and responsive tier | Required scope: Validation, permission/unavailable/rate-limit/transport and unmapped failures, blank problem details, Retry-After, scope/policy denial, repeat/navigation/scope clearing and accepted-command lifecycle. Required outcome is readable feedback, retained trigger focus and one spoken message; actual itemized observations have not been supplied. Owner reports Pass for this record; actual routes/tier are not itemized |
| Observed result supplied | **Pass — Owner-reported.** “add all seven manual records, all is OK” (2026-10-25); no itemized speech/focus/geometry account supplied |
| Unreported details (waived for qualification) | Actual OS/browser/AT versions; test date/tester; itemized failure/accepted-command, focus-retention and heard-speech observations. |
| Issue / resolution | Owner reports Pass and no defect; record qualified under the dated supporting-details waiver |
| Shared identity, owner, evidence and waiver fields | Shared fields and `MAN-136-REPORT-DETAILS-2026-10-25` above apply to this record |

All seven records carry an explicit Owner-reported Pass, renewed on 2026-10-27. Their current canonical qualification is `completed` under the Owner's supporting-details waiver. The live MAN-136 summary below matches these records. Earlier dated missing-details, absent-result and unperformed statements are historical and superseded for these seven reports. The eighteen detailed observation boxes remain an unchecked inventory, while separate hosted/platform/full-topology and gate-specific acceptance requirements remain open; story and sprint stay **in-progress**.

## Named Owner approval — 2026-10-25

**Owner approval: approved by Jérôme Piquot (Owner).** The direct statement in this conversation is: “I Jérôme Piquot, Owner, approve”. This supplies the named Owner approval for Story 13.6's implementation and reported automated verification in the [A3 completion](#approved-a3-technical-completion--2026-10-25) and [automated gap closure](#automated-gap-closure--2026-10-25) records. Earlier statements that a named Owner approval had not been supplied are historical for this scope.

The reported candidate is root `29a5ac9401c95a68ebe679989b6bd9d343e10351`, tracked `src tests eng` patch SHA-256 `7ab2ca566909d6ecd0f86ac214f429f91684c5cc1b8588db31de23c2e6a3dd9c`, and Fontconfig raw SHA-256 `c9a0ab0b3d8a3cad57e1c86a408f9e76aceddb0464956b62cea4c3a79530f89b`. During recording, the checkout advanced to published commit `76e063b56a9ef2a0d976926d3e8aac89b08364c2`. A raw-file comparison and the original patch comparison excluding the separately hashed font confirm that it preserves the reported implementation inputs. The approval is recorded against that work and its landed revision; existing test results keep their original source identities and reuse limits. This statement does not claim a new test run on the landed commit.

**Accessibility acceptance remains incomplete.** All eighteen human checklist boxes and seven MAN-136 rows remain **unperformed**. Human observations, per-run manual sign-offs, platform/live-backend evidence, hosted-CI qualification and gate-specific OI-16/G-4/FLUENT-APP-1/Product/Architect decisions retain their existing requirements. The named Owner approval supplies acceptance of the reported work; it does not supply missing observations or amend the frozen acceptance criteria. Story and sprint remain **in-progress** until every acceptance criterion is met.

## Approved A3 technical completion — 2026-10-25

**All A1–A6 technical tasks are complete; Story and sprint remain in-progress because mandatory human evidence and independent owner acceptance are missing.** The user's subsequent approval explicitly authorized the Parties historical-probe correction and the bounded local commits needed to validate later-base activation. It did not authorize pushing, publishing, committing the accessibility work, or closing any other owner gate. This section owns current A3 attribution; prior sections retain their original candidates, failures and commands.

| Local revision | Exact scope |
| --- | --- |
| Parties `d915a5893715328ef5bdf6a740888b6d212257ca` | Remove the three historical executable probe files and update only their README, four approved paths |
| FrontComposer `db4fdc50e17292305812e8192aa872f13abaf401` | Record only that corrected Parties gitlink |
| FrontComposer `50d9a8b908a5cb5318e58c5bdef9b81d533c229a` | Commit only the two approved semantic policy expectations |
| FrontComposer `29a5ac9401c95a68ebe679989b6bd9d343e10351` | Commit only the approved Aspire hosting expectation; final local activation revision |

The current implementation is final local root `29a5ac9401c95a68ebe679989b6bd9d343e10351` plus remaining tracked `src tests eng` diff SHA-256 `7ab2ca566909d6ecd0f86ac214f429f91684c5cc1b8588db31de23c2e6a3dd9c` and the still-untracked font configuration raw SHA-256 `c9a0ab0b3d8a3cad57e1c86a408f9e76aceddb0464956b62cea4c3a79530f89b`. The original story baseline remains `3c8679628ebdb8995afc09f712a04fcde623695c`. The index is empty; every other accessibility source/test/configuration change and the evolving spec/result remain unstaged.

All four original Parties probe files were copied byte-for-byte outside the checkout before removal; their raw hashes equal the original Git blobs. The retained README links the unchanged historical negative outcome and provides exact retrieval from Parties `1ef274ea0471643d83f924147e85ac8326cf6a47`. No production package declaration, renamed catalog or policy exemption replaces the removed override. Retrieval, absence of the three executable files, four-path scope and absence of other tracked `PackageVersion` overrides were checked. The committed README retains owner-requested CRLF, with the normal whitespace checks preserved through `git -c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol diff HEAD^ HEAD --check`.

Every exact full commit message was validated before use with **commitlint 21.2.2**, matching both owners' lockfiles and using each owning cwd/config. The already installed root CLI also served Parties; no dependency installation or update occurred. All four validations report zero problems/warnings. Root Husky staged-file and commit-message hooks remained active; no hook was bypassed. Successful validation and commit logs are external scratch.

| Fresh A3 verification | Result |
| --- | --- |
| Final exact committed graph | Exit 0; 51 edges, seven semantic selectors |
| Local push-model transition `50d9a8b9…`→`29a5ac94…` | Exit 0; committed base policy, seven selectors on both sides, no graph changes in activation |
| Exact Parties standalone Release/NuGet restore and build | Both exit 0; build zero warnings/errors |
| Shell.Tests Debug build | Zero warnings/errors |
| Focused CiGovernance and InfrastructureGovernance classes | **106 passed / 0 failed / 0 skipped/errors** |
| Configured individual Shell lane | **3,785 passed / 0 failed / 0 skipped/errors** |
| Dependency graph / handoff engineering suites | 102 / 11 passed; no failures |
| Story-artifact validation / diff check | Passed |

Both formerly failing configured governance methods now pass. The final graph digest is `8b00fe9475a5c0a452dd6f99c7256afa6beee89e1492dab2b64b8921d46f4bfc`. The tested transition consumes policy from committed base `50d9a8b908a5cb5318e58c5bdef9b81d533c229a`, whose Git-blob SHA-256 is `0c5cf75b88a16f0cb9fec0a5bff4501f2808f1ea1dd72656ac4edd3313106d71`. No `--policy` substitution was used. Earlier gitlink/policy transition revisions are not claimed as green CI. The transition's `release_eligible` field describes only the local revision model; it is not release authorization.

Parties was materialized at exact corrected revision `d915a589…` using the existing dependency-graph helpers. Its own Builds edge is **`857558036530a389ccb4ba16fa9777416af9b12e`**, not the root's `1783c0a3…`. The bounded regular-file contract contains 2,035 files/35,584,442 bytes and catalog SHA-256 `553796eef36f2789f60bb47bc8902b5fb9f1df5b3a942cde7f0523768bf1d2a1`. The closed registry restore/build argv ran from that isolated owning checkout with only this exact contract materialized. No nested submodule was initialized, and no fallback was needed because both commands pass. This validates the standalone build; it does not claim execution of all Parties test suites or live-backend acceptance.

Exact fresh commands from FrontComposer root, except where stated:

```bash
python3 eng/dependency_graph.py --root . validate --commit 29a5ac9401c95a68ebe679989b6bd9d343e10351
python3 eng/dependency_graph.py --root . diff --event push --event-base 50d9a8b908a5cb5318e58c5bdef9b81d533c229a --candidate 29a5ac9401c95a68ebe679989b6bd9d343e10351
python3 -m unittest discover -s tests/eng -p test_dependency_graph.py -v
python3 -m unittest discover -s tests/eng -p test_dependency_handoff.py -v
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*CiGovernanceTests' -class '*InfrastructureGovernanceTests' -parallel none -result-xml /tmp/fc-story13-6-approved-a3-vra4y_n8/shell-governance.xml
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-approved-a3-vra4y_n8/shell-configured.xml
# From /tmp/fc-story13-6-approved-a3-vra4y_n8/parties-standalone:
dotnet restore Hexalith.Parties.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true
dotnet build Hexalith.Parties.Standalone.slnx --configuration Release --no-restore -p:UseNuGetDeps=true
# From each owning cwd; exact message files are retained beside the logs:
node /home/administrator/projects/hexalith/frontcomposer/node_modules/@commitlint/cli/cli.js --config commitlint.config.mjs --edit /tmp/fc-story13-6-approved-a3-vra4y_n8/party-commit-message.txt --verbose
node /home/administrator/projects/hexalith/frontcomposer/node_modules/@commitlint/cli/cli.js --config commitlint.config.mjs --edit /tmp/fc-story13-6-approved-a3-vra4y_n8/root-gitlink-message.txt --verbose
node /home/administrator/projects/hexalith/frontcomposer/node_modules/@commitlint/cli/cli.js --config commitlint.config.mjs --edit /tmp/fc-story13-6-approved-a3-vra4y_n8/root-policy-message.txt --verbose
node /home/administrator/projects/hexalith/frontcomposer/node_modules/@commitlint/cli/cli.js --config commitlint.config.mjs --edit /tmp/fc-story13-6-approved-a3-vra4y_n8/root-activation-message.txt --verbose
python3 eng/validate-story-artifacts.py
git diff --check
```

The earlier **1,061 browser passes/four capability skips**, eighteen Linux visual passes, **1,411 SourceTools passes**, fourteen automated native zoom cases and twenty-one mobile row-metric stages retain the exact original source attribution in the next section. They were not rerun on the new root. Parent byte audits confirm their UI/helper/font/threshold/image inputs are unchanged, but the current dependency graph has new provenance; no whole-runtime equivalence or new hosted acceptance is inferred. All 69 tracked PNG/Verify baselines remain unchanged. Only the Parties root gitlink changed; the other seven remain at their prior revisions.

Raw archives, commitlint/commit logs, graph/transition JSON and .NET/engineering results remain under `/tmp/fc-story13-6-approved-a3-vra4y_n8/`. Independent parent checks are under `/tmp/fc-story13-6-approved-a3-parent-gi5e_796/`. Nothing was added as a committed evidence packet or runtime-identity record. The owned Aspire topology remains stopped; exact-target describe confirms no AppHost running.

These are **local committed activation/model checks only**. `origin/main` remains at `80782afc7fc2fd33ee365d4601cb6b5e91d215fa`; the tested `50d9…`→`29a5…` transition does not qualify a single batch push from that unchanged origin base. No push, publication or hosted CI run occurred or is authorized here. The earlier [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179812149) and [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179811928) links remain failed historical base-commit observations; there is no hosted result for these local commits.

A3 and its four sub-tasks are complete because the remaining configured governance failure now passes through committed policy. All eighteen human boxes and seven MAN-136 rows remain unperformed. Human AT/heard speech/zoom/device observations, Windows, actual WebKit system-palette paint, live-backend acceptance, hosted publication/CI and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect acceptance remain open. The limited governance approval does not approve Fluent Components/Icons 5.0.1. Original matrix/result/Evidence acceptance tasks stay unchecked; story/sprint remain **in-progress**.

## Automated gap closure — 2026-10-25

**Story and sprint remain in-progress; accessibility acceptance is incomplete.** This section owns current automated attribution and supersedes the earlier visual, prerequisite and complete-browser failures only for the candidate tested here. Historical sections and failed attempts remain unchanged. The canonical story baseline remains `3c8679628ebdb8995afc09f712a04fcde623695c`; the tested root is `80782afc7fc2fd33ee365d4601cb6b5e91d215fa`, plus tracked implementation/configuration patch `sha256:d9fd9618b5c42775320a7f42d673eca3a1f7e50230c392c45411871137cf7ef4` from `git diff --binary 80782afc7fc2fd33ee365d4601cb6b5e91d215fa -- src tests eng | sha256sum`. That command omits the new untracked `tests/e2e/fonts/linux-fontconfig.conf`, whose raw SHA-256 is `c9a0ab0b3d8a3cad57e1c86a408f9e76aceddb0464956b62cea4c3a79530f89b`. Together these identify the tested source. The approved working-tree policy's raw SHA-256 is `f20b5f0307420913d44ed0da6158093f82dc355f9b0a4024be871268dc9a95c9`.

The twelve visual failures were caused by Linux font fallback. Local Ubuntu 26.04 selected Noto Sans where the approved hosted Firefox/WebKit captures match DejaVu Sans; WebKit's remaining code-sample difference disappeared with the matching monospace fallback. The maintained Playwright configuration now supplies the small source-owned Fontconfig file only to Linux Firefox/WebKit, honoring an explicit `FONTCONFIG_FILE`. Chromium, other platforms and Fluent's product font tokens retain their existing configuration. All eighteen Linux visual comparisons pass with the original image bytes and thresholds. Installed DejaVu fonts are an explicit test-environment prerequisite, documented in `tests/README.md`.

The restored fonts exposed a real WebKit pointer defect in generated mobile grids. Fluent's inline fixed table layout squeezed four columns into 62px cells; unwrapped neighboring glyphs intercepted native hits, leaving 14.5px/16px reachable strips. In the existing Phone/Tablet global-scrollbar layout, generated tables now use natural column sizing inside their existing labelled horizontal scroll owner. Desktop retains fixed table layout. No target exception, helper exclusion or threshold was added. Both new themed regressions fail before the correction (six failures across three engines; WebKit fails actual target geometry), then pass with native reachability, text/page loss, labelled scrolling and desktop-restoration assertions. The affected thirty-case browser selection passes. A separate read-only three-engine mobile probe verifies twenty-one stages: Compact rows/ItemSize 32px, combined spacing 47px, Roomy 56px, removal/restoration back to Compact 32px; every physical row equals ItemSize, and desktop sizing restores.

The original Firefox focus failure is retained. Its trace shows a late `pa-0` header class overriding the inline inset. Replaying that observed class reproduces the same −2px ring and four clipping failures with `scrollX` and ancestor `scrollLeft` all zero; restoring the existing inset gives a 10px envelope and no failures. The origin of that historical late class remains unconfirmed; no speculative header edit was made. The final **complete** Firefox selection, including the spaced module-navigation journey, passes on the current layout corrections; isolated repeats are not used as the gate result.

| Final verification | Passed / failed / skipped / flaky |
| --- | --- |
| Complete Chromium accessibility selection | 355 / 0 / 0 / 0 |
| Complete Firefox accessibility selection | 353 / 0 / 2 / 0 |
| Complete WebKit accessibility selection | 353 / 0 / 2 / 0 |
| Complete browser aggregate | **1,061 / 0 / 4 / 0**, 1,065 cases, zero retries |
| Ordinary Linux visual comparisons, included above | 18 / 0 / 0 / 0; approved PNGs unchanged |
| Automated native Chromium 400% browser zoom | 14 / 0 / 0 / 0; rerun after the mobile grid correction |
| Configured Debug SourceTools | 1,411 / 0 / 0; zero runner errors |
| Configured Debug Shell | 3,784 / 1 / 0; zero runner errors; only the committed-policy gate fails |
| Focused packaging/deprecation consumers | 5 / 0 / 0, also included in SourceTools |
| Linux Node evidence suite | 29 / 0 / 0 |
| Dependency graph / handoff engineering suites | 102 / 0 / 0; 11 / 0 / 0 |
| TypeScript; visual governance; story-artifact validator; diff check | Passed |

All final individual Release prerequisite and Debug test-project builds pass with zero warnings/errors. Release was used only for the explicitly authorized packaging prerequisites: Contracts net10.0/netstandard2.0, Contracts.UI, Shell and SourceTools. Ordinary local test projects remain Debug with the tracked dependency modes. The final configured Shell lane confirms `ToolchainPins_MatchApprovedDotnetAndAspireVersions` passes.

The fourteen native zoom cases ran after the mobile CSS correction and before the three approved governance edits. Product, helper and native-probe inputs have not changed since that run; this is scoped reused production evidence, rather than execution of the entire later `src tests eng` fingerprint. The final complete browser and configured .NET gates did execute on the final fingerprint above.

Each engine has 116 required state files and its own run ID/report. All three unchanged artifact validators exit 0. The final results/HTML file bytes are Chromium **14,295,865 / 7,991,243**, Firefox **15,498,734 / 8,444,873**, WebKit **12,705,913 / 7,009,313**; every root stays below **52,428,800** bytes. The four skips retain their existing scope: Firefox bevel capability, WebKit overflow-clip-margin capability, and the two non-Chromium Story 11.5 scoped-style probes. WebKit's actual system-palette paint remains unsupported; capability-bound media-only evidence is not actual palette observation.

The user explicitly approved exactly three governance expectation changes on 2026-10-25: FsCheck.Xunit.v3 3.5.0→3.6.0, ModelContextProtocol.AspNetCore 2.2.0→2.3.0, and expected Aspire hosting/AppHost SDK 13.6.1→13.6.3. Those changes are applied; selected dependencies, the independent Aspire CLI 13.6.0 pin and gitlinks are unchanged. This approval does not approve Fluent Components/Icons 5.0.1 or any other owner gate. The central configured test intentionally still reads committed HEAD policy and fails on FsCheck 3.5.0 versus 3.6.0. FC-DEP-1 requires later-base activation, so the working-tree policy cannot authorize itself. An explicit `--policy` diagnostic with the approved values reaches a separate prohibited `PackageVersion` in Parties commit `1ef274ea0471643d83f924147e85ac8326cf6a47`, at `_bmad-output/implementation-artifacts/tests/five-owner-config-and-recovery-deferred-items-2026-10-23/eventstore-3.127.0-probe/Directory.Packages.props`. That owner correction and later policy activation remain open; no exemption or bypass was added.

Exact commands, from the repository root unless noted:

```bash
aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj -c Release -m:1
dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -c Release -f netstandard2.0 -m:1
dotnet build src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj -c Release -m:1
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*QueryRequestDeprecationTests' -class '*PackagedAnalyzerConsumerTests' -parallel none -result-xml /tmp/fc-story13-6-automated-84ymyif2/final/packaging-focused.xml
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-automated-84ymyif2/final/sourcetools-configured.xml
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-automated-84ymyif2/final/shell-configured.xml
ASPIRE_ENVIRONMENT=Test aspire start --no-build --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json -- --environment Test
aspire wait counter-web --status healthy --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive
python3 /tmp/fc-story13-6-automated-84ymyif2/run-final-browser-gates.py
xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-story13-6-automated-84ymyif2/native-cell-fix/native-zoom.mjs https://localhost:7201
node /tmp/fc-story13-6-automated-84ymyif2/mobile-density-probe.mjs
node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
npm --prefix tests/e2e run typecheck
npm --prefix tests/e2e run validate:visual-governance
python3 -m unittest discover -s tests/eng -p test_dependency_graph.py -v
python3 -m unittest discover -s tests/eng -p test_dependency_handoff.py -v
python3 eng/dependency_graph.py --root . validate --commit 80782afc7fc2fd33ee365d4601cb6b5e91d215fa
python3 eng/dependency_graph.py --root . --policy eng/dependency-graph-policy.json validate --commit 80782afc7fc2fd33ee365d4601cb6b5e91d215fa
aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive
aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
python3 eng/validate-story-artifacts.py
git diff --check
```

The browser script runs from `tests/e2e` and starts one complete invocation per engine concurrently, each with **two workers and zero retries**: `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=<engine> --workers=2 --retries=0`. Each invocation sets `BASE_URL=https://localhost:7201`, `PLAYWRIGHT_SKIP_WEBSERVER=1`, `FC_E2E_BUILD_CONFIGURATION=Debug`, its own `FC_A11Y_EVIDENCE_ENGINE`/`FC_A11Y_EVIDENCE_RUN_ID`, and final per-engine results/HTML paths, then runs `node scripts/validate-a11y-artifacts.mjs` with that same environment. Its existing production-style route guards also pass. Native zoom again verifies 1280×900/DPR1→320×225/DPR4, unchanged outer width, CSS zoom1, visual scale1 and `tabs.setZoom(4)`/`getZoom` with viewport emulation disabled; it remains **automated Chromium evidence**.

The initial exact-target AppHost start succeeds; Counter is Healthy. Explicit Test selection and all existing Test-only timings were inspected before the browser runs. Rebuilds followed exact-target stops, and the final owned topology is stopped; exact-target describe reports no running AppHost. This is specimen verification, not live-backend acceptance. Read-only `gh run view ... --json databaseId,headSha,status,conclusion,url,name` rechecked [CI 40179812149](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179812149) and [Quality 40179811928](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179811928): both completed failure at the exact base above. Neither validates the working-tree patch; no hosted run was triggered.

Raw results remain external scratch under `/tmp/fc-story13-6-automated-84ymyif2/`, with final reports in `final/`. The original twelve visual failures, intermediate font probes, six red grid regressions and pre-cell-correction WebKit **287 passed / 64 failed / 2 skipped** run remain separate. The complete pre-cell sweep belongs to tracked source patch `sha256:f9907216d6754d77cdd6d70c4ee98a1f235476513cd0aaccfa3ef97f9c56a6aa` plus the same new Fontconfig file hash above, based on the same root; it supplies no current-pass attribution. That failed WebKit run also correctly fails the unchanged size gate at **138,348,523 results / 137,829,724 HTML bytes**; it is not relabelled as a pass. Engineering policy logs and both failed exact-policy diagnostics remain under `/tmp/fc-story13-6-parent-y_1aws1j/`. No committed evidence packet, manifest or replay lane was added.

The parent preservation audit confirms all **69 tracked PNG/Verify files**, the frozen intent and all **eight root gitlinks** retain their initial bytes/identities. Existing accessibility corrections and the responsive helper are preserved without a new threshold change. A1, A2, A4, A5 and A6 are implemented/verified; A3 remains partially blocked as described above. All **eighteen human boxes and seven MAN-136 rows remain unperformed**. Human AT/heard speech/zoom/device observations, Windows, actual WebKit system-palette paint, live-backend acceptance, final-candidate hosted CI and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect approvals remain open. No staging, commit or push occurred.

## Native zoom correction and verification — 2026-10-25

**Story and sprint remain in-progress; accessibility acceptance is incomplete.** The current candidate is root `80782afc7fc2fd33ee365d4601cb6b5e91d215fa` plus source/test patch `sha256:59987573c5037c6dd6f0aff57f5fbadb26f62b536de89d22348c84b463b250e2`, measured with `git diff --binary HEAD -- src tests | sha256sum`. This section owns current attribution; earlier dated sections retain their original candidates and outcomes. The user supplied no human logs and requested available checks. Actual native browser zoom was performed automatically; no human AT, heard-speech or physical-device observation is inferred.

The native 400% probe exposed a real short-viewport trap: the wrapped mobile header and footer left only 32px of content height (24px with spacing), while the focused Home card was 88px/101px tall. The shell now selects Fluent's existing global scroll mode for the existing Phone/Tablet tiers and sizes its rows intrinsically, allowing focused controls and chrome to scroll together. Its implicit keyboard scroll stop uses the existing inset Fluent focus stroke. The existing labelled data-grid region remains a bounded scrollport, with maximum block size derived from the viewport and existing scroll insets; row sizing and virtualization remain intact. Desktop/CompactDesktop keep their existing layout mode.

Settings had both measurement and product defects. Accordion geometry now measures its verified native shadow heading/button and retains all real indicator paint, rather than the expanded panel's entire host. Target inventory scrolls offscreen controls in fixed modal scrollports and returns them to a common coordinate frame. An already evidenced native circular radio Spacing exception verifies its full circular hit area, rather than treating transparent square corners as covers; the 24px square requirement, spacing thresholds and exception provenance are unchanged. Clipped/covered controls still fail. Settings density choices use Fluent's existing vertical orientation on Phone/Tablet: the complete native-zoom capture had revealed one-character wrapping, and all three labels now remain readable on one line with combined spacing. Desktop/CompactDesktop retain horizontal orientation.

The focus obstruction inventory compares a pseudo-element's own stacking layer, preserving high-z paint escaping a non-stacking owner and paint trapped by a real owner. Hint matching is exact and case-normalized, retains standard backdrop-filter and only browser-supported prefixed behavior, and treats unrecognized hints as unknown for pseudo ordering. Unknown paint is retained rather than suppressed. Browser hit-test oracles cover mixed-case transform, invented `mytransform`, standard/prefixed backdrop-filter, unlisted real `transform-style`, and neutral hints. The new accordion controls explicitly retain/refocus the same native heading before every clipped/covered/restored measurement.

| Verification on the final candidate | Result |
| --- | --- |
| Debug Shell.Tests build, including Counter.Web | Passed; 0 warnings / 0 errors |
| FrontComposerShellTests and FcSettingsDialogTests | 68 passed / 0 failed / 0 skipped/errors |
| Affected R5/R6/R7 geometry/paint negatives and new pseudo/modal/Settings controls, all three engines | 138 passed / 0 failed / 0 skipped / 0 flaky; zero retries |
| Automated actual 400% browser zoom, headed Chromium 153.0.8010.12 | 14 passed / 0 failed; five page surfaces and palette/Settings, each default/spaced |
| TypeScript; story-artifact validator; diff check | Passed |
| Owned Test runtime cleanup | Exact-target stop succeeded; describe reports no running AppHost |

Native zoom uses `chrome.tabs.setZoom(4)` in automatic per-tab mode, with Playwright viewport emulation disabled. Every case verifies 1280×900 CSS pixels/DPR 1 at 100%, then 320×225/DPR 4, unchanged outer width 1280, CSS zoom 1, visual viewport scale 1, and `tabs.getZoom` approximately 4. Page cases check text clipping, bounded horizontal scrolling, target inventory, keyboard focus and blocking WCAG 2.2 axe findings; overlays check bounded scrolling, targets and forward/reverse keyboard focus. Settings additionally checks each density label has one text line. Complete viewport captures use CDP `Page.captureScreenshot` and are 1280×900 pixels; the original red probe's partial Playwright captures are not whole-viewport evidence. This is automated Chromium evidence, not a manual zoom log or evidence of other browsers' native zoom.

The final 138-case selection includes the existing affected negative controls, and the final native 14-case run uses the same final helper. Broader checks below were completed earlier in this continuation and are deliberately attributed separately:

| Earlier observation | Source and result |
| --- | --- |
| Full responsive spec after shell/grid/helper corrections, before later hint-boundary and Settings-orientation edits | Source/test patch `sha256:769fab6408e7445c1d4045add11cff367d16b31028ac43d491722596d8b98cc0`: 638 passed / 0 failed / 1 Firefox capability skip / 0 flaky, 639 total; all three engines, zero retries |
| Six ordinary Chromium specimen visual baselines after shell/grid correction | 6 passed; no image update. Before the Settings-only orientation and final conservative hint guard; does not clear Firefox/WebKit visual failures |
| Readable Settings controls after orientation correction | 6 passed across three engines; final 138-case/native runs subsequently include these controls |
| Initial complete three-spec gate on unchanged root `80782afc…` | 964 passed / 13 failed / 4 skipped / 0 flaky, 981 total; zero retries. Chromium 327/0/0, Firefox 318/7/2, WebKit 319/6/2 (passed/failed/skipped) |
| Initial state evidence and artifact validation on that root | 116 state files per engine; Chromium validator exit 0, Firefox/WebKit exit 1 for failed outcomes. All six raw/HTML roots below unchanged 52,428,800-byte limit |
| Initial configured Shell lane on that root | 3,775 passed / 2 failed / 0 skipped/errors; 3,777 total |
| Initial configured SourceTools lane on that root | 1,408 passed / 3 failed / 0 skipped/errors; 1,411 total |
| Initial scanner / Node / TypeScript | 9/9 across three engines; 29/29 on Linux; passed |
| Initial serialized Debug AppHost, SourceTools.Tests and Shell.Tests builds | Passed; zero warnings/errors |

The complete browser gate's failures remain open: six Firefox and six WebKit visual comparisons, plus Firefox's spaced 320px module-navigation/tab keyboard geometry failure (theme-button focus envelope left −2px). An unchanged isolated one-worker repeat passed 3/3, and an instrumented repeat passed 3/3; neither attributes or supersedes that canonical full-gate failure. The current affected responsive pass does not replace a new complete three-spec gate. The ordinary Chromium images passed again after the global grid height change, without updating approved images.

Configured Shell failures are `ToolchainPins_MatchApprovedDotnetAndAspireVersions` (approved AppHost SDK 13.6.1 versus declared 13.6.3) and `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` (pinned Builds `1783c0a377615063f3222c1d28f3ab513af4e72f`: FsCheck.Xunit.v3 3.5.0 versus 3.6.0). SourceTools' two `QueryRequestDeprecationTests.NetStandardConsumer_*` cases require the missing Release/netstandard2.0 Contracts DLL; `PackagedAnalyzer_ContractsOnlyPayload_GeneratedShellConsumerCompiles` fails NU5026 for missing Release/net10.0 Contracts DLL. These remain prerequisite/governance failures; no Release output was manufactured to turn the Debug lane green. The configured runs include passing CommandFormEmitter 68, CommandRendererEmitter 35, RazorEmitter 18, ZeroField 21, CommandRendererWrapper 77, hidden-banner 39 and analyzer-governance 5 cases. They predate the bounded layout patch; only the affected layout classes were rerun afterward.

Initial normal `aspire start` exited 2 with MSB4018 `GetAssemblyAttributes`, reporting a Shell `obj` assembly in use by another process. Exact-target stop and the serialized AppHost build succeeded. The explicitly selected existing Test profile then started and `aspire wait counter-web --status healthy` passed. Every post-production-change rebuild was performed after stopping that owned runtime; the rebuilt Counter endpoint was rediscovered through Aspire and was `https://localhost:7201`. Test-only specimen options were verified, including the 30,000ms reconnect notice. This does not establish live-backend acceptance. The earlier startup dependency repair remains separately documented; this run does not retain its obsolete pre-repair startup errors as the current result.

Existing hosted runs were inspected, not triggered. At exact base `80782afc…`, [CI 40179812149](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179812149) completed failure at dependency-governance, and [Quality 40179811928](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40179811928) completed failure. These are base-commit observations, not CI validation of the uncommitted correction.

Exact build, configured-lane and final-check commands from the repository root:

```bash
aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -c Debug -m:1
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-current-mmlpok_m/shell-configured.xml
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -trait- Category=GovernanceBuild -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -longRunning 30 -result-xml /tmp/fc-story13-6-current-mmlpok_m/sourcetools-configured.xml
node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
npm --prefix tests/e2e run typecheck
# Final production build and focused layout run:
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerShellTests' -class '*FcSettingsDialogTests' -parallel none -result-xml /tmp/fc-story13-6-current-mmlpok_m/density-label-layout.xml
ASPIRE_ENVIRONMENT=Test aspire start --no-build --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json -- --environment Test
aspire wait counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --status healthy --timeout 30 --non-interactive
xvfb-run -a -s '-screen 0 1600x1200x24' node /tmp/fc-story13-6-native-final-ot40ny28/native-zoom.mjs https://localhost:7201
aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive
aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
python3 eng/validate-story-artifacts.py
git diff --check
```

Browser commands ran from `tests/e2e`:

```bash
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-mmlpok_m/axe-helper FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-mmlpok_m/axe-helper-html npx playwright test specs/a11y-helper.spec.ts --workers=3 --retries=0
# Complete initial engine runs and artifact validation, with distinct engine/run IDs:
python3 /tmp/fc-story13-6-current-mmlpok_m/run-browser-gates.py https://localhost:7201
BASE_URL=https://localhost:7201 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-mmlpok_m/zoom-responsive-final FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-mmlpok_m/zoom-responsive-final-html npx playwright test specs/responsive-accessibility.spec.ts --workers=3 --retries=0
BASE_URL=https://localhost:7201 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-mmlpok_m/visual-chromium-final FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-mmlpok_m/visual-chromium-final-html npx playwright test specs/specimen-accessibility.spec.ts --project=chromium --grep 'visual baseline' --workers=1 --retries=0
BASE_URL=https://localhost:7201 PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-current-mmlpok_m/fail-closed-final FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-current-mmlpok_m/fail-closed-final-html npx playwright test specs/responsive-accessibility.spec.ts --grep 'R[567] |pseudo stacking owner|pseudo owner hint|pseudo paint respects|fixed modal scroll probes|native accordion focus measures|short settings retains' --workers=3 --retries=0
```

The initial gate script runs `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --project=<engine> --workers=4 --retries=0`, then `node scripts/validate-a11y-artifacts.mjs`, for Chromium/Firefox/WebKit sequentially with `BASE_URL`, `PLAYWRIGHT_SKIP_WEBSERVER=1`, `FC_E2E_BUILD_CONFIGURATION=Debug`, per-engine raw/HTML roots, `FC_A11Y_EVIDENCE_ENGINE` and distinct `FC_A11Y_EVIDENCE_RUN_ID` values. The raw reports retain those run IDs and outcomes.

Red controls remain separate: unchanged production failed all six short-Home cases and all six Settings cases; all three engines reproduced one-character density-label wrapping before vertical orientation. Modal coordinates, lower/escaping pseudo paint, mixed-case/exact hints, backdrop-filter preservation and unlisted hints each retain their pre-fix failures and browser layout oracles. In particular, `transform-style` falsely suppressed a real cover in all three engines before the final unknown-hint guard; the final 138 cases retain that cover, preserve neutral hints and reject invented-token misclassification.

Raw logs/reports remain external scratch under `/tmp/fc-story13-6-current-mmlpok_m/`; final native results/captures are under `/tmp/fc-story13-6-native-final-ot40ny28/`. Original native red results and complete diagnostic captures remain under `/tmp/fc-story13-6-native-zoom-kg7smrug/` and `/tmp/fc-story13-6-native-diagnostic-elsiqnff/`. The earlier geometry-green but unreadable Settings capture remains historical under `/tmp/fc-story13-6-native-green-ncnp_s6_/`; readable results are separately retained under `/tmp/fc-story13-6-native-readable-8wgwmdfw/`. No committed evidence packet, manifest or runtime-identity record was added; the tests, command/count/source record and CI links above follow the 2026-10-23 evidence policy. Raw scratch does not establish owner approval.

All eighteen human boxes and seven MAN-136 rows remain unperformed. Missing human zoom/AT/heard-speech/device observations, Windows execution, actual WebKit system-palette paint, twelve Firefox/WebKit visual failures, the full-gate Firefox focus failure, catalog/governance/prerequisite failures, live-backend acceptance, final-candidate CI and named owner approvals remain open. OI-16, G-4, FLUENT-APP-1, UX, Product and Architect approval are not inferred. Z1–Z4 are implemented and verified; the original matrix/result and resumed Evidence acceptance tasks remain unchecked. All approved PNG/Verify baselines, frozen intent, dependencies, gitlinks and public product APIs are unchanged. Nothing was staged, committed or pushed.

## Current committed-source verification — 2026-10-25

**Story and sprint remain in-progress; accessibility acceptance remains blocked.** All implementation patches through Pass 8 are already committed. This continuation changes only this result and the owning spec. The starting root was `08a9a73f2fc8f8c4d5ae716d9a435760e5849a13`; an independent concurrent `/pushall` advanced it to `26d77f82bb3d721bc2b5165d1d77e6b3d6885ca8`, committing the prepared epic context and EventStore pointer. There is no `src`/`tests`/`samples`/`.github` difference between those roots, and all five raw source hashes in the [Pass 8 table](#pass-8-focused-verification--2026-10-25) match. The browser/helper outcomes below apply to that unchanged source; they establish no whole-runtime acceptance. CI run link: unavailable; no CI run was initiated.

| Fresh verification | Result |
| --- | --- |
| R5/R6/R7 responsive helper fixtures, all three engines | 90 passed / 0 failed / 0 skipped / 0 flaky; zero retries |
| WCAG scanner and impact-policy helper, all three engines | 9 passed / 0 failed / 0 skipped / 0 flaky; zero retries |
| Node evidence suite, Linux | 29 passed / 0 failed / 0 skipped |
| TypeScript | Passed |
| Debug SourceTools.Tests and Shell.Tests builds | Both passed; zero warnings/errors |
| CommandFormEmitterTests | 68 passed / 0 failed / 0 skipped/errors |
| ZeroFieldFailureAccessibilityTests and CommandRendererWrapperIntegrationTests | 98 passed / 0 failed / 0 skipped/errors |
| Story-artifact validator and diff check | Passed |

The 90-case selection retains positive and negative controls for containing blocks, escaped paint, hidden ancestors, competing glyph paint, focus and clipping; the scanner cases prove the default `wcag22aa` target-size rule remains active. These are focused automated checks. They do not rerun every changed shell, Home, navigation, tab, toolbar, projection/detail, form, lifecycle, palette, dialog and abandonment journey. AE-01/03–09 retain the dated integrated evidence and its limitations below; AE-02 still requires actual browser zoom. The full Counter-host/browser, configured .NET, visual and artifact gates were not rerun. Prior failed/interrupted outcomes retain their original attribution and remain unresolved.

Exact fresh commands from the repository root:

```bash
node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
npm --prefix tests/e2e run typecheck
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*CommandFormEmitterTests' -parallel none
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*ZeroFieldFailureAccessibilityTests' -class '*CommandRendererWrapperIntegrationTests' -parallel none
python3 eng/validate-story-artifacts.py
git diff --check
```

Browser commands ran from `tests/e2e`:

```bash
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-reconcile-waAjmM/browser FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-reconcile-waAjmM/browser-html npx playwright test specs/responsive-accessibility.spec.ts --grep 'R[567] ' --workers=3 --retries=0
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-reconcile-waAjmM/axe-helper FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-reconcile-waAjmM/axe-helper-html npx playwright test specs/a11y-helper.spec.ts --workers=3 --retries=0
```

Raw browser reports/logs and focused build/test logs remain local scratch under `/tmp/fc-story13-6-reconcile-waAjmM/`. No new evidence packet, manifest, replay lane or runtime-identity record is created. The owner evidence policy below governs historical links.

The required baseline command `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited **2** before startup: two NETSDK1004 errors for missing Commons.UniqueIds `project.assets.json`, Parties CS1061 for unavailable `RecoverNonExpiringPendingAsync`, and one Shell MSB3026 copy warning. The log is retained by Aspire as `cli_20261025T062814_9eeba522.log`. Exact-target `aspire stop` and `aspire describe --format Json`, both with the same `--apphost` and `--non-interactive`, report no running AppHost. The focused builds do not resolve that whole-runtime blocker.

All eighteen human boxes and seven MAN-136 rows remain unperformed. Actual AT/heard speech, actual 400% zoom, physical devices, Windows, WebKit system-palette paint, visual/catalog/live-backend and named owner approvals remain open. R8-B1's WebKit pointer-activation fixture remains deferred; R7-A1's supplementary sweep classification remains the engineering disposition recorded below. No UX, Product, Architect, OI-16, G-4 or FLUENT-APP-1 approval is inferred. The original matrix/result and resumed Evidence tasks remain unchecked. No production source, tests, approved images, dependency declarations, gitlinks or frozen requirements were changed by this continuation; nothing was staged, committed or pushed by it.

## Pass 8 focused verification — 2026-10-25

**Story and sprint remain in-progress; final accessibility classification is blocked.** The local candidate is root `c7f1666508e6582cfe168b12efb4a71638abc328` plus source/test patch `sha256:7f76211b44721767233f37fb0dadc869cd8aa88d14642b35f7f090f558de2871`, from `git diff --binary c7f1666508e6582cfe168b12efb4a71638abc328 -- src tests | sha256sum`. All eight Pass 8 patches are applied. This record supersedes earlier candidate attribution only for the focused checks below; earlier complete matrix, visual and .NET observations retain their dated source identities. No current whole-surface or hosted CI pass is inferred; CI run link: not available, no CI run initiated.

| Current focused lane | Result |
| --- | --- |
| New browser regressions against unchanged helper | 5 failed / 1 passed across three engines; Chromium/Firefox miss computed offset-path, all engines miss mixed-case will-change; WebKit already catches offset-path via computed transform |
| R5/R6/R7 related fixtures after correction | 72 passed / 0 failed / 0 skipped across Chromium, Firefox and WebKit; zero retries |
| Spaced temporary directory with original CLI regexes | 27 passed / 2 failed at the two targeted path assertions |
| Node evidence suite after correction | 29 passed / 0 failed / 0 skipped on Linux |
| TypeScript | Passed |
| Debug SourceTools.Tests / Shell.Tests builds | Both passed; zero warnings/errors |
| CommandFormEmitterTests / ZeroFieldFailureAccessibilityTests | 68 passed / 21 passed; zero failures/skips/errors |
| Repository story-artifact / diff checks | Passed |

The helper now recognizes a computed non-`none` `offset-path` and normalizes `will-change` tokens before exact matching. Filtered backdrop paint still fails closed before containment can be observed; the dead computed backdrop-filter predicate is removed. Both prefixed fixture loops require the browser layout oracle to be WebKit-only. The CLI assertions match the escaped exact report path and all their fixtures exercise a directory containing spaces. The emitter assertion names a missing method anchor before any dependent search or slice. The scope-unavailable assertion explicitly pins the current hidden-form target, with R8-B1's visible-trigger/WebKit pointer behavior still deferred.

**Retained sweep disposition (R7-A1/R7-A2):** the transient 700ms reconciliation marker decorates a refreshed grid; its refreshed values carry the content. It can appear after a partial reconnect without the Reconnected notice, so the supplementary classification does not depend on that notice. The historical 35% brand reduced-motion outline is below 3:1 (about 1.6:1 on white); this engineering classification makes no essential non-text contrast or UX approval claim. UX may authorize a ≥3:1 Fluent stroke token as a Story 5.4 follow-up. The test now describes the grid host accurately.

Exact successful commands, from the repository root unless a working directory is stated:

```bash
npm --prefix tests/e2e run typecheck
node --test tests/e2e/scripts/a11y-state-evidence.test.mjs
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class '*CommandFormEmitterTests' -parallel none
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*ZeroFieldFailureAccessibilityTests' -parallel none
python3 eng/validate-story-artifacts.py
git diff --check
```

From `tests/e2e`, the final browser command was:

```bash
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-pass8-kiS6zN/browser-green FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-pass8-kiS6zN/browser-green-html npx playwright test specs/responsive-accessibility.spec.ts --grep 'R[567] (containing block|filtered paint|escaped|display contents|a nested|exterior|simple contrast)' --workers=3 --retries=0
```

The executed red browser command used the same working directory and flags, `--grep 'R6 containing block (offset-path:ray|will-change:TrAnSfOrM retains)'`, and separate `browser-red-installed` / `browser-red-installed-html` roots. Its final report records five expected failures and WebKit's passing offset-path case. An earlier nine-case attempt under `browser-red` failed before assertions because browser executables were absent; it supplies no regression evidence. `npx playwright install chromium firefox webkit` then installed the normal cached binaries selected by existing Playwright 1.63.0 without dependency-file changes. Node's spaced-path red control used its normal command before the two regex corrections.

Raw logs/reports are local scratch under `/tmp/fc-story13-6-pass8-kiS6zN/`, outside the checkout, with passing and failed attempts retained separately. They are not durable immutable accepted proof. Current raw-byte source identities:

| Input | SHA-256 |
| --- | --- |
| `tests/e2e/helpers/responsive-accessibility.ts` | `06b141a9d8a25ca3c3d6b10df47ec101d5bf3a9c848de72c61a3441464f238ec` |
| `tests/e2e/specs/responsive-accessibility.spec.ts` | `7239f927ad101ea1ad7e09efe963545a18f0e141eae904af9339ec92aa078e34` |
| `tests/e2e/scripts/a11y-state-evidence.test.mjs` | `38acd888b9a89a4f22ab9c8cb7a90c2cd54fc34b75fbbd21220369ac79537b06` |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` | `f2ae3c8c571f545e16f8ce51489e44f8c67dd6919025a727aa4239ed10b2e82d` |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs` | `477e9506cc86c6e8a0f9f16ec59e04a83bd9edf29cdc25243614eefd15d28336` |

The pre-edit runtime command `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited 2 before startup. Its build reported two NETSDK1004 missing Commons.UniqueIds assets errors, Parties CS1061 for unavailable `RecoverNonExpiringPendingAsync`, and two MSB3026 copy warnings. Exact-target describe reported no running AppHost, and exact-target stop was issued. Successful focused Debug builds do not establish a healthy whole-runtime baseline.

Full Counter-host/browser, configured Shell/SourceTools, visual and artifact gates were not rerun on this patch. Every historical failed/interrupted packet retains its failure attribution. All eighteen human checklist boxes below remain open; actual AT/heard speech, 400% browser zoom, physical tablet/phone, Windows and actual WebKit forced-palette paint remain unperformed. OI-16, G-4, FLUENT-APP-1, visual/catalog/live-backend and Product/Architect approval remain unresolved. The original matrix/result and Evidence tasks stay unchecked. No approved image bytes, frozen requirements, dependency declarations, gitlinks or public product APIs changed; nothing was staged, committed or pushed.


## Current committed-source reconciliation — 2026-10-23

**Story and sprint remain in-progress; accessibility acceptance is incomplete.** The final observed root is `637ecb8ba7cf9e21831ee3f3bb32eb2cceaf9990`, retaining the frontend source from initial root `6f6eb8982ca1e97e28586de1d4ffc540467058af`. The [current reconciliation receipt](tests/story-13-6-committed-reconciliation-2026-10-23.json) verifies all fourteen Pass 6 source inputs and twenty-four approved PNGs against the [prior bounded receipt](tests/story-13-6-pass6-2026-10-23.json). The frozen intent also matches the pre-Pass-6 version after Git line-ending normalization. The prior receipt and every dated section below retain their historical attribution.

Reused local evidence remains **941 browser passes / 12 Firefox-WebKit visual failures / 4 capability skips**, **3,760 Shell passes / 9 dependency-governance failures**, and **1,415 SourceTools passes**. Five assembly hashes, twenty-seven focused artifact hashes, eleven broad command-log hashes and three browser-report hashes still match their recorded bytes. These checks establish availability and byte identity of the previous local observations; the broad lanes were not rerun and no durable immutable accepted-proof retrieval is established.

New execution in this reconciliation: the Node evidence suite passes **29/29**, TypeScript checking passes, and the repository artifact/diff checks pass. Exact commands, exits and bounded sanitized excerpts are in the current receipt. This task changes only the result, spec and reconciliation receipt; production source, tests, approved images and dependency declarations are preserved.

The dependencies changed concurrently, independently of this work: EventStore and Memories first drifted from their tracked gitlinks, then an external `/pushall` advanced the root to `637ecb8ba7cf9e21831ee3f3bb32eb2cceaf9990` and committed updated EventStore, Memories and Parties gitlinks. The receipt lists all three before/after identities and final checked-out revisions; every final checkout matches its tracked gitlink. This task preserves those external changes. Prior browser/.NET evidence does not revalidate the final whole-runtime identity.

All eighteen manual checklist boxes remain open: no human observation log was supplied. Actual screen-reader/heard-speech, 400% browser zoom, physical devices, Windows and WebKit system-palette observations remain unperformed. Visual, catalog, owner, live-backend and durable-proof gates remain unresolved. The matrix, result and Evidence tasks remain unchecked, and Build cannot advance beyond Tasks & Acceptance.

## Current Pass 6 continuation — 2026-10-23

All fifteen pending Pass 6 patches are implemented. This local candidate is root `4a4051c2511e5c4b7c0285fbc129c72146c1c275` plus unstaged source/test patch `sha256:e78f81ed6d708f39aa123a4860ae07b0e77164247a8e2ed9ba4404520666183f`; the fourteen raw input hashes include the new TypeScript declaration. The [bounded verification receipt](tests/story-13-6-pass6-2026-10-23.json) records exact commands, outcomes, source/assembly/report hashes and external scratch paths. No durable immutable accepted-proof packet is claimed.

| Verification | Current result |
| --- | --- |
| Focused browser regressions | 57/57 across Chromium, Firefox and WebKit |
| Helper negative controls | All twelve deliberate mutations detected; 60 expected failures, zero surviving mutants |
| Scope-disposal / validator controls | Removing the production guard and using the baseline nested-report validator both fail their regressions |
| Affected Shell / generator / Node | 136/136; 103/103; 29/29; TypeScript passes |
| Configured SourceTools lane | 1,415 passed; zero failed/skipped/errors |
| Configured Shell lane | 3,760 passed / 9 failed / 0 skipped; Fluent chrome and analyzer seal pass |
| Complete per-engine browser selection | 941 passed / 12 failed / 4 skipped / 0 flaky; no retries |
| Required state files | Chromium 116, Firefox 116, WebKit 116 of 116 each; validation details in receipt |
| Artifact-size policy | All six roots below 52,428,800 bytes |
| Full artifact validator exits | Chromium 0; Firefox 1; WebKit 1 |

The Shell failures are six MCP 2.3.0 assembly-load cases and three governance checks: the approved AppHost SDK expectation, centralized catalog policy and story-validator workflow wiring. Their source/configuration is unchanged by this patch. The Debug Shell build also records three MSB3277 dependency-conflict warnings; the SourceTools build has zero warnings/errors. Broad failures are retained separately from passing focused checks.

The first combined browser command exceeded its 600-second wrapper budget after 940 passing, ten failing and four skipped log entries, leaving three cases unreported and no final outcome report. Its owned leftover Test host was stopped. The subsequent three complete per-engine runs provide final reports and separate raw/HTML roots; the interrupted attempt remains failed history. The Debug Test host is configured with the existing Test-only timings and Fluent Components/Icons 5.0.1. This supplies no live-backend acceptance.

All twenty-four approved PNGs, frozen intent, dependency configuration, root gitlinks and historical packets remain unchanged. Only two generator text approvals changed, each by the disposed guard. All eighteen manual boxes remain open. Actual AT/heard speech, browser zoom, physical devices, Windows, WebKit system-palette coverage and named owner approvals are unperformed or unresolved. Story and sprint status remain **in-progress**; Build does not claim acceptance or completion. Nothing is staged, committed or pushed. Earlier dated sections below remain historical evidence.

Final repository checks: `python3 eng/validate-story-artifacts.py` and `git diff --check` pass. Recorded log hashes, source and frozen intent match; all eighteen manual boxes remain unchecked. Exact-target Aspire describe reports no running AppHost, and the owned Test-host port is closed.

## Current reviewed verification — 2026-10-18

**Story 13.6 remains in review; accessibility classification is blocked.** The final candidate is root `1f8c7b2b4424b6232121a7cc567f7129e46577d3` plus source/test/configuration patch `sha256:b6b5b7a761b4927d21ee0fe9ba5daf78ce31ea87928f28b3e2a3b842e8fe5fc3`. Its [candidate](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/candidate.json) binds seven source/config inputs, 37 unchanged host assemblies, eight unchanged submodules and 26 unchanged approved PNG/text baselines.

The palette focus restoration and menu measurement corrections remain. Review also makes circular native-plane coverage fail closed for effective CSS zoom and nonround corners, protects retained runner records, persists incomplete lane metadata, binds JSON/JUnit paths, and verifies runtime inputs and unique candidate/engine/run/report identities. [Individual review dispositions](spec-13-6-make-browser-accessibility-verification-repeatable.md) record every finding; two unverified dialog-timing hypotheses are deferred separately.

| Final engine | Complete outcome | Results / HTML bytes | Full artifact validator |
|---|---|---|---|
| Chromium | 252 passed / 6 visual failed | 33,343,714 / 28,418,229 | Failed on six visual outcomes |
| Firefox | 250 passed / 6 visual failed / 2 capability skips | 41,588,245 / 36,146,414 | Failed on six visual outcomes |
| WebKit | 250 passed / 6 visual failed / 2 capability skips | 34,263,532 / 29,970,027 | Failed on six visual outcomes |

The aggregate is **774 total / 752 passed / 18 visual failures / 4 capability skips / 0 flaky / 0 runner errors**. All executed nonvisual checks pass, including both originally failing journeys. Responsive/helper cases are 482 passed with one explicit Firefox bevel-capability skip; the state matrix is 224 passed with one existing capability skip. All six final roots meet the unchanged 52,428,800-byte limit. Full artifact gates remain failed on visual outcomes. [Exact commands and outcomes](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/browser-runs.json) and [verification](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/verification.json) preserve that distinction.

The [complete portable capture](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/README.md) retains all raw reports, captures, expected images and HTML assets. [Independent Git-visible-only replay](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/git-visible-copy-proof.json) passes integrity for 2,079 files, 709 JSON paths and 709 JUnit markers, with three validator/runtime hashes and all engine/run identities checked. Raw validator exit 1 and the original eighteen visual failures remain unchanged; portability supplies no accessibility approval. Earlier packets and every failed/interrupted attempt remain retained.

Final TypeScript checking and 24 evidence regressions pass; [25 tooling regressions and 14 focused browser passes](evidence/story-13-6/2026-10-18-repeatable-browser/review-fixes/README.md) cover review corrections, with one explicit capability skip. Debug build and configured Shell evidence are reused from the preceding run because C# source and all 37 host assemblies match: build has zero warnings/errors, the lane has 3,699 passes and four existing snapshot/catalog failures, and the required Fluent chrome guard passes. No new full .NET execution after the helper/tool-only review is claimed.

The [owned Aspire topology is stopped](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/aspire-stopped.txt). Manual AT/heard speech, actual 400% browser zoom, physical tablet/phone, Windows, actual WebKit forced-palette paint, visual/catalog/owner approvals and live-backend acceptance remain open. The 18 manual boxes below are unchanged. Nothing was staged, committed or pushed.

## Prior repeatable browser verification — 2026-10-18

**Story 13.6 remains in review; release accessibility classification is blocked.** The tested candidate is root `1f8c7b2b4424b6232121a7cc567f7129e46577d3` plus application/test/configuration patch `sha256:1728a6ab1753d21e18dd95d171f9e275ccb07852f4eba6f410d04af7f4a06cfe`. The [candidate manifest](evidence/story-13-6/2026-10-18-repeatable-browser/candidate.json) binds source, 37 Debug host assemblies, eight unchanged root submodules and 26 unchanged approved PNG/text baselines. The [.NET assembly manifest](evidence/story-13-6/2026-10-18-repeatable-browser/dotnet-assemblies.json) also confirms the browser and test lanes used identical Shell assembly bytes. Dependencies, thresholds, capture policy and approvals are unchanged.

Both demonstrated browser defects are corrected. The palette installs its return guard before entry focus; its two owned shell dialogs keep Fluent's redundant return slot disabled even when delayed opening callbacks assign it again. Dismissal notifications are handled once per opening, preserving the operator's next Tab. The focus sampler checks actual circular corner coverage instead of removing a radius-wide strip from every opaque native-menu edge. Transparent surfaces, outside/corner rings and competing native planes remain fail-closed. The actual palette delayed-entry/native-return journey fails on all three engines before correction, and the final causal batch passes 18/18, including Settings without added test-only containment registration. [Causal history and commands](evidence/story-13-6/2026-10-18-repeatable-browser/verification.json) retain every failed intermediate attempt.

All three accessibility specs ran independently per engine, with four workers, no retries and unique run IDs bound to that one frozen candidate:

| Engine | Complete suite outcome | Original results / HTML file bytes | Full artifact validator |
| --- | --- | --- | --- |
| Chromium | 255 total: 249 passed / 6 visual failed / 0 skipped | 33,234,618 / 28,329,422 | Failed: six unsuccessful visual outcomes |
| Firefox | 255 total: 248 passed / 6 visual failed / 1 capability skip | 43,944,218 / 38,509,133 | Failed: six unsuccessful visual outcomes |
| WebKit | 255 total: 247 passed / 6 visual failed / 2 capability skips | 35,832,621 / 31,548,420 | Failed: six unsuccessful visual outcomes |

The aggregate is **765 total: 744 passed / 18 visual failed / 3 skipped / 0 flaky / 0 runner errors**. Every executed nonvisual case passes. Each final results/HTML root meets the unchanged 52,428,800-byte limit. The full artifact validators remain failed on the eighteen visual outcome IDs; they report no additional size or state-evidence errors. These results supply no visual or catalog approval. [Exact commands, all outcomes and run IDs](evidence/story-13-6/2026-10-18-repeatable-browser/browser-runs.json), [Chromium validator](evidence/story-13-6/2026-10-18-repeatable-browser/chromium-artifacts.log.txt), [Firefox validator](evidence/story-13-6/2026-10-18-repeatable-browser/firefox-artifacts.log.txt), [WebKit validator](evidence/story-13-6/2026-10-18-repeatable-browser/webkit-artifacts.log.txt).

The [portable packet instructions](evidence/story-13-6/2026-10-18-repeatable-browser/README.md) provide complete raw JSON/JUnit reports, captures and HTML assets in lossless archives, together with a hash-bound relocation map. The [relocation audit](evidence/story-13-6/2026-10-18-repeatable-browser/relocation-audit.json) passes structural integrity and attachment ownership for all 116 required state rows per engine, verifies every retained attachment, and checks path-only JSON/JUnit replay with unchanged outcomes. The underlying replay/full validators still exit 1 with exactly the original visual failures. Successful portability is not an overall accessibility or artifact-gate pass. Normal tar hard links deduplicate identical archive members; every restored path and byte is retained, and logical root sizes stay unchanged. All 29 support logs are retained as byte-identical, hash-bound nonignored `.log.txt` files. A [Git-visible-only copy replay](evidence/story-13-6/2026-10-18-repeatable-browser/git-visible-copy-proof.json), with independent locked dependencies and zero original checkout support files, also passes; its [output](evidence/story-13-6/2026-10-18-repeatable-browser/git-visible-copy-replay.json) preserves the same failed gate outcomes.

All raw failed/intermediate captures remain available. An initial Chromium full-run attempt was interrupted before report generation while the palette correction was incomplete. Its 4,358 raw files occupy 234,234,008 results bytes, exceed the same limit, and have no JSON/JUnit/HTML completion report. That attempt remains explicitly **incomplete and failed history**, separate from the final three-engine proof. The historical packets below are untouched.

The Debug Shell.Tests build passed with zero warnings/errors. Its configured lane with all five exclusions executed 3,703 tests: 3,699 passed and the same four existing failures (two generated-grid text snapshots, centralized catalog and Fluent-version approval). The required `Shell_chrome_styles_never_use_accent_as_surface_background` guard also passed independently. Final TypeScript checking and the existing 24 Node evidence regressions passed. [Build](evidence/story-13-6/2026-10-18-repeatable-browser/shell-build.log.txt), [configured .NET lane](evidence/story-13-6/2026-10-18-repeatable-browser/shell-full.log.txt), [required Fluent guard](evidence/story-13-6/2026-10-18-repeatable-browser/fluent-chrome-final.log.txt), [verification manifest](evidence/story-13-6/2026-10-18-repeatable-browser/verification.json).

Aspire started and was inspected before code edits; Counter ran Healthy in Test and was restarted after the Debug build. Final [sanitized resource state](evidence/story-13-6/2026-10-18-repeatable-browser/aspire-final-resources-sanitized.json) records EventStore as Finished; these synthetic Counter journeys provide no live-backend acceptance. The owned topology is [stopped](evidence/story-13-6/2026-10-18-repeatable-browser/aspire-stop.log.txt), and [inspection confirms no AppHost running](evidence/story-13-6/2026-10-18-repeatable-browser/aspire-stopped.log.txt). Manual AT/heard speech, actual 400% browser zoom, physical tablet/phone, Windows execution, actual WebKit forced-palette paint, visual/catalog decisions and owner approvals remain open in the unchanged checklist below. Nothing was staged, committed or pushed.

## Prior resumed Build review — 2026-10-18

**Story 13.6 remains in review; release accessibility classification is blocked.** This candidate is root `1d2cc12eb467db048c41d1aee9c056f1e29e5fc1` plus source/test patch `sha256:3aa1747eb36b3f774c4bf71a81864f877ed9eb4addd77d7614edb600a262d837`. The [source manifest and exact patch](evidence/story-13-6/2026-10-18-build-resume/source.json) bind all five changed source/test files, eight unchanged root submodules and 26 unchanged approved PNG/text baselines. No dependency declaration or approval changed.

The three independent Build review layers returned ten blind findings, two edge findings and no verification gaps. Every finding has an individual verdict in the main story spec. Four private corrections are applied: simple text contrast rejects opacity across composed ancestors; fixed pseudo focus checks include the transformed target itself; unrelated overlapping text cannot certify nominal warning contrast; and missing-detail completion revalidates settlement, result identity, scope, surface and group ownership, including handled stale interop failure. Existing successful focus/speech behavior stays covered. One unverified detached-observer retention concern was appended to the deferred ledger; exact prior findings retain their existing dispositions.

| Current lane | Executed result | Retained proof |
| --- | --- | --- |
| New helper counterexamples and valid positives, three engines | Before fix: 9 failed. After fix: 9 passed; zero retries | [Red log](evidence/story-13-6/2026-10-18-build-resume/browser-red.log), [green log](evidence/story-13-6/2026-10-18-build-resume/browser-green.log), [red source](evidence/story-13-6/2026-10-18-build-resume/red-source.json) |
| Missing-detail delayed completion | Two original awaited lifecycle regressions fail on baseline production source. Final banner class: 39 passed, including four completion/exception cases | [Red log](evidence/story-13-6/2026-10-18-build-resume/recovery-red-final.log), [red source](evidence/story-13-6/2026-10-18-build-resume/recovery-red-source.json), [39 passing tests](evidence/story-13-6/2026-10-18-build-resume/banner-green.log) |
| Debug Shell.Tests build, with Counter.Web project reference | Passed; 0 warnings / 0 errors | [Build](evidence/story-13-6/2026-10-18-build-resume/shell-build-final.log) |
| TypeScript | Passed | [Exact command/result](evidence/story-13-6/2026-10-18-build-resume/verification.json) |
| Configured Debug Shell lane | 3,703 total: 3,699 passed / 4 failed / 0 skipped / 0 errors | [Log](evidence/story-13-6/2026-10-18-build-resume/shell-full.log); includes the required Shell chrome Fluent guard |
| Complete three-spec browser gate, three engines, 4 workers, no retries | 759 total: 736 passed / 20 failed / 3 skipped / 0 flaky; no runner errors | [Console](evidence/story-13-6/2026-10-18-build-resume/browser-full.log), [raw JSON](evidence/story-13-6/2026-10-18-build-resume/browser-full.playwright.json), [JUnit](evidence/story-13-6/2026-10-18-build-resume/browser-full.junit.xml), [all outcomes](evidence/story-13-6/2026-10-18-build-resume/browser-outcomes.json) |
| Browser breakdown | Responsive/helper: 468 passed. State matrix: 222 passed / 2 failed / 1 skip. Specimen: 46 passed / 18 visual failed / 2 skips | [Per-file outcomes](evidence/story-13-6/2026-10-18-build-resume/browser-outcomes.json) |
| Complete artifact validator | Failed under unchanged 52,428,800-byte limit: results 120,809,278 / HTML 102,705,183 file bytes | [Validator log](evidence/story-13-6/2026-10-18-build-resume/artifact-validation.log) |

The four .NET failures are the two existing generated-grid text snapshots plus centralized-catalog and Fluent-version assertions. SourceTools was not rerun because its producer source is unchanged; its prior Fluent-version approval failure remains open. All 18 browser visual comparisons fail, and their approved image bytes remain unchanged. This run supplies no visual or catalog approval.

The two nonvisual browser failures remain **open**. Chromium's light palette-to-settings journey fails its expected Tab focus destination; an isolated one-worker repeat passes, but does not replace the canonical failure. Firefox's light theme-menu journey fails on competing underlying card border/shadow paint while measuring the focused menu item; a current one-worker repeat fails again. A temporary import-relocated copy using the HEAD helper passes, so causal attribution remains unresolved rather than being called a proven pre-existing issue. [Chromium failure](evidence/story-13-6/2026-10-18-build-resume/chromium-full-failure.md), [Firefox failure](evidence/story-13-6/2026-10-18-build-resume/firefox-full-failure.md), [diagnostic commands and limitations](evidence/story-13-6/2026-10-18-build-resume/verification.json) preserve those distinctions. No complete current state matrix or nonvisual gate pass is claimed.

Aspire successfully launched this time. The launch profile initially overrode environment-only selection; the corrected command explicitly passes `-- --environment Test`. `aspire wait counter-web` reports Healthy, and sanitized [resource state](evidence/story-13-6/2026-10-18-build-resume/aspire-described.json) confirms both Counter environment names are Test. Security remains unhealthy and dependent backend resources wait, so this is no live EventStore proof. The owned topology is [stopped](evidence/story-13-6/2026-10-18-build-resume/aspire-stop.log); [final inspection](evidence/story-13-6/2026-10-18-build-resume/aspire-stopped.log) finds no AppHost running.

The bounded retained record contains exact outcome reports and regression/build logs. Full raw browser results/HTML, including attachment captures, remain at `/tmp/story-13-6-resume-full/{results,html}`; copied reports still reference those local attachments. This is **not** a portable or complete artifact-validation packet. Historical packets remain untouched. At the start of this review, the unchanged Node evidence suite passed 24/24 and historical state replay returned zero errors for 116 rows per engine; those checks do not certify new current browser paint. WebKit actual forced-palette coverage, Windows execution, all manual AT/heard speech, actual 400% zoom, physical devices, live-backend acceptance and owner approvals remain unperformed/open. No staging, commit or push occurred.


## Prior scoped PNG-integrity validation — 2026-10-18

**Story 13.6 remains in review.** This correction uses root `4ee1009c947886a0437b3b93b8cabd433920b8de` plus source/test patch `sha256:f36c6f62d0a2c753deb8ea3db6faa398dd50f980382b348ddd4f32da39ba6fcb`. The [source manifest](evidence/story-13-6/2026-10-18-png-integrity/source-reviewed.json) records the two changed JavaScript files, eight unchanged root submodules and 26 unchanged approved PNG/text baselines. Browser producers and host source are unchanged. This run replays the retained hash-bound browser packet through the new validator; it performs no new browser, .NET, manual AT or device execution.

Matching nonempty image bytes now require PNG chunk integrity, a complete checksummed compressed stream, exact filtered-data length and successful RGBA pixel decoding. Resource limits of 16,777,216 pixels and 33,554,432 expanded bytes apply before inflation/decoding; element screenshots retain flexible dimensions, including grayscale, indexed, 16-bit and Adam7 formats. The decoder comes from the existing pinned Playwright package. Existing final-attempt image ownership and the same-run WebKit capability exemption remain required. These checks establish image completeness, checksum integrity and pixel decodability; full PNG encoder/ancillary metadata conformance is outside scope.

| Scoped check | Result | Retained proof |
| --- | --- | --- |
| Node evidence regressions | 24 passed / 0 failed / 0 skipped; the same final regressions against the frozen baseline produce 19 passed / 5 failed | [Final log](evidence/story-13-6/2026-10-18-png-integrity/node-reviewed-final.txt), [attributable red log](evidence/story-13-6/2026-10-18-png-integrity/node-red-replay.txt), [red source identity and replay command](evidence/story-13-6/2026-10-18-png-integrity/red-source.json) |
| Existing three-engine state packet replay | All 116 required rows per engine return zero errors; Chromium/Firefox screenshots decode; WebKit remains capability-bound media-only | [Replay](evidence/story-13-6/2026-10-18-png-integrity/state-reviewed.json), [unchanged replay script](evidence/story-13-6/2026-10-18-attempt-ownership/validate-state-packet.mjs) |
| TypeScript check | Passed | [Log](evidence/story-13-6/2026-10-18-png-integrity/typecheck-reviewed.txt) |

The retained Chromium/Firefox images remain bound to their original successful attempts; replay does not convert them into a new browser run. WebKit actual forced-palette paint remains uncovered. Historical oversized packets remain failed under the unchanged 52,428,800-byte per-root limit; this scoped replay does not claim a complete artifact-validator pass. Aspire startup again exited 2 with MSB4018/file-in-use before readiness; exact-target stop/describe found no running AppHost ([raw startup log](evidence/story-13-6/2026-10-18-png-integrity/aspire-start.txt)). Visual/catalog/snapshot decisions, manual AT/heard speech, actual 400% zoom, physical devices, Windows, live EventStore and owner approvals remain open. The [bounded spec](spec-13-6-validate-state-screenshot-pngs.md) records all nine review dispositions: five patches and four rejected metadata-conformance improvements, with the scope stated above. The [reviewed verification manifest](evidence/story-13-6/2026-10-18-png-integrity/verification-reviewed.json) records exact commands and results. Nothing is staged, committed or pushed.

## Prior scoped screenshot-attempt binding — 2026-10-18

**Story 13.6 remains in review.** This bounded evidence correction uses root `ba59327358b5639fa4db87c8eee2711858c77007` plus source/test patch `sha256:07ddface94810fbceffe0c69311e855e3c0aa279146fec0cb3473c80f7e75e0d`. The [reviewed source manifest](evidence/story-13-6/2026-10-18-attempt-ownership/source-reviewed.json) records the four changed implementation/configuration files, all eight root-submodule identities, Debug host assemblies, 24 unchanged PNG approvals and both unchanged generated-grid text approvals. The browser producer and host assemblies match the [initial manifest](evidence/story-13-6/2026-10-18-attempt-ownership/source.json); review strengthened only validator diagnostics/type handling and Node regressions. Final state replay uses the reviewed validator. No dependency declaration, submodule pointer or approved baseline changed.

Each forced-palette state screenshot is now attached as a state-specific `image/png` to its owning Playwright attempt. Validation requires exactly one readable path-backed image on the final successful attempt and byte equality with that state's nonempty local screenshot. Missing, duplicate, altered, exchanged, foreign-test/engine, failed-retry, empty and inline-only image evidence cannot fill the required image. This establishes attachment ownership; PNG decoding remains open.

| Scoped lane | Executed result | Retained proof |
| --- | --- | --- |
| Node evidence regression suite | 18 passed / 0 failed / 0 skipped | [Final log](evidence/story-13-6/2026-10-18-attempt-ownership/node-reviewed.txt); three initial red cases and the intermediate correction remain retained |
| Complete state matrix, three engines, four workers, no retries | 225 cases: 224 passed / 1 existing WebKit overflow-clip-margin capability skip / 0 failed or flaky | [Console](evidence/story-13-6/2026-10-18-attempt-ownership/browser.txt), [outcomes](evidence/story-13-6/2026-10-18-attempt-ownership/browser/outcomes.json), [raw JSON](evidence/story-13-6/2026-10-18-attempt-ownership/browser/results/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-attempt-ownership/browser/results/junit.xml), [HTML](evidence/story-13-6/2026-10-18-attempt-ownership/browser/html/index.html) |
| Final WebKit matrix with its required same-run capability probe | 76 cases: 75 passed / the same existing skip / 0 failed or flaky | [Console](evidence/story-13-6/2026-10-18-attempt-ownership/browser-webkit.txt), [outcomes](evidence/story-13-6/2026-10-18-attempt-ownership/browser-webkit/outcomes.json), [raw JSON](evidence/story-13-6/2026-10-18-attempt-ownership/browser-webkit/results/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-attempt-ownership/browser-webkit/results/junit.xml), [HTML](evidence/story-13-6/2026-10-18-attempt-ownership/browser-webkit/html/index.html) |
| Existing state-evidence validator on authoritative final reports | All 116 required rows per engine return zero errors: Chromium/Firefox use the first report; WebKit uses its capability-bound final report | [Final validation](evidence/story-13-6/2026-10-18-attempt-ownership/state-final.json), [replay script](evidence/story-13-6/2026-10-18-attempt-ownership/validate-state-packet.mjs) |
| Debug Counter.Web build and TypeScript | Build: 0 warnings / 0 errors; `npm run typecheck` passed | [Build log](evidence/story-13-6/2026-10-18-attempt-ownership/build-host.txt), [exact commands/results](evidence/story-13-6/2026-10-18-attempt-ownership/verification.json) |

The first WebKit matrix-only report lacked the required capability probe and is insufficient for its fallback validation; its [448 validation errors](evidence/story-13-6/2026-10-18-attempt-ownership/state-initial.json) remain retained. The final 76-case WebKit run includes the existing responsive-spec probe in the same authoritative report and supersedes only that initial WebKit evidence. Its actual authored-color replacement is unsupported, so acceptance is explicitly media-only; actual WebKit forced-palette paint remains uncovered. Counts from the two runs are separate and not pooled into a larger unique matrix.

All four current results/HTML roots are below the unchanged 52,428,800-byte limit (38,444,036 / 19,891,527 / 11,262,125 / 6,208,664 bytes). These matrix-only packets omit specimen axe/focus artifacts, so no complete `validate:a11y-artifacts` pass is claimed. Historical broader packets remain oversized and failed. The [697-path relocation map](evidence/story-13-6/2026-10-18-attempt-ownership/attachment-path-map.json) preserves raw report bytes and lets the replay script verify report/attachment hashes after checkout relocation.

All ten scoped blind-review findings were patched; the [bounded spec](spec-13-6-bind-state-screenshots-to-playwright-attempts.md) records each verdict and proof. Aspire startup again failed with MSB4018/file-in-use before resource readiness; [diagnostics](evidence/story-13-6/2026-10-18-attempt-ownership/aspire-start.txt) retain the exact command and exit. Stop/describe reported no running AppHost. Browser checks used an owned standalone Debug Test host, now stopped, and establish no live EventStore proof. Full .NET, visual comparisons and the broad three-spec gate were not repeated for this JavaScript/TypeScript-only correction. Manual AT/heard speech, actual 400% zoom, physical devices, Windows, visual/catalog/owner approvals and live-backend acceptance remain open as documented below. No staging, commit or push is asserted.

## Prior scoped field-layout correction — 2026-10-18

**Story 13.6 remains in review.** Tested base root is `6ab8d2daac6d2ee8ef2f0ce8686cb446d0b6dd0b`, with final source/test binary diff `sha256:d193180d49fc3d5a827fecdcdd7cb7a79b3626f5c93348acb9768e26e22d2064`. The [final source manifest](evidence/story-13-6/2026-10-18-field-layout/source-followup.json) records both changed source/test files, all eight root-submodule identities, rebuilt Debug assemblies and the unchanged 24 PNGs and two generated-grid text approvals. Review added browser assertions only; production source and assemblies match the [initial correction manifest](evidence/story-13-6/2026-10-18-field-layout/source.json), whose source/test diff is `sha256:3fa66e524c59feaddb285b4f80616881d77d5e3e2017392fa5df99f95e9ec5e0`. Five submodule pointers advanced before this work, so the older full gates below remain historical for their recorded candidate. This correction changes no dependency declaration, policy, submodule pointer or approved baseline.

Generated descriptions now use FluentText's existing `Block` parameter. They remain persistent siblings with the same IDs and accessible description links when Fluent displays its single validation error. The existing destructive-command journey now checks all three descriptions below their editors at desktop width and at 320 CSS pixels with validation present. The [before-fix run](evidence/story-13-6/2026-10-18-field-layout/red.log) failed on all three inline descriptions; the corrected journey passes on Chromium, Firefox and WebKit. Viewport checks do not establish actual browser zoom or heard speech.

| Current scoped lane | Result | Retained proof |
| --- | --- | --- |
| Debug Counter.Web, SourceTools.Tests and Shell.Tests builds | Passed; each 0 warnings / 0 errors | [Host build](evidence/story-13-6/2026-10-18-field-layout/build-after.log), [SourceTools build](evidence/story-13-6/2026-10-18-field-layout/build-source-tools.log), [Shell build](evidence/story-13-6/2026-10-18-field-layout/build-shell.log) |
| CommandFormEmitterTests | 68 passed / 0 failed | [Generator log](evidence/story-13-6/2026-10-18-field-layout/generator-tests.log) |
| Full destructive-command and specimen specs; three engines; 4 workers; no retries | 90 total: 70 passed / 18 visual failed / 2 skipped / 0 flaky; no nonvisual failures | [Console](evidence/story-13-6/2026-10-18-field-layout/browser.log), [90 outcomes](evidence/story-13-6/2026-10-18-field-layout/browser/outcomes.json), [raw JSON](evidence/story-13-6/2026-10-18-field-layout/browser/results/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-field-layout/browser/results/junit.xml), [HTML](evidence/story-13-6/2026-10-18-field-layout/browser/html/index.html) |
| Type specimen 320px reflow, with/without all spacing overrides, plus real Fluent editor focus contours; three engines | 9 passed / 0 failed | [Reflow log](evidence/story-13-6/2026-10-18-field-layout/reflow.log) |
| Full SourceTools assembly | 1,313 total: 1,312 passed / 1 failed / 0 skipped | [Log](evidence/story-13-6/2026-10-18-field-layout/source-tools-tests.log); unchanged Fluent 5.0.0 approval versus declared 5.0.1 fails before packaged-consumer compilation |
| Counter generated rendering plus catalog/Fluent governance | 30 total: 26 passed / 4 failed | [Log](evidence/story-13-6/2026-10-18-field-layout/shell-affected-tests.log); the two grid-role text approvals and two catalog/Fluent policy decisions remain unresolved |
| Three previously failing analyzer governance checks after consistent declared-catalog restore | 3 passed / 0 failed; existing canonical/forced Recommended Release builds and negative controls unchanged | [Analyzer log](evidence/story-13-6/2026-10-18-field-layout/analyzer-governance-tests.log), [Release restore](evidence/story-13-6/2026-10-18-field-layout/restore-release.log) |
| Final reviewed destructive validation journey, three engines | 3 passed / 0 failed; desktop validation plus 320px validation with/without all spacing overrides, rendered text clipping and page overflow | [Follow-up log](evidence/story-13-6/2026-10-18-field-layout/review-followup.log), [JSON](evidence/story-13-6/2026-10-18-field-layout/review-followup/results/playwright.json) |

`dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false` refreshed stale assets without changing tracked declarations. All three existing `AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate`, `AnalyzerPolicy_Story1122HiddenControlNegativeProbes_RemainClean` and `AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean` methods then passed under their unchanged zero-warning/error assertions. This resolves those three retained assembly-conflict failures; it does not establish a complete Shell assembly pass. TypeScript typecheck and `git diff --check` also passed. Exact commands, failed/skip inventories and raw-report hashes are in the [current verification manifest](evidence/story-13-6/2026-10-18-field-layout/verification.json).

Chromium's approved and actual page heights now match in every theme/density combination. Remaining Chromium comparisons differ by 3,378 pixels in each light case and 6,524 in each dark case, versus roughly 48,000 and 247,000–250,000 previously; description paint/position and header differences remain. Firefox/WebKit still have broader baseline differences. The [current 18-row visual inventory](evidence/story-13-6/2026-10-18-field-layout/visual-diff-inventory.json) records every engine/theme/density's dimensions, changed pixels and actual/expected/diff paths and hashes. All 18 comparisons remain failed; no screenshot tolerance or baseline changed. The two browser skips are the pre-existing Firefox/WebKit scoped visual-hook cases. This scoped run does not replace the complete three-spec accessibility gate or a full Shell assembly run. The 90-case browser and nine-case reflow reports retain the initial test revision; the three-case review follow-up uses the final regression revision with unchanged production code.

The new browser output also exceeds the existing 50 MiB per-root limit: `browser/results` contains 78,296,904 file bytes and `browser/html` 79,731,410. Its size gate fails, independently of the preserved older packet's failure documented below. The complete state-evidence validator is not claimed to pass for these two specs, which omit the required state-matrix lane. Required manual AT/heard speech, actual 400% zoom, physical devices, Windows, WebKit actual forced-palette paint and owner approvals remain open.

The final focused follow-up is bounded at 10,112 file bytes for its results root and 543,243 for HTML; it does not replace the failed broader packet. Avoiding duplicate attachment retention in the broader HTML/results workflow is deferred, with both failures and the limit preserved. All linked logs are covered by a scoped Git ignore exception. For another checkout, the [attachment relocation map](evidence/story-13-6/2026-10-18-field-layout/attachment-path-map.json) resolves raw report paths against the relocated packet or repository root and includes each file hash; apply it only to a derived report, preserving the raw JSON. The [review triage](evidence/story-13-6/2026-10-18-field-layout/review-triage.md) records every finding and disposition.

Aspire baseline command `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` failed before resource readiness with MSB4018/file-in-use output for Shell build artifacts. `aspire describe` and `aspire stop` reported no running AppHost. The browser lanes above ran against the separately owned Debug Test host at `http://127.0.0.1:5096` using the launch command in the source manifest; they establish no live EventStore proof.

## Prior full candidate and automated gates — 2026-10-18

The following full-gate outcomes belong to the earlier candidate; current scoped results are above.

**Readiness remains open; Story 13.6 and its sprint entry stay in review.** The review-patched candidate is root `acfc74c7e90acf7f55d0a9af7137859c77a4a09f` plus the unstaged `src`/`tests`/`samples` binary diff `sha256:0c1217adfe38e3a488dba91350ff098f6eab0de227714eee99f12b456374af60`. The full browser, Shell and SourceTools gates below ran this exact source digest. Earlier full attempts remain failed historical proof for their own digests. The [source manifest](evidence/story-13-6/2026-10-18-gate-rerun/source.json) records all three changed source/test file hashes, eight exact root-submodule pointers, rebuilt Debug DLL hashes, and hashes of all 24 approved PNGs and two approved `.verified.txt` snapshots. The [verification manifest](evidence/story-13-6/2026-10-18-gate-rerun/verification.json) records commands, every failed and skipped name, raw-report hashes and artifact paths. No approved baseline, dependency declaration or submodule pointer changed in the tested candidate. A later `/pushall` may advance root submodule pointers; the automated outcomes remain bound to the hashes in this manifest and do not certify those newer pointers.

All three Debug builds passed with **0 warnings and 0 errors**: `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-restore`, and the same command for `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj`. Their raw logs are [Counter.Web](evidence/story-13-6/2026-10-18-gate-rerun/build-counter-web-gate.log), [Shell.Tests](evidence/story-13-6/2026-10-18-gate-rerun/build-shell-tests-gate.log) and [SourceTools.Tests](evidence/story-13-6/2026-10-18-gate-rerun/build-source-tools-tests-gate.log).

The current full three-spec, three-engine browser gate ran against the rebuilt Debug Test host with `PLAYWRIGHT_SKIP_WEBSERVER=1`, four workers and zero retries. The exact host launch and browser commands are recorded below and in the manifests:

```text
npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --workers=4 --retries=0
```

This exact source digest finished **750 total: 729 passed, 18 failed, 3 skipped, 0 flaky**. The [750-row outcome inventory](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/outcomes.json), [Playwright JSON](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/junit.xml), [HTML report](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/html/index.html), [console log](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/run.log) and result directory retain raw failed screenshots, diffs, video and traces. The three skips are WebKit's overflow-clip-margin capability fixture and two non-Chromium Story 11.5 scoped visual-hook cases.

Earlier attempts in this same 2026-10-18 investigation remain separate proof: the [747-case prior full gate](evidence/story-13-6/2026-10-18-gate-rerun/browser-final-2/playwright.json) had 725 passed, 19 failed and 3 skipped; the [initial full gate](evidence/story-13-6/2026-10-18-gate-rerun/browser-initial/playwright.json) had 744 total, 722 passed, 19 failed and 3 skipped; the [first post-fix full gate](evidence/story-13-6/2026-10-18-gate-rerun/browser-final/playwright.json) had 747 total, 722 passed, 22 failed and 3 skipped. The [typecheck-interrupted attempt](evidence/story-13-6/2026-10-18-gate-rerun/browser-interrupted-typecheck/) and red/green focused runs are also retained. All three preceding full browser attempts are historical for the review-patched source digest; the 750-case run above is the current full-gate outcome.

The current full browser gate has **18 visual failures**, one for each engine, theme and density combination. The [current 18-row evidence inventory](evidence/story-13-6/2026-10-18-gate-rerun/visual-diff-inventory-parent-final.json) records exact expected/actual heights, changed pixels, retained actual/diff paths and hashes. The table below classifies every failed comparison from those images. Each screenshot is 1280 pixels wide. These are observed differences, not an approved new visual design or a proven single cause.

| Engine | Theme / density | Expected → actual height | Changed pixels | Pattern | Retained proof |
| --- | --- | ---: | ---: | --- | --- |
| chromium | light / compact | 3818 → 3770 | 47,854 | C | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-chromium/frontcomposer-type-light-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-chromium/frontcomposer-type-light-compact-diff.png) |
| chromium | light / comfortable | 3878 → 3830 | 48,036 | C | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-chromium/frontcomposer-type-light-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-chromium/frontcomposer-type-light-comfortable-diff.png) |
| chromium | light / roomy | 3938 → 3890 | 48,175 | C | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-chromium/frontcomposer-type-light-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-chromium/frontcomposer-type-light-roomy-diff.png) |
| chromium | dark / compact | 3818 → 3770 | 246,526 | CD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-chromium/frontcomposer-type-dark-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-chromium/frontcomposer-type-dark-compact-diff.png) |
| chromium | dark / comfortable | 3878 → 3830 | 248,208 | CD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-chromium/frontcomposer-type-dark-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-chromium/frontcomposer-type-dark-comfortable-diff.png) |
| chromium | dark / roomy | 3938 → 3890 | 249,852 | CD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-chromium/frontcomposer-type-dark-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-chromium/frontcomposer-type-dark-roomy-diff.png) |
| firefox | light / compact | 3778 → 3772 | 187,145 | F | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-firefox/frontcomposer-type-light-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-firefox/frontcomposer-type-light-compact-diff.png) |
| firefox | light / comfortable | 3846 → 3832 | 206,564 | F | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-firefox/frontcomposer-type-light-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-firefox/frontcomposer-type-light-comfortable-diff.png) |
| firefox | light / roomy | 3914 → 3892 | 219,503 | F | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-firefox/frontcomposer-type-light-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-firefox/frontcomposer-type-light-roomy-diff.png) |
| firefox | dark / compact | 3778 → 3772 | 426,864 | FD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-firefox/frontcomposer-type-dark-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-firefox/frontcomposer-type-dark-compact-diff.png) |
| firefox | dark / comfortable | 3846 → 3832 | 444,192 | FD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-firefox/frontcomposer-type-dark-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-firefox/frontcomposer-type-dark-comfortable-diff.png) |
| firefox | dark / roomy | 3914 → 3892 | 460,850 | FD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-firefox/frontcomposer-type-dark-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-firefox/frontcomposer-type-dark-roomy-diff.png) |
| webkit | light / compact | 3818 → 3815 | 180,414 | W | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-webkit/frontcomposer-type-light-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-f59b0-sual-baseline-light-compact-webkit/frontcomposer-type-light-compact-diff.png) |
| webkit | light / comfortable | 3886 → 3875 | 199,940 | W | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-webkit/frontcomposer-type-light-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-bad1f--baseline-light-comfortable-webkit/frontcomposer-type-light-comfortable-diff.png) |
| webkit | light / roomy | 3954 → 3935 | 215,667 | W | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-webkit/frontcomposer-type-light-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-79faf-visual-baseline-light-roomy-webkit/frontcomposer-type-light-roomy-diff.png) |
| webkit | dark / compact | 3818 → 3815 | 425,276 | WD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-webkit/frontcomposer-type-dark-compact-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-2be2e-isual-baseline-dark-compact-webkit/frontcomposer-type-dark-compact-diff.png) |
| webkit | dark / comfortable | 3886 → 3875 | 438,079 | WD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-webkit/frontcomposer-type-dark-comfortable-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4df55-l-baseline-dark-comfortable-webkit/frontcomposer-type-dark-comfortable-diff.png) |
| webkit | dark / roomy | 3954 → 3935 | 450,341 | WD | [actual](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-webkit/frontcomposer-type-dark-roomy-actual.png) · [diff](evidence/story-13-6/2026-10-18-gate-rerun/browser-parent-final/results/specimen-accessibility-Fro-4cdc6--visual-baseline-dark-roomy-webkit/frontcomposer-type-dark-roomy-diff.png) |

Pattern **C**: Chromium light shows destructive-form helper text alongside its controls and a 48-pixel lift in later sections. **CD** adds extensive dark text/border paint differences (about 247,000–250,000 changed pixels). **F/FD**: Firefox has the helper displacement plus page-wide typography, control, border and vertical-layout differences from header through footer; dark differs more densely. **W/WD**: WebKit has similarly broad layout and raster differences, and its generated-grid metadata fallback row also shifts; dark differs more densely. Density increases the height delta in Firefox and WebKit. All 24 approved PNGs and both approved generated-grid `.verified.txt` files retain their original bytes. Any baseline replacement needs recorded owner approval.

The earlier Chromium dark `active toolbar, tabs, palette and settings states` Tab failure is retained in the [historical trace](evidence/story-13-6/2026-10-18-gate-rerun/browser-final-2/results/accessibility-state-matrix-b4dcc-and-settings-states-in-dark-chromium/trace.zip) and [screenshot](evidence/story-13-6/2026-10-18-gate-rerun/browser-final-2/results/accessibility-state-matrix-b4dcc-and-settings-states-in-dark-chromium/test-failed-1.png). It passed [8/8 focused repeats](evidence/story-13-6/2026-10-18-gate-rerun/browser-chromium-palette-isolation/playwright.json) and the current full run. The previously observed Firefox palette return failure also did not recur. The keyboard assertion remains intact; the intermittent cause is unresolved. The native-dialog regression passed in the current full run, along with formerly failing Firefox lifecycle/rail cases. [Focused post-fix matrix: 12/12](evidence/story-13-6/2026-10-18-gate-rerun/browser-transient-check/playwright.json).

The current full Shell assembly command `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none` finished **3709 total, 7 failed, 1 skipped, 0 errors**. The current full SourceTools command `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll` finished **1313 total, 1 failed, 0 skipped/errors**. Their [Shell raw log](evidence/story-13-6/2026-10-18-gate-rerun/shell-tests-parent-final.log) and [SourceTools raw log](evidence/story-13-6/2026-10-18-gate-rerun/source-tools-tests-parent-final.log) retain the exact failures:

| Gate | Exact failing test | Diagnosis and disposition |
| --- | --- | --- |
| Shell | `CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot` | Received generated-grid table adds `role="grid"`; approved `.verified.txt` unchanged. |
| Shell | `CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting` | Same `role="grid"` drift; approved `.verified.txt` unchanged. [Counter received snapshot](evidence/story-13-6/2026-10-18-gate-rerun/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.received.txt) and [Status received snapshot](evidence/story-13-6/2026-10-18-gate-rerun/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.received.txt) retained separately. |
| Shell | `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` | Expects Localization 10.0.12; unchanged Builds catalog has 10.0.13. Policy/catalog owner alignment remains open. |
| Shell | `FluentConformanceTests.Fluent_package_versions_are_v5_only_and_icons_match_components` | Approved 5.0.0 pin expectation versus unchanged Fluent component/icon 5.0.1 catalog. Fluent owner alignment remains open. |
| Shell | `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate` | Release zero-warning check sees 10.0.12/10.0.13 MSB3277 assembly conflicts. |
| Shell | `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_Story1122HiddenControlNegativeProbes_RemainClean` | Same Release assembly conflicts; no analyzer policy weakened. |
| Shell | `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean` | Same Release assembly conflicts; dependency alignment remains open. |
| SourceTools | `PackagedAnalyzerConsumerTests.PackagedAnalyzer_ContractsOnlyPayload_GeneratedShellConsumerCompiles` | Expects Fluent 5.0.0; unchanged catalog resolves 5.0.1. |

### Review patch and focused verification

The focus return now waits until a native dialog closes, ignores Escape as a replacement move while that dialog is open, and focuses the route heading when a connected origin cannot receive focus. If neither destination is focusable after dismissal, it waits up to five seconds for a heading to mount and then removes its retry timer and both document input listeners. The open-dialog wait has no elapsed-time cutoff. The synthetic regression holds the dialog open for 20 animation frames, dismisses it with Escape, and checks both the normal origin return and the connected hidden-origin fallback. The real timed lifecycle matrix refreshes the selected confirmation immediately before target, clipping, icon, contrast, forced-color and screenshot measurements, and verifies that the selected state remains visible afterward. Its contrast and geometry thresholds are unchanged.

The exact Debug Test-host launch command from the repository root was:

```sh
env ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true Hexalith__Shell__FormAbandonmentThresholdSeconds=5 Hexalith__Shell__TimeoutActionThresholdMs=5000 Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 dotnet run --project samples/Counter/Counter.Web/Counter.Web.csproj --configuration Debug --no-build --no-launch-profile --urls http://127.0.0.1:5070
```

The Debug host loaded `samples/Counter/Counter.Web/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll` with SHA-256 `386b1ec30868f4623fb9d5cf5ecae4f6759cd8d94e4a59041d51503391a92445`. In the earlier 15-case run, HTTP-served `fc-focus.js` had SHA-256 `d9f59ea38c4e2a57db88ab3747f00239c755ea6cfe470310e539122acbeace1d`. The focused three-engine command, with exact artifact paths and a skipped Playwright web server, is in the [verification manifest](evidence/story-13-6/2026-10-18-gate-rerun/verification.json). That earlier review digest (`sha256:4cf2cb299a7ec5c6c45b2e2e8dcb457990608dea6b2d00d84fbb6d0f637b6bc3`) passed **15/15, 0 failed, 0 skipped**. Raw [Playwright JSON](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focused-final/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focused-final/junit.xml), [HTML](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focused-final/html/index.html), [console log](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focused-final.log) and [host log](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/host-final.log) are retained. That 15-case result belongs to the immediately preceding source digest. For the current follow-up candidate, the two affected focus regressions ran first, followed by the complete browser and .NET gates above. The current host HTTP-served `fc-focus.js` SHA-256 is `f3464f07ed59a677cefcdc87a29e2ed8729c539badcfc3fee4ee45b25c6c7c2a`. The final [Playwright JSON](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-final/playwright.json), [JUnit](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-final/junit.xml), [HTML report](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-final/html/index.html), [console log](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-final.log) and [host log](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/host-focus-final.log) show **6/6 passed across Chromium, Firefox and WebKit**, 0 failed or skipped. The first cleanup test attempt failed because its harness counted one shared callback instead of two event types; that [failed attempt](evidence/story-13-6/2026-10-18-gate-rerun/review-fix/browser-focus-bound/playwright.json) and its screenshots/traces remain retained. TypeScript typecheck and JavaScript syntax check passed. The complete gates above are bound to this candidate.

The Shell skip is `LiveTwoTenantProductionAdapterTests.ProvisionedTenants_QueryCountAndSubscribe_StayIsolated`: no live two-tenant EventStore endpoint, identities or bearer tokens were configured. [Approved baseline hashes](evidence/story-13-6/2026-10-18-gate-rerun/approved-browser-baselines.sha256) and [approved .NET snapshot hashes](evidence/story-13-6/2026-10-18-gate-rerun/approved-dotnet-baselines.sha256) are retained; the snapshots were not regenerated.

No dated NVDA, JAWS, VoiceOver, heard-speech, actual 400% browser zoom, physical tablet/phone or owner-approval record was supplied. The automated 320 CSS-pixel viewport checks do not close actual zoom or real-device gates. The checklist below remains open, along with Windows execution, WebKit actual forced-palette observation, live EventStore fresh-row acceptance, visual approval and catalog/policy alignment.

### Historical review evidence — 2026-10-17

Updated 2026-10-17. **Readiness remains open; Story 13.6 stays in-review.** The current review patch is based on root `0618c1102d7858c14e3d5f5af13b88b06497ab37` on `main`. Its tracked code/test diff has `sha256:65b5464381deda2e0786e3f8aa3c83ebb7b5fa00c0125aad8e8a47091626b51f` (`git diff --binary 0618c1102d7858c14e3d5f5af13b88b06497ab37 -- src tests | sha256sum`). This identifies the review patch, not a full candidate manifest.

**Prior broad Story 13.6 browser gate (before the current review patch):** From `tests/e2e`, `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --workers=4 --retries=0` ran 732 cases across Chromium, Firefox and WebKit: **711 passed, 18 failed, 3 skipped, 0 flaky**. All 18 failures are the retained ordinary visual comparisons; there are **zero nonvisual failures**. [Prior broad verification summary](evidence/story-13-6/2026-10-17-review-continuation/verification.json) retains the failed-test inventory and SHA-256 of the raw Playwright report. The generated destructive-form validation journey also passed 3/3 across the same engines, proving that its description remains linked while Fluent displays the single validation error. DOM focus and status checks do not establish heard speech.

**Prior broad .NET gates and current build checks:** Debug Shell.Tests and SourceTools.Tests builds pass with zero warnings/errors after local restores against the declared catalog. The earlier full Shell run reported **3709 total, 7 failed, 1 skipped**; failures include existing catalog/governance and Fluent snapshot drift. The earlier full SourceTools run reported **1313 total, 1 failed**: its packaged analyzer expects Fluent 5.0.0 while the declared catalog resolves 5.0.1. Focused `CommandRendererWrapperIntegrationTests` pass 70/70 and `CommandFormEmitterTests` pass 68/68. The earlier affected hidden-banner, StatusOverview and virtualization tests and 6/6 synthetic browser fixtures passed. Current AppHost, Shell.Tests and SourceTools.Tests serialized Debug builds, TypeScript typecheck and `git diff --check` pass. No tracked dependency declaration changed.

**Current review checks:** Against the gated direct Test host, the full responsive accessibility spec passed **453/453** across Chromium, Firefox and WebKit. The accessibility state matrix passed **224**, with **one WebKit capability skip** and no failures. The new shadow-label, offscreen-target, clipped-cover and transformed-pseudo fixtures passed 12/12 across the three engines. The palette forced-colors journey passed 6/6; Chromium and Firefox assert disabled label and hint `GrayText`, while WebKit retains the actual palette-paint evidence limit. The reduced-motion CSS fixture passed 6/6 and does not establish a live reconciliation transition. [Current review verification](evidence/story-13-6/2026-10-17-review-continuation/review-2026-10-17-verification.json) records commands, counts, the retained state-matrix report digest and limitations. No current combined visual or full .NET test gate was run.

**Prior complete candidate:** Source `sha256:0bfddb4016c3bfe00e84ae88ca698b3a63c373b1ac2be5048577cdae0ee6b77a` at root `008736825376e6e2b419cb8d3b81067a8f9cc3b1` completed the earlier 726-case browser packet: 705 passed, 18 visual comparisons failed and 3 skipped. Its [source manifest](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/source.json), [verification report](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/report.md) and [browser outcomes](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/verification/browser-outcomes.json) are historical evidence; the current review patch has its own results above.

Manual NVDA/JAWS/VoiceOver, actual 400% zoom, tablet/phone and heard-speech observations, Windows execution, WebKit actual forced-palette evidence, owner approvals, visual approval, catalog-policy alignment, raw artifact-size gate and live EventStore fresh-row acceptance remain open. All 24 approved image bytes, four frozen intent blocks and eight root submodules remain unchanged. The original story baseline is `3c8679628ebdb8995afc09f712a04fcde623695c`.

## Human evidence checklist — leave each box open until observed

Use [the existing manual log template](../../docs/accessibility-verification/manual-log-template.md) for future runs, including candidate identity, human tester, environment, observed outcome and sanitized evidence. The [2026-10-27 amendment](#story-specific-manual-acceptance-amendment) waives completing the requested supporting details for the seven existing Owner-reported Pass records below. Unreported observations remain unreported; do not paste raw screen-reader transcripts or tenant/user values.

| Gate | Owner-reported result | Canonical qualification status | Owner / release impact / next action / reopen trigger |
| --- | --- | --- | --- |
| MAN-136-NVDA | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-nvda) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-JAWS | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-jaws) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-VO | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-vo) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-ZOOM | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-zoom) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-TABLET | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-tablet) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-PHONE | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-phone) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| MAN-136-ZERO-FIELD | **Pass** | `completed` | Jérôme Piquot, Owner, renewed this Pass on 2026-10-27 and waived completing the requested supporting details. See [record](#man-136-zero-field) and [amendment](#story-specific-manual-acceptance-amendment). Revalidation follows material implementation changes or a subsequent Owner decision. |
| UX-136-REVIEW | Not a manual Pass record | `performed: approved three-lens review on 2026-11-02; three unique High findings open` | The current rubric, accessibility and Fluent reviews are summarized above. Earlier scoped Build review remains historical. Human UX/Product/Fluent sign-off remains open and is not inferred. |

All gate rows above share task/story `13.6` and release branch `main`; future manual runs name the candidate actually tested, while the seven existing reports retain the explicit supporting-details waiver. Jérôme Piquot is the named Owner/reporting human and has explicitly confirmed Pass for all seven records. The seven [human-reported records](#human-reported-verification-records--2026-10-25) retain his exact statement. Their canonical qualification is `completed` under the dated supporting-details waiver; their reported result is Pass. The eighteen detailed evidence checkboxes below remain unchecked as an inventory of observations not itemized in the reports, and do not reinstate the waived metadata blocker. The current UX-136-REVIEW row records the later approved three-lens review; earlier scoped reviews retain their original attribution. AE-01–AE-09 technical behavior, the actual-zoom scope and independent acceptance requirements remain unchanged.

### Common operator journey on every supported AT/browser pairing

- [ ] Exercise shell skip links, hamburger/drawer/72px and 48px rails, account/settings/menu controls, Home cards and CTAs, navigation and route headings, tab selection, toolbar search/filter/view/actions, projection loading/empty/error/recovery/grid/detail, command form validation, lifecycle recovery, palette results, dialogs and the in-flow abandonment guard.
- [ ] At 320 CSS pixels and separately at **actual browser zoom 400% from 1280 CSS pixels**, repeat every operation and overlay. Confirm logical reading/focus order, no lost or clipped control, no page-level horizontal scrolling, and bounded labelled grid/tab scrolling with the entire focused target and ring visible.
- [ ] Apply all four spacing overrides together. Confirm complete labels, instructions, statuses, errors and controls; grid rows grow when needed without inaccurate virtual spacers. Preserve the ordinary compact 32px minimum and 75rem constrained measure.
- [ ] Inventory every pointer target including open overlays. For each target below 24×24, record exactly one permitted exception, dimensions/equivalent control, rationale, evidence and keyboard path. Validate spacing circles against both neighboring target rectangles and undersized-target circles.
- [ ] Traverse every keyboard stop with sticky chrome, messages, drawers, scrollports and active overlays present. Confirm the whole target and ring are visible, modal focus is contained, and each return/fallback destination remains unobscured.
- [ ] In light/dark and forced colors, inspect UX-VC-1 text, secondary instructions, current navigation/tab, focus, status/lifecycle, fresh cue, validation, unavailable and no-access states. Retain measured 4.5:1 normal text, 3:1 large text/non-text/focus evidence. Confirm text/icon/shape/current-state meaning survives authored-color suppression.
- [ ] With reduced motion enabled, verify immediate navigation, validation, lifecycle, reconnect and fresh-row cues; no essential information requires animation, hover or smooth scrolling.

### Approved missing-item sequence

- [ ] Expand a generated detail, focus a detail control, then remove the item through the completed data result without changing filters. Hear the neutral unavailable explanation once through its focused non-live text; verify no Clear Filter control and no AM-22 copy.
- [ ] Repeat with focus in a surviving control outside detail. Preserve that focus; hear one shared polite neutral explanation. Refresh the unchanged missing result repeatedly: no refocus or duplicate speech.
- [ ] Delay recovery, then navigate, change tenant/user scope, or move operator focus/input elsewhere. Confirm the old recovery never takes focus or reads stale content. Recheck a genuinely filter-hidden item separately: preserve its existing filter-only AM-22/Clear Filter behavior.

### Approved zero-field failure sequence — gated Test host now exposes the real renderer

- [ ] Trigger safe validation failure, permission/unavailable/rate-limit/transport failure and unmapped rejection. Include field-only server validation, a normally returned rejection, blank unavailable/rate-limit problem text, Retry-After guidance, admission-scope-unavailable and submit-time policy denial. Confirm existing safe outcome text appears in compact Fluent feedback beside the trigger, wrapping below at narrow width/zoom/spacing. Keep trigger focus and hear exactly one message; inspect shared status ownership, without counting DOM changes as heard speech.
- [ ] Wait and refresh: feedback remains, no retry or duplicate speech. Start a new attempt: prior feedback clears. Navigate/change scope: prior feedback is removed. No extra dialog opens.
- [ ] Accept a new command: use the existing pending-command summary and truthful lifecycle; do not infer confirmation from transport acceptance.

### Direct speech, silent restore and new-message sequence

- [ ] Hear direct route/denial headings, focused non-live validation/unavailable text, shared polite lifecycle/projection status, and palette-owned combobox result status. Each event uses its specified owner once; no competing live node reads the same event.
- [ ] Navigate away/back or remount/restore already-delivered state. The old announcement stays silent. Then cause a distinct valid result/command/connection/palette event: the new message is heard once.
- [ ] Observe silent polling/retry ticks, unchanged background refreshes, rejected/suppressed fresh cues, dismissal and expiry. Fresh New/Updated appearance speaks only for eligible material change. Use the preserved live-only fresh-row lane for EventStore proof; this candidate does not close it.

### Physical tablet and phone

- [ ] On a tablet, use touch-adapted navigation, target activation, fields, toolbar/popovers, grid/detail, palette/dialogs, lifecycle/recovery and rotation. Confirm visible controls, correct return focus, readable status and system-color/motion preferences.
- [ ] On a phone, establish functional fallback for the same essential operations, including row actions exposed through detail and bounded two-dimensional scrolling. Record constraints honestly; do not claim full daily-use phone support.

## Remaining acceptance gates

The [2026-10-27 amendment and verification](#owner-amendment-and-current-source-verification--2026-10-27) owns current manual qualification and remaining independent requirements. The following paragraph and machine-readable snapshot retain only the historical 2026-10-18 reviewed candidate; their manual, visual and .NET failure statements are not current qualification claims.

All 18 manual boxes stay open. NVDA/JAWS/VoiceOver, heard speech, actual browser zoom, physical tablet/phone and human reviewer observations remain unperformed. OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and live EventStore acceptance remain unresolved. The final reviewed browser run passes every executed nonvisual case and the unchanged per-root size limit; eighteen visual comparisons and four existing .NET snapshot/catalog tests remain failed. Complete capture/portability integrity passes, while the full artifact validators and accessibility classification remain blocked. Historical oversized and interrupted packets remain failed history, without changing the cap. Windows and actual WebKit forced-palette coverage stay open.

### Historical machine-readable snapshot — 2026-10-18

The following values describe the dated reviewed candidate only; they are not current-run results.

```yaml
story: 13.6
story_status: in-review
sprint_status: review
final_classification: blocked
current_tested_base_root: 1f8c7b2b4424b6232121a7cc567f7129e46577d3
current_source_diff_sha256: b6b5b7a761b4927d21ee0fe9ba5daf78ce31ea87928f28b3e2a3b842e8fe5fc3
current_debug_shell_build: passed; 0 warnings / 0 errors; reused unchanged assemblies
current_banner_tests: included in the current configured Shell lane; prior standalone 39/39 is historical
current_helper_regressions: passed; 14 focused browser / 1 capability skip; 25 tooling; 24 evidence tests
current_responsive_spec: passed; 482 passed / 1 capability skip
current_browser: failed; 774 total / 752 passed / 18 visual failed / 4 capability skips / 0 flaky
current_browser_visual: failed; 18 comparisons; approved images unchanged
current_browser_nonvisual: passed; all executed cases across three engines
current_shell_configured: failed; reused 3703 total / 3699 passed / 4 existing snapshot-or-catalog failures / 0 skipped
current_source_tools: not rerun; prior Fluent-version approval failure remains open
current_artifact_size_gate: passed; all six roots below 52428800 bytes
current_artifact_root_bytes:
  chromium: { results: 33234618, html: 28329422 }
  firefox: { results: 43944218, html: 38509133 }
  webkit: { results: 35832621, html: 31548420 }
current_full_artifact_gate: failed; exactly six unsuccessful visual outcomes per engine; no additional state-evidence errors
current_portability_integrity: passed; 7310 files / 968 JSON attachment paths / 709 JUnit markers; failed gate outcomes preserved
current_evidence: evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/verification.json
complete_portable_artifact_packet: true
full_artifact_validator_passed: false
accessibility_approval_inferred: false
complete_portable_artifact_packet_scope: complete final reports, captures and HTML assets; no overall accessibility or artifact-gate pass
protected_baselines_changed: 0
root_submodules_changed: 0
windows_execution: not performed
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
heard_speech: not observed
webkit_actual_forced_palette: not observed
visual_and_catalog_owner_approval: not supplied
product_approval_inferred: false
apphost_running: false
staged_committed_pushed: false
```

## Retrospective follow-through — 2026-10-22

Historical packet, baselines, delivery entries and owner decisions above remain unchanged. The
[bounded follow-through receipt](tests/epic-13-follow-through-2026-10-22.json) binds the current root
revision, changed runtime/test inputs, tested Shell assembly, realm/fixture and EventStore identities.
It records successful lifecycle/replay checks and the unsuccessful live prerequisite check. No
manual observation, accessibility acceptance, Fluent approval, OI-16 closure or G-4 closure is supplied.

### Supported historical replay

From a clean checkout containing historical Git objects, run:

```bash
python3 eng/replay_story_13_6.py
```

Optionally pass `--scratch-root` naming an existing directory outside the checkout. The wrapper
retains a fresh external scratch tree and its receipt/audit/restore logs; it never replaces current
helpers, dependencies, baselines or sealed packets. The [reconstruction recipes](tests/story-13-6-historical-runtime.json)
bind all 36 packet inputs and restore 29 runtime/dependency/baseline inputs from Git
`397dac34a1bd1bf4d789161794a66811360cb59a`. Runtime and package JSON use CRLF; the lockfile uses
CRLF except LF on lines 14/15; all 24 PNGs retain raw bytes. Size and SHA-256 checks precede npm or
JavaScript execution. A fresh locked `npm ci --ignore-scripts --no-audit --no-fund` runs only in
scratch; `NODE_PATH` and `NODE_OPTIONS` are cleared and extraction stays in scratch.

Observed with Node `v26.4.0`, npm `11.18.0`, and locked playwright-core `1.63.0`: wrapper **exit 0**,
all **2,079 files** and **116 required states per engine** verified, including support logs, unique
engine/run identities, attachment relocation and unchanged outcomes. The unchanged sealed raw
validator still exits **1**: **752 passed / 18 visual failed / 4 skipped / 0 flaky**, overall
accessibility classification **blocked**. Wrapper success means historical reconstruction matches;
it does not turn the original visual failures green. Missing history or corrupt inputs fail closed.
Earlier direct live-helper replay commands remain historical; this is the supported reconstruction
procedure after helper changes. No 15.6/15.7 storage migration or evidence retirement is claimed.

### MAN-136 handoff and observations

E13R-AI-2 stays **open**. The seven existing MAN-136 rows and all eighteen manual boxes above remain
unperformed/unchecked: no named human tester, supported AT session, actual zoom observation or
physical device was supplied. Preparing this handoff supplies no observed Pass/Fail result.

Accessibility/Stakeholder and Quality/Test owners should assign named testers and a named reviewer
to the existing NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari, zoom, tablet, phone and zero-field rows.
Before each run, select and record the exact tested root revision plus patch/artifact hashes and
executing Components/Icons identity; the historical packet and this focused lifecycle candidate
must not be conflated. Use the existing [manual log template](../../docs/accessibility-verification/manual-log-template.md)
and the common operator/zero-field sequences above. Record date, full OS/browser/AT or device/model
versions, route/journey, orientation and responsive tier, actual 400% zoom from the measured 1280 CSS
pixel reference, heard-speech observations, Pass/Fail, issue links, sanitized evidence and the named
reviewer's approval reference. Reuse these MAN-136 identifiers; append observations to this result.

The release impact remains blocked accessibility completion. Reopen each `not performed` row when
the named human and required equipment can exercise the identified candidate. Automated focus,
DOM/speech-owner assertions and viewport resizing cannot satisfy heard speech, actual browser zoom
or physical-device observations. No assignment, version, result or sign-off has been invented.

### Current verification and live tenant boundary

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug --no-restore -m:1 -v:q`
  — passed, zero warnings/errors. The direct built assembly with `-class '*FcRouteFocusTests'`
  — **21 passed**, zero failures/skips/errors. Held imports, late failures/callbacks, repeated
  disposal, disconnected-JS cleanup and mounted normal focus are covered.
- `python3 -m unittest discover -s tests/eng -p test_replay_story_13_6.py` — **8 passed**,
  including corruption, absent historical objects, exact newline/binary recipes and isolation.
  `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs` — **24 passed**.
- Initial fixture-enabled `TenantScopeFixture__Enabled=true aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json`
  — exit **2**, NETSDK1004 for Commons.UniqueIds and Memories.Contracts/Client.Rest source assets.
  An initial Shell file-copy lock warning was also retained. The startup attempt preceded edits,
  but completed after the first lifecycle patch; no healthy topology baseline was established before
  those edits. Exact-target stop/describe subsequently confirmed no running AppHost.
- `dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Debug -v:q`
  — exit **0**, but the next startup still reported missing source assets. Individual Debug restores
  of `references/Hexalith.Commons/src/libraries/Hexalith.Commons.UniqueIds/Hexalith.Commons.UniqueIds.csproj`,
  `references/Hexalith.Memories/src/Hexalith.Memories.Contracts/Hexalith.Memories.Contracts.csproj`, and
  `references/Hexalith.Memories/src/Hexalith.Memories.Client.Rest/Hexalith.Memories.Client.Rest.csproj`
  all exited **0**. Repeating the exact fixture-enabled start then exited **2** with **CS1704**:
  Tenants.UI imports both source and NuGet `2.27.1` Memories.Contracts/Client.Rest assemblies
  (two errors, two MSB3243 warnings). No resource reached readiness and no token was acquired.
  Dependency versions and gitlinks were not changed to bypass that source/package conflict.
- All seven required `FC_TEN_SCOPE_*` endpoint/tenant/user/token inputs were absent. The direct
  built assembly with `-class '*LiveTwoTenantProductionAdapterTests' -result-trx "$RESULTS/live-two-tenant.trx"`
  in a fresh result directory exited **0** with **one skipped / zero passed**. The existing exact
  Passed-identity validator exited **1**, reporting `ProvisionedTenants_QueryCountAndSubscribe_StayIsolated=NotExecuted`.
  The receipt retains the TRX hash and exact commands. This is failed prerequisite evidence, not
  a current query/count/SignalR isolation pass.

E13R-AI-5 stays **open** pending a provisioned zero-skip run. Its
[candidate revalidation requirement](../../tests/Hexalith.FrontComposer.CounterFixture/README.md#candidate-revalidation-requirement--2026-10-22)
now requires the existing production-adapter proof before approving relevant scope, authorization,
storage, adapter, server, AppHost/realm/fixture or EventStore changes, and again after further relevant
changes. The advisory default lane and Story 13.1's historical pass remain intact. Final exact-target
Aspire stop and describe confirm no AppHost running. E13R-AI-1/3/4 are completed by the focused fix,
dated tracking reconciliation and supported replay; human/live prerequisites remain separately open.

Final repository checks: `python3 eng/validate-story-artifacts.py` passed with the sentinel scan
enabled; `git diff --check` passed. No sealed packet, approved baseline, dependency version, gitlink,
historical decision or unrelated work was changed. No staging, commit or push was performed.

Parent browser regression: rebuilt Counter Debug (zero warnings/errors) and executed the existing Chromium route-contract module-routing, invalid-tab and palette/settings focus-return journeys: **3 passed / 0 failed / 0 skipped**. Artifacts remained outside the checkout and the synthetic Test host was stopped. The bounded receipt records the exact selection and executing Shell assembly hash. This is focused UI regression proof, not live tenant isolation or human accessibility evidence.

Post-review final verification: Debug Shell build passed with zero warnings/errors; **22/22** focus tests, **11/11** replay tests and **24/24** Node evidence tests passed. The supported historical command reran successfully with the same 2,079 files, 116 states/engine and blocked 752/18/4/0 outcome. Review added command-exit and surviving-owner regressions, manifest-snapshot provenance, Windows temporary-variable confinement, bounded failure diagnostics, and CI helper-suite wiring; Windows execution itself remains unperformed. A fresh final live test still skipped, and exact-identity TRX validation still rejected NotExecuted. E13R-AI-2/5 remain open. The current bounded receipt contains the final input/assembly hashes and exact raw-byte convention.

## Evidence policy — 2026-10-23

**Evidence policy (2026-10-23):** proof is the repository's tests plus CI, recorded here as commands, pass/fail counts, commit SHA and CI run link (PRD §7). The committed `evidence/story-13-6/` folders this record links to were removed by [`spec-evidence-storage-simplification.md`](spec-evidence-storage-simplification.md); `evidence/` folders are ignored local scratch and `eng/check-committed-files.sh` rejects new evidence commits.


## Full-topology readiness candidate and scope blocker — 2026-10-28

**The disposable nine-application health rehearsal passes; authenticated navigation fails 3/3, so full-topology readiness remains unqualified and this story/sprint remain in-progress.** Candidate is root `5688c3eba98567133285b0f51843d9daa8f24f0b` plus the uncommitted implementation. EventStore is `b62a5f4b376fbd5994ef811f80d652e7c6e53f54`, Tenants `ec2d21a93cc8591eabdba17b2004dfcc6482b6e3` and Parties `6ba02654ca390b3333183962355bd5c5857b8224`; root dependency selections, authentication enforcement and approved baselines are unchanged.

The [bounded full-topology spec](spec-13-6-qualify-full-topology-readiness.md#current-execution-and-scope-blocker--2026-10-28) owns the unapplied amendment. Full selection reuses the ordinary graph only after isolated maintenance, verified native backup, authenticated activation and persisted marker readback. Every state/pubsub/configstore file is private and strictly validated; all existing sidecars use private resource/discovery/placement/scheduler paths. The default gateway rehearsal retains its synthetic Counter. Full-selected maintenance composes only the maintenance gateway; full browser navigation retains the existing fixed OIDC callback URLs under exclusive lifecycle ownership and refuses occupied ports.

Disposable Keycloak has an owner-specific physical name/label, checked before private realm reads; its logical security resource and existing user-client contracts are unchanged. Startup, failure and cancellation cleanup retain exact ownership checks. A real cancellation fixture failed once while a source build was active. A deterministic reproducer then proved SIGTERM could arrive inside Popen startup before the cleanup guard held its child handle; the original child remained alive until explicit fixture rescue. The fix defers cancellation only until the handle is owned inside the cleanup guard, then terminates the child and runs its EXIT trap before returning. Both deterministic and ordinary cancellation cases pass. The earlier failure is preserved rather than erased by retries.

Selected Parties Development settings retain a dormant `http://memories-server/` route even with Memories indexing disabled; its disabled-mode health check probes remote cleanup/status. The first direct Parties check correctly reports HTTP 200 / Degraded for `memories-search`. The disposable full configuration now refuses source-enabled indexing/inherited Memories overrides and, only after pristine-store verification, clears `Parties:MemoriesSearch:{Endpoint,ApiToken,TenantId,CaseId}` while retaining disabled indexing and all local mapping/inventory probes. Degraded still fails. Ordinary development settings are untouched; no remote Memories capability is qualified.

| Command / exact scope | Observed outcome |
| --- | --- |
| `python3 tests/e2e/scripts/verify-isolated-readiness.test.py` | **48 passed**, zero failures, including topology parity, maintenance/readback order, missing/unhealthy apps, component/discovery/shared-route refusal, real cancellation and prerequisite refusal before credential access. |
| `npm --prefix tests/e2e run test:epic-9-evidence` | **186 Node + 48 Python passed**, zero failures/skips. |
| `npm --prefix tests/e2e run typecheck` | Exit **0**. |
| `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` (the runner's individual source build) | Latest observed source build succeeds with **0 warnings / 0 errors**. |
| Individual Shell Debug build; final focused rebuild adds `--no-restore --no-dependencies` after the complete dependency build | **0 warnings / 0 errors**. |
| `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | **51 passed**, zero errors/failures/skips/not-run. |
| `python3 eng/verify-isolated-readiness.py --topology full` before the final prerequisite guard | Native backup/pristine-state proof, authenticated activation **200**, exact persisted v2 marker readback, all **9 applications Running/Healthy**, all **6 sidecar inventories**, and direct health/readiness contracts pass. Sample uses its documented exact JSON strings `healthy` at `/health` and `alive` at `/alive`; it has no `/ready` route. Browser navigation then **fails 3/3**. Owned AppHost, infrastructure, private configuration and generated realms are removed. The full run does not reach the unchanged rejection/Epic 9 helpers. |

The last live attempt is `/tmp/story13-6-isolated-readiness-lr6h5acv/`; its sanitized `navigation.log`, per-app health summaries and browser error contexts retain exact failures. Both combined and Tenants UI complete sign-in and focus their Tenants headings, but display the query gateway's unauthenticated/unauthorized message instead of an authorized data/empty surface. Admin focuses its heading but shows “Tenants service is not responding” and a stale/unavailable alert alongside the empty heading. The Admin assertion stops on its missing accessible Create-tenant button; the captured alert independently prevents accepting that empty display as successful data.

Read-only diagnosis identifies a missing composition contract: the selected UI read client always applies Dapr invocation routing to `tenants-api`; when a BFF sidecar is absent it falls back to `Tenants:BaseAddress`, which this AppHost sets to the owned domain processor. The selected Tenants module's own AppHost instead composes `Hexalith.Tenants.Api`, a Tenants UI BFF sidecar and a GET-only API receiver policy. FrontComposer composes none of them. No localhost:3500 request was observed on the selected UI path. A final prerequisite guard now refuses the missing API/BFF composition and any absent/unmatched SDK endpoint or default/unowned sidecar listener **before credential reads and authenticated navigation**. This guard has focused test evidence; the preceding live failures retain their exact attribution.

The unapplied proposal extends the application count **9→10** and ordinary sidecars **6→9** (API plus two BFFs), adds a source metadata/build edge and private invocation paths, and explicitly requests the new narrow Dapr caller policies. Existing JWT/OIDC/domain authorization stays enforced. This is an authentication-policy scope amendment under the frozen spec and has not been applied. Admin is a separate path: its query service forwards the caller bearer through EventStore `api/v1/queries`; adding the API host alone does not establish an Admin fix. Safe HTTP/envelope/membership diagnosis and, only if needed, the documented delegated-human bootstrap with actual status and authoritative membership proof are proposed. No bootstrap command, API host or new caller policy was added.

Earlier attempts retain their original scope:

| External scratch / attempt | Retained outcome |
| --- | --- |
| `xg9pqltz` | Pre-AppHost-change reduced rehearsal passes backup/activation/readback, four actual first-endpoint 401 responses, one unchanged Epic 9 Chromium test/artifact validation and owned cleanup. Runner/checker/test files changed while it ran, so it is explicitly mixed runner attribution, not a clean final-candidate baseline or full qualification. |
| `ibm9xhxd` | First full prebuild deliberately canceled to finish diagnostic privacy changes; exit 1 through cancellation, private configuration removed, Aspire ps empty. |
| `xeszpgq4` | First full composition reaches ordinary gateway health and all app waits, then fails a direct application health-contract check before per-app diagnostics existed. Owned cleanup succeeds. No browser/rejection/full proof qualification. |
| `go3mbrth` | Clean source build, then network/start failure. Docker events show the Aspire network created at 00:17:15 UTC and destroyed at 00:17:17; the deleting actor is unknown. No ambiguous network was manually changed. Owned cleanup succeeds. |
| `x1zhij3e` | Fresh CLI start recreates networking and passes all three infrastructure waits. Keycloak is killed with signal 9 at 00:24:14 UTC, exits 137 and is destroyed before this runner's cleanup; no OOM event or deleting actor was established. Source build and owned cleanup succeed. |
| `4xxp98wi` | Owner-named identity and maintenance/readback pass. The full run waits on the harness's mistaken sample `/ready` assumption; source inspection establishes the actual `/health`+`/alive` contract. Controlled SIGTERM cleanup succeeds. |
| `4jmr3fs0` | Correct sample contract; direct Parties health correctly fails Degraded `memories-search`. This exposes the dormant remote configuration, later isolated as described above. Owned cleanup succeeds. |

Those abbreviated scratch names all expand under `/tmp/story13-6-isolated-readiness-`. Final command logs are `/tmp/frontcomposer-full-topology-{python-final,evidence-final,typecheck-final,shell-build-final,shell-tests-final}.log`; the AppHost source build remains in the last live scratch. `/tmp/frontcomposer-full-topology-cancellation-first-failure.txt` is explicitly a retained terminal excerpt after the original path was reused, not a recreated raw report. `/tmp/frontcomposer-startup-race-before.log` and `...-after.log` preserve deterministic before/after cleanup observations. Raw reports/logs stay external; no evidence packet or manifest is committed.

Existing successful [CI40586087034](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586087034) and [Quality40586086551](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586086551) remain attributed to historical `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4`. There is no new hosted result or publication for this patch. All seven MAN-136 records remain **completed — Owner-reported Pass; supporting details waived**; their eighteen detailed observation boxes are untouched. Actual WebKit system-palette paint and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independent. Parent/sprint stay **in-progress**, and full authenticated topology acceptance stays open pending an approved amendment and successful real verification.


## Approved API and BFF candidate, blocked qualification — 2026-10-28

Story 13.6 remains **in-progress**. The original baseline remains `5688c3eba98567133285b0f51843d9daa8f24f0b`. The separately published external `/pushall` commit `5f665c4d38afb2255a7dde4620330e09824a40a1` captured the preceding candidate and approved amendment; this implementation performed no commit, push, dependency update or nested initialization.

The candidate adds the selected Tenants API, its source build edge and invocation-only API/BFF sidecars to the ordinary graph: **10 applications / 9 ordinary sidecars**. The domain processor remains composed. Both BFFs use their own SDK sidecar endpoints and reference the API; API JWT authority/audience validation uses the existing platform helper without injecting service credentials, preserving the original inbound human bearer relay. The API receiver admits only the six approved GET read routes from the two BFF IDs; both BFF receivers deny all invocation operations. New sidecars have zero state/pubsub component access. Private discovery/config validation, API dependency health, exact endpoint ownership, input revision/patch guards and live boundary probes are implemented. The runner records safe independent Admin query/HTTP/membership outcomes when that phase is reached and preserves failed boundary probe statuses. Default gateway selection, maintenance ordering, existing rejection/Epic 9 scripts, OIDC clients and approved browser baselines remain unchanged.

### Stable inputs and focused checks

To avoid ongoing external publication changing live inputs, verification used detached checkout `/tmp/fc-story13-6-stable-vishqiou/checkout`: canonical root `5f665c4d38afb2255a7dde4620330e09824a40a1` plus the story-owned working patch. Its eight root-declared submodules were materialized as detached worktrees from their exact locally available root pins; no network or nested initialization was used. Runtime/build inputs were Builds `9f85d8e66e5c16675cee8c224f1fd88430d96007`, Commons `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a`, EventStore `903174461632e7bcabd6111092aa059f16e1b1c7`, Tenants `750bdc2e8de77595a55bf595c335094cce0d580c`, Parties `3e1dd7cac6ee4db1db48d1bfb478e1b5848f7b8e`, and Memories `86011d4dc1e16d0d8222e07e6a492faf8e2347c0`. AppHost SDK is 13.6.4; local CLI/DCP bundle is 13.6.1; Dapr is 1.18.7. The original baseline is not relabelled as this candidate.

| Sequential command / evidence | Observed result |
| --- | --- |
| AppHost `dotnet build ... --configuration Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` within each owned rehearsal | Stable builds succeeded with **0 warnings / 0 errors** (2m04s and 2m23s). |
| Individual Shell.Tests Debug build with those build controls; `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | Build **0 warnings / 0 errors**; **55/55** tests, no errors/failures/skips. `/tmp/fc-amendment-stable-shell-{build,tests}.log`. |
| `python3 tests/e2e/scripts/verify-isolated-readiness.test.py` | **54/54**, including retained cancellation coverage, strict API/BFF policies, health format, input changes, safe realm-loss diagnostics and failed-probe evidence. `/tmp/fc-amendment-stable-python-final.log`. |
| `npm --prefix tests/e2e run test:epic-9-evidence` | **186 Node + 54 Python**, all pass. These are helper tests, not a current live Epic 9 pass. `/tmp/fc-amendment-stable-helper-final.log`. |
| `npm --prefix tests/e2e run typecheck` | Exit **0**. `/tmp/fc-amendment-stable-typecheck.log`. |

Input/copy attribution is retained in `/tmp/fc-story13-6-stable-vishqiou/handoff-inputs.json`; each real run includes `source-inputs.json` with revisions and patch/input hashes. Only the two refined Python files were copied back after checking the captured main-file hashes (`safe-copyback.json`). External main-checkout submodule advances were preserved, not promoted to these stable results.

### Retained real attempts and lifecycle blocker

- The stable pre-change bounded maintenance baseline at `/tmp/fc-amendment-baseline-9ey_d49v` passed its source build, expected maintenance component phase and owned workload exchange, then completed owned cleanup. It deliberately attempted no activation, full writers or navigation. `/tmp/fc-amendment-maintenance-baseline.log`.
- Main-workspace attempt `/tmp/story13-6-isolated-readiness-mtnpkwyd` passed backup/activation/readback bound to EventStore `90317446...`, all **10 Aspire application waits**, and the original six sidecar inventories. It failed the checker at the new API's HTTP 200 JSON health response. Its saved response has Healthy self and gateway dependency results. The checker had incorrectly expected plain text; that mismatch was corrected and covered by focused tests. This attempt did not verify all nine sidecars or reach navigation. `/tmp/fc-amendment-full-first.log`.
- Main-workspace attempt `/tmp/story13-6-isolated-readiness-e6v7hici` passed maintenance/activation, then refused full startup because EventStore changed during the run from `903174461632e7bcabd6111092aa059f16e1b1c7` to the separately published `36c78f3435d2d492a73f1b0707cf865c8735d740`. Other external submodule advances were preserved. This failure motivated stable detached verification and broader input guards. `/tmp/fc-amendment-full-second.log`.
- Stable attempts `/tmp/story13-6-isolated-readiness-om2gr6ci` and `/tmp/story13-6-isolated-readiness-g5o2zxr5` passed clean source builds and expected gateway maintenance components, then lost their uniquely named/labelled private Keycloak containers before the owned realm read. Docker records show signal 9 / exit 137 / destroy at approximately **07:11:18** and **07:16:50 UTC**. Neither reached activation or the full phase. Both stopped the exact owned AppHost, removed only their verified private infrastructure/configuration, and ended with an empty Aspire inventory. `/tmp/fc-amendment-stable-full-{first,second}.log` and corresponding Docker event/lifecycle extracts under `/tmp/fc-amendment-stable-{first,second}-*` preserve these failures.

The captured AppHost/CLI PID pairs were `1040687/1040132` and `1283682/1282646`; DCP monitored those AppHost PIDs. Separate ContainerMonitor PIDs `1041357` and `1285455` were associated with the private identity containers. Logs report monitor-process shutdown near the losses; AppHost-wide stopping occurs later during runner cleanup. Both launches explicitly report `defaultWatchEnabled=False` and use `aspire run --isolated --no-build`; `aspire start --help` exposes no per-run no-watch flag. No watch-based fix is justified by this evidence.

Bounded kernel journal inspection found allocation warnings but no OOM/Killed-process record or either MonitorPID in that interval. Current `/init.scope/memory.events` and `.local` counters report zero OOM/OOM-kill/group-kill; the stopped PIDs' historical cgroup identities are unavailable. These observations do not establish a causal actor or complete root cause. Evidence is `/tmp/fc-amendment-kernel-{journal,oom-summary}.log`, `/tmp/fc-amendment-cgroup-scope-memory.log`, and the sanitized lifecycle extracts. No limits, privileges, unrelated processes or containers were changed.

### Actual caller-policy transport result and dependency boundary

A separate **stateless transport diagnostic**, not the product graph or acceptance proof, ran native Dapr 1.18.7 with the actual API/BFF ACL files, private SQLite discovery, empty resource directories, loopback listeners, no placement/scheduler/Sentry addresses and no credentials. Direct access to its diagnostic receiver returned **200**. Through Dapr, both approved callers (`tenants-ui`, `frontcomposer-ui`) received **403** for the approved `GET /api/tenants`, with zero receiver hits. Disallowed POST/path probes and an unapproved caller also received **403**. Receiver logs explicitly report that no SPIFFE ID could be read from the client certificate and apply the global deny policy while mTLS is disabled. All four owned processes exited **0**, and the private registry/configuration was removed. Exact report: `/tmp/fc-actual-acl-_daygjl7/transport-result.json`; receiver evidence: `/tmp/fc-actual-acl-_daygjl7/tenants-api.log`. The earlier smaller probe `/tmp/fc-actual-acl-q199lai7` is retained too.

The strict policy therefore fails closed on the current plaintext plane but cannot admit its intended callers. Authenticated product navigation cannot qualify on that transport. The selected Tenants AppHost already consumes canonical `DaprSelfHostedMtls.cs` and `DaprSelfHostedMtlsSubscriber.cs` from its Platform pin `b742b1b7a496ac72d6961ca6db5213d3df42d234`. Read-only inspection of that exact locally available object identifies Sentry, mTLS placement/scheduler, private issuer credentials and native `--enable-mtls` sidecar binding. Read-only comparison confirms that root-selected Builds `9f85d8e66e5c16675cee8c224f1fd88430d96007` already exposes every Tooling.Runtime type/member the helper calls: private workspace creation/deletion, port allocation, run/sidecar port records and the runtime pin. The earlier incomplete search incorrectly reported these APIs absent; that claim is corrected. No Builds update is established as necessary. Platform's historical Builds pin is not a proposed replacement: its catalog would downgrade the approved Aspire alignment. FrontComposer declares no root Platform dependency. No nested source was initialized, orchestration copied, dependency/pin changed, or policy relaxed.

A concrete amendment would make the canonical Platform helper a permitted root dependency/source surface, preserve existing Builds/SDK/package pins, link the two canonical helper files and existing root-selected Tooling project reference in the AppHost, and verify integration compatibility, and reuse them for private Sentry plus replacement mTLS placement/scheduler. It also needs owner labels/names and certificate-directory validation/cleanup in `LocalReadiness{Configuration,Topology}.cs` and the runner; native-sidecar executable/endpoint checks must recognize the exact selected `daprd`. API/BFF route policies, OIDC clients, original bearer forwarding, JWT validation and domain authorization must remain enforced. Ordinary counts stay **10 applications / 9 sidecars**; the infrastructure plane gains Sentry while replacing the two plaintext control-plane services. This amendment is proposed, not implemented or compatibility-qualified.

### Remaining acceptance

The current full nine-sidecar check, API bearer/caller/route/verb runtime lane, three authenticated navigation cases, independent Admin HTTP/query-envelope/membership diagnosis, and unchanged final rejection/Epic 9 live proof remain **unreached/incomplete** in stable verification. No missing authoritative membership has been established and no bootstrap command was dispatched. The Admin path must still be diagnosed independently before considering the existing delegated-human bootstrap; a heading or empty text cannot substitute for authorized, fresh data/status/membership evidence.

This section appends evidence only. All historical failed attempts and previous reduced/hosted results retain their original scope. The seven completed Owner-reported manual Pass records, waiver, frozen primary intent, WebKit paint exception and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect gates are preserved. No acceptance or completion transition is claimed.

Final controlled realm-loss/status logging refinements followed the two stable runtime failures and have focused/helper evidence only; no further full pass is claimed. Static Platform/Builds API comparison is recorded in `/tmp/fc-amendment-platform-api-comparison.json`; it is not an integration/runtime compatibility pass.


### Exact proposed canonical Platform enrollment (unapplied)

| File / surface | Required bounded change and check |
| --- | --- |
| Root `.gitmodules`; `references/Hexalith.Platform` | Declare `https://github.com/Hexalith/Hexalith.Platform.git` as a root dependency at `references/Hexalith.Platform`, with gitlink **`b742b1b7a496ac72d6961ca6db5213d3df42d234`**. Materialize only that root dependency; never initialize its nested references. This is the new dependency enrollment requiring amendment. |
| `deps.local.props` | Resolve `HexalithPlatformRoot` and `HexalithBuildsRoot` from the root-declared checkouts and propagate those roots through `_HexalithSourceReferenceProperties`, preventing an implicit nested fallback. Preserve existing Builds, package and SDK selections. |
| `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` | Add canonical `Compile` links from `$(HexalithPlatformRoot)/DaprSelfHostedMtls.cs` and `$(HexalithPlatformRoot)/DaprSelfHostedMtlsSubscriber.cs` (links `Platform/DaprSelfHostedMtls.cs` and `Platform/DaprSelfHostedMtlsSubscriber.cs`); add the existing-root `$(HexalithBuildsRoot)/src/libraries/Hexalith.Builds.Tooling/Hexalith.Builds.Tooling.csproj` source reference with `IsAspireProjectResource=false` and the existing source-reference properties. Do not copy helper implementations or add a package/version update. Compile/validate against the preserved current SDK/catalog. |
| AppHost `Program.cs`, `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`; `DaprComponents/sentry.yaml` | Reuse `DaprSelfHostedMtls.AddLocalApplicationInfrastructure` and its subscriber with canonical Sentry configuration, private issuer/run directories and mTLS placement/scheduler. Bind all participating sidecars to that owned plane. Add exact ownership/endpoint/configuration checks, reject inherited certificate/control-plane escapes, and dispose the AppHost before bounded deletion of only verified private credentials/run files. Preserve API/BFF ACLs and human JWT/domain enforcement. |
| Runner and focused safety tests | Discover/verify owned `dapr-sentry`, `dapr-placement-mtls`, `dapr-scheduler-mtls` resources, images/listeners/labels; replace plaintext placement/scheduler rather than keeping an active fallback. Accept only the selected native `daprd` executable with matching SDK endpoints. Include the Platform revision and private certificate/control-plane configuration in input/ownership guards. Require locally provisioned catalog-matching images, strict zero API/BFF components, and safe cancellation/failure cleanup. |
| Root dependency/configuration governance | Check root `.gitmodules`/gitlink consistency and source-authority mappings; update explicit root-dependency initialization/closure lists where required in `.github/workflows/{ci,quality,release,release-evidence}.yml`, `eng/dependency_graph.py` / its policy and `eng/workflow_source_closure.py`; run the relevant `.github/scripts/ci_governance.py` checks. Preserve workflow pins, all unrelated dependency selections and original baseline. No such governance changes were applied in this candidate. |
| Acceptance after enrollment | Rebuild AppHost/Shell in Debug, rerun focused helper/type checks and prove actual mTLS caller admission plus bearer/verb/path/BFF rejection, then 10 applications / 9 sidecars, authenticated non-stale navigation, independent Admin status/envelope/membership evidence and unchanged rejection/Epic 9 live proof. Resolve the independently retained owned lifecycle loss before claiming full acceptance. |

No Builds/package/SDK pin update is proposed. Existing Builds `9f85d8e6...` supplies the canonical helper's required APIs by exact source inspection; adopting Platform's historical Builds pin is neither necessary nor authorized. Static API matching does not substitute for compilation/runtime integration evidence. The added infrastructure resource is Sentry; ordinary application/sidecar counts remain ten/nine.

Final main-workspace observation also found the separate external publication `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c`, which captured the AppHost/policy/C# candidate and additional dependency advances while the detached proof remained stable. This implementation did not create that commit or update those checkouts. Current unstaged changes therefore do not represent the whole story. Review artifacts include the amendment against `5f665c4d...` and the complete story-owned change since original baseline `5688c3e...`; the separately authored SDK 13.6.3→13.6.4 hunk is identified as shared external scope, not attributed to this story.


## Canonical mTLS full topology proof — 2026-10-28

**Status remains in-progress.** The implementation is reviewable and its disposable runtime passes; strict dependency provenance is still blocked. No independent Build review, parent/sprint completion, release qualification, commit, push or publication is claimed.

Actual successful candidate: detached root `c49412ed567ac47e10a1cceec100d2c41505cf45` plus the recorded story-owned working patch. Original baseline remains `5688c3eba98567133285b0f51843d9daa8f24f0b`. Runtime working-patch SHA-256 is `6277a3d3815002b71322a01e7b514112eda145c9589590083a8172c3ca0d4072`; exact source/module/native-runtime identities are in `/tmp/story13-6-isolated-readiness-c2tg0uhi/source-inputs.json`. The final detached workspace is `/tmp/fc-mtls-final-gad9h52g/checkout`, with local exact root-pinned detached dependencies, no network and no nested initialization. Main source fixes were copied back only after matching captured working-tree bytes; external work was preserved.

`env -u HEXALITH_DAPR_HOME python3 eng/verify-isolated-readiness.py --topology full` exited **0**. The shell's inherited override is intentionally refused by the real runner; the synthetic unit suite now uses a minimal hermetic baseline while retaining explicit override-refusal tests.

| Actual runtime check | Result |
| --- | --- |
| Gateway-only maintenance, expected preactivation health, native pristine backup, activation and persisted readback | Passed; authenticated activation 200, persisted v2 marker bound to EventStore `36c78f3435d2d492a73f1b0707cf865c8735d740`; writers only after maintenance stop/readback |
| Ordinary full graph | All **10 applications Running/Healthy**, **9 native sidecars**, direct health/readiness, private canonical Sentry/placement/scheduler ownership and four actual process-level channel-token matches passed |
| Actual API/BFF mTLS boundaries | Both approved GET callers admitted with valid user bearer; **11 negatives** deny missing/invalid bearer, unapproved caller, verb/path and BFF receiver invocation |
| Independent Admin path | EventStore query 200 / success=true / authorized empty payload; ordinary Admin API 200 / array; pre-bootstrap native membership 403 was diagnosed separately |
| Real delegated-human bootstrap on independently pristine native state | POST **202**, retained MessageId `01M61462JZ7DFTEM4RNW2SZSYD`, CorrelationId `01M61462JZACSF0CF1SP59CJ03`; status **200 / Completed**; actual token-subject membership fresh, non-degraded and page-version-consistent at `tenant-sequence:1` |
| Authenticated combined UI, Tenants and ordinary Admin session | **3/3 Chromium cases passed**, including heading/focus, authorized data/empty state and no stale/unauthorized/alert surface |
| Unchanged invalid-bearer helper | All **4 routes returned 401** |
| Unchanged Epic 9 live proof and artifact validator | **1/1 passed**; artifact validator passed in explicitly allowed dirty-candidate development evidence mode, not final release acceptance |
| Exact owned cleanup | AppHost, all owner-labelled containers and owned native consumers absent; private issuer/run/configuration files removed only after absence checks |

Evidence root is `/tmp/story13-6-isolated-readiness-c2tg0uhi/`: `maintenance-result.json`, `mtls-plane-observed.json`, `native-channel-observed.json`, `admin-path.json`, `api-boundaries.json`, `bootstrap-result.json`, `navigation.log`, `invalid-bearer.log`, `epic9.log`, `epic9/` and `cleanup-verified.json`. Raw logs remain external and credentials are not recorded in these source records.

Focused successor checks at the c494/81ab candidate: AppHost and individual Shell.Tests Debug builds have **0 warnings/errors**; host checks **56/56**; helpers **186 Node + 71 Python**; dependency regressions **109/109**; four Debug/Release/source-mode evaluations **4/4**; TypeScript exit **0**; analyzer governance contract **1/1** after the factual inventory reconciliation below. The actual source-governance CLI still fails as described below. Logs/manifests are under `/tmp/fc-mtls-final-gad9h52g/`.

The preserved source-selection gap was reproduced without real state: `EventIdentityValidator.IsUlid` requested `Hexalith.Commons.UniqueIds, Version=3.128.3.0` against runtime `1.0.0.0`. Tenants' explicit EventStore Version leaked across source edges, and the SDK added transitive references after evaluation. `deps.local.props` plus `eng/FrontComposer.SourceReferences.targets` now remove that foreign Version only at the exact normalized Commons.UniqueIds Debug/source boundary, including SDK-created late edges, while chaining the caller's existing import and preserving Release/package/producer-mapping behavior. One coherent rebuild emits only UniqueIds 1.0.0. All **222 Hexalith assembly references across ten output directories match exact available identity/version**, and the stateless synthetic persistence stage completes; both final fresh real bootstraps also Completed. No dependency source/pin or package/SDK selection changed.

Canonical channel-token wiring uses the existing generated-token binder only in run mode for the existing channels. Local caller-policy trust domain is aligned to canonical localhost SPIFFE with the same existing caller/namespace/route/verb/default-action rules; API/BFF receivers remain explicitly deny by default. No new resource-binding grant was added or observed as necessary; authorized native queries succeeded with the existing configuration. Normal derived metadata indexes are awaited, not seeded. Disposable Tenants automatic bootstrap is explicitly suppressed; inherited bootstrap/build/import/source-mapping/isolation overrides fail before startup. Normal Keycloak lifetime, global watch settings and gateway-mode parity remain preserved; the approved API/BFF composition and full-mode canonical mTLS integration are recorded above.

Post-proof metadata-only reconciliation updates the existing `source-suppression-attributes` factual census **46→47** and adds only `FcOptimisticReconciler.cs`, whose external commit `5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d` already supplied the member-level CA1822 compatibility justification. Existing diagnostic IDs, disposition, owner, approvals/dates, source severity and the suppression itself are unchanged. The initial contract failure is retained; the corrected contract passes. `/tmp/fc-mtls-final-gad9h52g/post-proof-inventory-reconciliation.json` distinguishes this bookkeeping from the sealed successful runtime input hash.

**Remaining source-governance task: unchecked.** Actual `python3 eng/dependency_graph.py --root . validate --commit c49412ed567ac47e10a1cceec100d2c41505cf45` fails closed on Platform b742's untrusted nested `hexalith.agents` identity. Its umbrella declares 17 raw mappings, seven outside current global trust. [The exact unapplied two-file consumer/projection proposal](13-6-canonical-platform-provenance-amendment.md) preserves every raw depth-two edge, global unknown-identity refusal, identity-based trust, delayed active policy and no nested/source acquisition/execution. It requires an explicit GOV-1 contract amendment; no new projection primitive, broad trust, dropped edge or nested initialization was applied. Separately, read-only selected 81ab catalog comparison retains Localization and Collections.Immutable **10.0.14 vs policy 10.0.13**; no version alignment was performed.

All earlier failed attempts remain attributed to their actual inputs. In particular, `4ahlluh3` bootstrap 500 remains **unknown delivery**, and independent fresh `b5w77dcd` remains **Rejected / EventsStored / FileNotFoundException**, without redispatch on either state. `b4wehas5` later achieved a real Completed/fresh bootstrap and two passing browser cases, but retained its Admin assertion failure: it assumed a native Create tenant button role. The corrected test follows the selected ordinary read/refresh action's explicit accessible label and preserves every authorized-empty/focus/no-alert/stale/unauthorized guard; no write grant or dependency UI fix was added. Current `c2tg0uhi` is a separate fresh successful run. Earlier query_projection_missing, malformed diagnostic metadata request, token-reference display mismatch, process-inventory/cleanup refusals, source-identity drift, lifecycle loss and first SIGTERM fixture failures remain historical failures. The identity-loss actor/root cause remains unproven; the measured uniquely owned verification-only persistent Docker lifetime keeps fresh nonpersistent realms and exact cleanup. No ordinary lifetime/global-watch setting changed.

Preserve seven completed Owner-reported manual Pass records and waiver, WebKit paint evidence and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions. This full rehearsal qualifies the observed disposable configuration only; independent parent review and the remaining provenance/catalog gates are not completed by runtime success.

External publication dispositions (not assistant commits): `5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d` owns the separate optimistic reconciliation change; `417d4b495ec31347e7dfb74dc54aa0f493d6245f` captured the approved Platform metadata; `c49412ed567ac47e10a1cceec100d2c41505cf45` captured prior story preimages/records and the external Builds 81ab selection. All are preserved, as are earlier mixed external publications. The old 28d/Builds 3d verification is diagnostic only and is not promoted to this successor.


The prepared [combined bounded governance proposal](13-6-canonical-platform-provenance-amendment.md) also explicitly proposes, without applying, only the FrontComposer semantic expectations for `Microsoft.Extensions.Localization` and `System.Collections.Immutable` at `profiles.frontcomposer-catalog-v1.selected_catalog_required_packages`: **10.0.13→10.0.14**, matching already-selected Builds 81ab. This is separate from source-projection evidence, changes no actual package/catalog/SDK/Gitlink or other baseline, and remains subject to AD-12 delayed activation and agreement among collection/consumer validation/policy activation. Both current expectations remain 10.0.13; no gate is silently bypassed.


## Approved provenance implementation verified — 2026-10-28

The user-approved bounded amendment is implemented in the current working candidate. Earlier proposal-only statements above retain their historical attribution. The closed `consumer_source_projections` policy and engine bind Platform `b742b1b7a496ac72d6961ca6db5213d3df42d234`, its `.gitmodules`/all 17 raw mappings/two unchanged canonical C# hashes, the exact consumer Compile links and root selectors/configuration, and existing root Builds `81ab049208678bd6616828824729c0ddac02f7d1` Tooling source. The projection owner must remain evidence-only; only the two canonical source blobs can be materialized into the isolated consumer. Raw v1 graph/envelope shapes remain unchanged. Seven inventory-only identities remain globally untrusted, without resolution, acquisition, nested initialization, materialization or execution; unknown identities elsewhere and extra/indirect consumption refuse before candidate execution.

Collection, acquisition, affected-build validation and live handoff/release consumers enforce the same gate. Supplied depth-two rows and catalog evidence are recomputed against committed owner objects before accepting a projection report; a structurally valid, rehashed forged envelope cannot authenticate changed inventory. The committed configuration seal includes project/props/targets/proj/slnx inputs plus automatic `.user` imports and `.rsp` response files. Negative fixtures add a project `.csproj.user`, root/nested `Directory.Build.rsp` and `MSBuild.rsp` without changing the consumer project, and verify Git-only refusal before execution. The SDK's actual automatic import is confirmed at `/home/administrator/.dotnet/sdk/10.0.402/Microsoft.Common.CurrentVersion.targets:30`.

The only package-expectation changes are FrontComposer `Microsoft.Extensions.Localization` and `System.Collections.Immutable` **10.0.13→10.0.14**, matching selected 81ab. Global trust, every other policy value, actual package/catalog/SDK/Gitlinks and consumed runtime code are preserved. The adopted GOV-1 [spine](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md), its [memory log](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/.memlog.md) and [owning contract](../contracts/shared-catalog-dependency-governance-2026-07-19.md) record the approved closed mechanism and AD-2/AD-6/AD-10/AD-12 limits without claiming a new Architect sign-off or closing independent Product/Release gates.

Sequential checks pass: dependency engine **126/126**, dependency handoff **11/11**, release-evidence consumers **15/15**. This includes exact owner/hash/inventory/configuration drift, forged raw/catalog/affected proof, evidence-only scheduling, two-file materialization, globally unknown identities, acquisition boundaries, exact 14/wrong/old/missing semantic rows and delayed-policy tests. Logs are `/tmp/fc-mtls-final-gad9h52g/governance-full-tests-auto-inputs.log`, `governance-handoff-consumers-auto-inputs.log` and `governance-release-consumers-auto-inputs.log`; earlier failed fixture/test attempts are retained alongside them.

Real committed-object component diagnostics pass for canonical seals and raw inventory: **69 total edges / 17 Platform edges**, graph digest `9a9308069b9a82a7e8acba6a1980aeea069a499356b2fcc80b71d35fb66049a1`. Platform's unconsumed nested Builds `51a71cb1cfe860d5fc135eb3b8521fd80c413810` retains raw catalog SHA-256 `95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6`; actual helper source remains root 81ab. Missing trusted exact metadata still refuses; no inventory-only objects were acquired. Current working consumer bindings/configuration match approved seal `4c4ecad2dd317b30734b8db55cd60604ab8a481b26e429e57d16c8c2f227ff8b`, and both exact 14 expectations match the actual selected catalog. Evidence and reproducible read-only commands are `/tmp/fc-mtls-final-gad9h52g/governance-actual-object-diagnostics.json`, `governance-actual-raw-inventory.json`, `governance-consumer-configuration-diagnostic.json` and `verify-governance-actual-objects.py`.

These are **component/working-candidate diagnostics**, not active-policy or release acceptance. Strict explicit approved-policy validation of committed `c49412ed567ac47e10a1cceec100d2c41505cf45` correctly refuses its older committed consumer configuration. Committed-policy validation and active-base diff at c494 correctly retain the old policy's unknown-Agents refusal. The dirty configuration/policy cannot authorize itself: owner landing and a later change selecting that base are required by AD-12. Positive fixture acceptance proves the later-base path; no actual-root commit, synthetic release claim or policy activation was created. These expected diagnostics do not require broader approval or another runtime rehearsal.

The successful `/tmp/story13-6-isolated-readiness-c2tg0uhi` runtime remains attributed to its original source manifest. No consumed runtime code changed in this governance amendment and no runtime was repeated. All nine dependency revisions and 33/34 explicitly listed source files byte-match main; the pre-existing `HexalithTenantsSample.cs` CRLF main bytes versus LF stable/committed bytes have identical normalized content and a clean Git diff, and main bytes are preserved. Post-proof governance/ledger metadata is distinct from that manifest. Original baseline 5688, external publications/work, seven Owner-reported manual Pass records/waiver and independent gates remain preserved. Parent-managed full diff/task/matrix audit and independent Build review remain pending; status stays in-progress and implementation does not mark the parent's task boxes.


## Verification guard refinement after parent audit — 2026-10-28

The parent found a concrete identity gap after the successful runtime: the old runner hashed tracked diffs and explicit host/helper inputs but missed other nonignored untracked root source/configuration and ignored automatic MSBuild inputs. A real-Git fixture retained six pre-fix failures for an added Shell `.cs`/configuration and ignored `.csproj.user`/`.rsp`. The minimal guard now hashes relevant nonignored untracked root inputs, tracks automatic `.user`/`.rsp` diffs and refuses ignored automatic project/response configuration in the root and selected source repositories. Git bounds input enumeration and excludes generated/cache trees and nested reference checkouts. Refusal occurs before configuration and AppHost startup; a fresh identity recheck also runs immediately before the first owned start. Default gateway behavior is preserved.

The refined Python suite passes **73/73**, covering untracked add/edit/remove drift, ignored automatic inputs in root/selected source, startup ordering and ignored build/scratch exclusions. Evidence: `/tmp/fc-mtls-final-gad9h52g/runner-untracked-inputs-before-fix.log`, `runner-untracked-inputs-after-fix.log` and `python-tests-refined-input-guard.log`. Read-only `source_inputs` plus stability recheck accepts the actual stable candidate: `refined-runner-source-inputs.json`, `refined-runner-input-attribution.json` and reproducible `verify-refined-runner-inputs.py` under that same evidence root. No current-runner full runtime pass is claimed.

The original c2tg0uhi manifest and its full success remain tied to runner SHA-256 `12d57e83bb7cf59e7dfa37cfd8fed2193f719cc83ec27227967d35fcf34a40ad`; the refined runner is `9802261acdc6f89d8f57ba1d467d033fad4aa4f5cd7a69172351b99213f5b1bb`. All nine source revisions, native daprd and **27 listed product inputs** are unchanged; **33/34 listed files match the stable proof**, with only the runner changed. In main, **32/34** are byte-identical because the pre-existing Sample CRLF/LF exception also remains preserved and normalized-equal. The refined untracked map contains three existing source/configuration entries, each already explicitly hashed in the old manifest. Approved governance/guard/test changes alter the working-patch digest; they do not change AppHost/service code or binaries. The parent explicitly accepted focused/read-only verification for this identity/refusal-only correction and directed no new full rehearsal. Historical helper/runtime results retain their original attribution; the new guard has separate evidence. Parent audit/task/matrix and independent Build review remain pending.


## Root evidence authentication refinement after parent audit — 2026-10-28

The parent identified a concrete new-primitive boundary: rehashed supplied depth-one rows could omit the Platform selection or disagree with actual root gitlinks/mappings, and root Builds catalog evidence was not independently authenticated. Validation now recomputes supplied root direct rows from actual committed root `.gitmodules`/gitlinks **before any selected-owner/skip path**, and verifies raw root Builds catalog bytes/digest/null marker alongside the existing nested metadata checks. A legitimate authenticated older root without Platform still receives no projection authority. These changes affect governance inspection only; AppHost/service/runtime code and binaries remain unchanged.

The final engine suite passes **128/128**, including structurally valid rehashed omitted-owner, actual-root-target/mapping and root-catalog forgeries through affected-build validation with no materialization/execution, plus a legitimate older-root case. Final consumer reruns pass handoff **11/11** and release-evidence **15/15**. Logs: `/tmp/fc-mtls-final-gad9h52g/governance-full-tests-root-evidence.log`, `governance-handoff-consumers-root-evidence.log`, `governance-release-consumers-root-evidence.log`. The pre-fix attempts/fixture failures remain retained, without relabelling retries as an original clean pass. Real-object component diagnostics are refreshed in `governance-actual-object-diagnostics.json` and `governance-actual-object-diagnostics-root-evidence.log`: full raw 69/all 17 and both root 81ab/nested 51 catalog evidence remain intact; current working seals/14 expectations pass their diagnostic components. Strict committed c494/old-active-base refusals retain their previously documented expected AD-12 limitations; no actual-root commit or candidate self-authorization is created.

The refined identity/refusal guard separately retains Python **73/73** and read-only actual-candidate acceptance. c2tg0uhi remains the earlier runner's successful development runtime proof, not a new-runner full-pass claim. No full runtime was repeated. Guarded copyback, exact hashes and refreshed review patches are external evidence under `/tmp/fc-mtls-final-gad9h52g/`; parent task/matrix audit, independent Build review and independent release/manual gates remain the parent's responsibility.


## Independent Build review and final parent verification — 2026-10-28

All three independent review layers completed against the full original-baseline diff before triage. All **16 findings** have individual evidence/verdict rows in the bounded implementation spec. Seven distinct patch entries are resolved: SDK/NuGet configuration sealing, NuGet filename identity, inherited build overrides, early full-mode procfs prerequisite, bootstrap failure diagnostics through retained status/membership, bounded HTTP error bodies, and a positive affected-build materialization integration regression. The materialization bypass mutation now fails that regression. Three distinct pre-existing optimistic-reconcile follow-ups are appended to `deferred-work.md`; the four externally published reconciliation files remain byte-identical to HEAD. Two claims were refuted against the actual Release build/restore entrypoints; four uncommon/low-value findings were rejected under the workflow's low-impact complexity rule. No intent/spec loopback was needed.

The same implementation agent made the five-file guarded copyback; the parent read its entire 401-line delta and independently checked all five after-hashes. The expanded working-consumer seal is `68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca`, adding only unchanged tracked `global.json` and `nuget.config`. Spine/FC-DEP-1/memory/GOV-1 record this closure consistently. No SDK/package/catalog/Gitlink or canonical source/owner/inventory/selector value changed.

Final parent checks all pass: engine **130/130**, handoff **11/11**, release-evidence **15/15**, helper evidence **186 Node + 78 Python**, dependency-mode suite **13/13**, TypeScript, individual AppHost and Shell.Tests Debug builds (**zero warnings/errors**), and AppHost selection **56/56** with no skips. Story-artifact and diff checks pass. Exact argv, cwd, exit codes and logs are `/tmp/fc-parent-13-6-final-verification/results.json`; corrected per-story validation is rerun after completion metadata.

Independent actual-object diagnostics confirm the expanded working seal, **69 total / 17 Platform raw edges**, exact canonical blobs, root Builds and both approved catalog expectations. Expected pre-activation refusals remain: explicit new-policy diagnosis rejects the old committed consumer configuration; committed-policy/active-base diagnoses reject the globally untrusted Agents identity. These diagnostics confer no current strict CI/release acceptance. AD-12 requires the owner landing and a later base; no dirty candidate self-authority is granted.

The original successful c2tg0uhi proof remains **10 healthy applications / 9 native sidecars, 3/3 authenticated UI cases, 11 boundary negatives, four unchanged invalid-bearer checks and one unchanged Epic 9 live proof**, with verified owned cleanup. The current runner hash is `c87d29b43156650ff24e4b9c7926c864577d86dd66be5946d243d5c324da8a4f`; the original runtime used `12d57e83bb7cf59e7dfa37cfd8fed2193f719cc83ec27227967d35fcf34a40ad`. Read-only current-guard acceptance/recheck passes with all nine source revisions/native runtime and 33/34 stable listed files unchanged; only the runner differs. Main preserves its previously documented normalized-equivalent Sample line-ending exception. No consumed AppHost/service code changed and no new full rehearsal is claimed. New input/refusal/error-diagnostic behavior has its own executed tests; original runtime evidence is preserved at its exact inputs.

The bounded build has completed parent audit and independent review. Parent Story 13.6/Epic 13/sprint remain **in-progress** because WebKit system paint and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independent. Seven Owner-reported manual Pass records/waiver and all historical failures remain preserved. No current remote CI, commit, push or publication is claimed.

## Parties catalog inheritance correction — 2026-10-28

The approved bounded correction is implemented locally: Builds `94d38129932f750a46adb7a0f6bb0b25b3415b23` descends directly from Parties-selected `abc0e1be8069d1c7a567515a13bb015adc407536` and adds only the Contracts.UI family row. Parties `c4fae6120af10b9f139b2e0a564988ff331aebd4` removes the exact shim and advances its nested Builds selector as committed metadata. Root `c3f217ee7216f998875b23a4d6b8175280e8a86a` changes only Parties selection and its necessary root selector seal. Root Builds stays `81ab049208678bd6616828824729c0ddac02f7d1`; every pre-existing catalog version, SDK, other root selection, product source and Platform inventory/trust boundary is preserved.

Fresh verification passes: exact isolated Parties standalone Release/NuGet restore/build (zero warnings/errors), all three Contracts.UI consumers at unchanged 4.7.1, current committed graph (eight selectors / 69 raw edges / all 17 Platform edges), engine **130/130**, handoff **11/11**, release consumers **15/15**, dependency modes **13/13**, final Debug AppHost/Shell builds (zero warnings/errors), AppHost tests **56/56** and infrastructure governance **20/20**, with no test skips. The first missing-Tooling-assets build, subsequent file-sharing failure, initial Shell assembly-version warnings and old-base selector-seal refusal are retained with exact argv/cwd/exits in `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv/results.jsonl` and its logs. Focused restores and serialized builds resolved the environmental checks without changing source, configuration or versions.

The [bounded catalog spec](spec-13-6-correct-parties-shared-catalog-inheritance.md) holds full revisions, commands, seals and limits. The parent still owns independent audit/review and the later activation record/committed-base transition; candidate policy has not been substituted for old event-base authority. No new full topology or hosted proof was run, and historical runtime evidence remains tied to its original inputs. Existing frozen acceptance/baseline, Owner-reported manual Pass records/waiver, historical failures and unperformed accessibility/platform/owner/release gates remain intact. Parent Story 13.6 remains **in-progress**.

Additional owning integration check: the Builds central catalog (308 entries), authoritative catalog (50 identities/three shared versions), consumer authority (22 projects) and exception alignment (16 exceptions) pass. The deterministic package audit **fails with two preserved errors**: catalogSha256 mismatch and missing Contracts.UI audit evidence. Its normal revision-bound family audit refresh remains a required separately reviewed correction; no audit generation/feed discovery or extra metadata successor was applied. The functional results above stand, while current owner integration readiness, final activation and bounded acceptance remain open. Exact output: `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv/builds-version-audit.log`.

## Derived audit metadata closure — 2026-10-28

The initial stale-audit checkpoint above is resolved through the documented FrontComposer-only incremental refresh: **307 existing selected versions, 302 unrelated package rows and 150 unrelated family decisions remain identical**. Final local selections are Builds `326af3d248974b5f3849ff667e97a5d61e3e3f30`, Parties `492a8cc1417c625cdfb7fd4c946686ef88adab9d`, and root `6a41a075e1f0512b8acaa80368974b92c4bbe1c3`. The derived commits contain audit metadata, the dependent nested/root selectors and necessary selector seal only; root Builds 81ab, raw catalog bytes, all actual versions/SDKs/product source and Platform source/inventory/trust stay unchanged. All full messages passed owning pinned commitlint before use.

All final real Builds owner validators pass (catalog 308; authoritative 50 identities/three family versions; Builds-self consumers 22; exceptions 16; audit 308 packages/151 families/one source). The documented audit generator/validator fixtures pass **115/115** and **103/103**; none remain pending. Final exact-root graph validation passes **eight selectors / 69 raw edges / 17 unchanged Platform edges**. Final exact Parties standalone Release/NuGet restore/build passes with zero warnings/errors, and all three UI consumers resolve unchanged Contracts.UI **4.7.1**. Infrastructure governance passes **20/20** against the final committed root. The earlier engine/source-mode/AppHost/Debug results remain attributed to their earlier functional selection; unchanged source/effective versions justify reuse without claiming a new full runtime or hosted execution.

The separately requested generic **Parties** authority check retains a pre-existing limit: its tracked wrapper overrides `HexalithEventStoreVersion` at the unchanged user-selected 3.128.3. The first external-catalog import-path refusal and the corrected single-override refusal are both retained. No extra source edit, policy relaxation, full Parties authority/CI acceptance, new topology, push or publication is claimed. The root amendment still correctly refuses an intermediate old-base transition; the parent owns the later activation record and committed-base transition before formal review. Full exact argv/revisions/limits are in the [bounded catalog spec](spec-13-6-correct-parties-shared-catalog-inheritance.md) and external scratch `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv`. Parent Story 13.6 remains **in-progress**, preserving original acceptance, manual waiver, historical failures and independent accessibility/platform/owner/release gates.


## Local catalog activation verified — 2026-10-28

The parent independently audited the full catalog/audit/Parties/root diff and verified the approved scope. Exact activation `71c5ac33d3e75e2bfa688d22149dafb34db85497` descends from amended base `6a41a075e1f0512b8acaa80368974b92c4bbe1c3`. Current-object graph validation and the actual committed-base push model both pass with **69 edges / eight selectors**, zero selector changes and no candidate-policy substitution. Fresh parent projection tests **20/20** and AppHost/infrastructure tests **76/76** pass without skips. The final selected Parties standalone and Builds owner checks retain their exact previously recorded inputs; no full runtime or hosted proof was repeated.

Independent formal review is next for the [bounded correction](spec-13-6-correct-parties-shared-catalog-inheritance.md). The separate pre-existing Parties EventStore override refusal, WebKit system paint and independent owner/UX/release gates remain open. Primary Story 13.6/sprint remain **in-progress**; original frozen acceptance, baseline, manual reports/waiver and historical failures are preserved. Local tracking origin/main was observed at c3f217ee during audit; this workflow did not push or query hosted CI.


## Independent catalog review: fallback amendment pending — 2026-10-28

All three independent review layers completed. The package fallback with uninitialized nested Builds still reads sibling root Builds 81ab and reproduces NU1010 for UI/AdminPortal/ConsumerPortal; the isolated selected-catalog proof does not cover that path. The [bounded correction](spec-13-6-correct-parties-shared-catalog-inheritance.md) remains in-review, with every finding dispositioned and an unapplied root-catalog extension prepared. The owner question is pending because the captured frozen scope keeps root Builds 81ab. Existing passing proofs retain their exact inputs; the regression is not deferred or reported as fixed. Primary Story 13.6/sprint remain in-progress, preserving original acceptance, manual waiver and independent platform/owner gates.
