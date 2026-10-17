# Story 13.6 current Build review evidence — 2026-10-17

Readiness is **open**. The original story remains **in-review**, and its sprint entry remains **review**. This packet binds source `sha256:0bfddb4016c3bfe00e84ae88ca698b3a63c373b1ac2be5048577cdae0ee6b77a` on `main` at `008736825376e6e2b419cb8d3b81067a8f9cc3b1`; original spec baseline `3c8679628ebdb8995afc09f712a04fcde623695c` is preserved. [Source identity and recipe](source.json), [runtime/served modules and scoped CSS](current-startup.json), [preservation audit](audit.json), [archive integrity](archive-integrity.json).

## Changes and review disposition

Both independent three-layer Build reviews returned before triage. Their 28 individual verdicts and grouped tasks are retained in the [original story spec](../../../../spec-13-6-verify-responsive-and-assistive-accessibility.md). All 27 medium findings received concrete corrections. The low generic undeclared-listener discovery proposal is rejected with its current-consumer and portability rationale; no listener registry or browser-specific discovery hook was introduced.

The implemented paths now follow composed/shadow text and painted clips; measure actual reachable targets and competing paint; preserve supported native paint while failing unsupported composition; recover missing detail to the affected physical grid with query-safe cancellation and a single shared fallback owner; and observe later-row/shadow changes with separately acknowledged metrics, bounded retries, disposal and quiescent multiple grids. The real generated two-row journey grows only its second row and checks shared physical height and ItemSize, density restoration and focus.

Parent complete verification additionally exposed native layout defects: zero-field wrapped feedback overlapped the exterior ring; narrow lifecycle actions displaced Dismiss beyond paint containment; the fixed-width theme button let its caret cover the ring; and the 32px circular account hit area could not contain a 24px square. Existing Fluent spacing, native multiline grid areas at the 480px breakpoint, natural theme width with its 48px minimum, and native AvatarSize.Size36 correct those cases. Direct tests use real click activation and contained glyph/ring/hit-square assertions. No SVG caret exemption, reduced threshold, approved image update, dependency/policy update, new public product API or frozen-intent change was made.

The original type-state contrast journey now waits for actual interactivity, explicitly selects the requested theme and settles paint. [Read-only causal evidence](review6-readonly-diagnosis.json) reproduced detached-heading sampling before readiness and proved the native 36px avatar preserves the 48px desktop header plus the 24px pointer square; the rejected 40px intermediate remains retained.

Competing black blur layers are bounded per text fragment. The exterior-only cap is an explicit conservative inference from the [CSS shadow blur requirement](https://www.w3.org/TR/css-backgrounds-3/#shadow-blur), with a full-pixel footprint, outward quantization and composition guards; source-intersecting fragments retain full alpha. Disjoint-line and near-threshold/unsupported negatives remain covered. Pixel comparison diagnostics establish the original false-failure cause and do not substitute for current passing assertions.

## Current verification

- [No-restore Debug build](review6-build-command.json): passed, zero warnings/errors.
- [Configured direct Shell command and binary hashes](current-configured-shell-command.json): 3695 passed / 2 failed / 3697, no functional failures. Both failures are the unchanged Fluent catalog 5.0.1 vs validated/restored 5.0.0 and Localization 10.0.13 vs policy 10.0.12 checks. Existing restore assets remain pinned in source identity; no clean-restore compatibility is claimed.
- [Current basic checks and unchanged-input reuse](current-basic-checks.json): typecheck and whitespace pass. The unchanged Node evidence suite retains 12/12 passes, visual governance retains its pass with all 24 PNG bytes unchanged, and the [two affected generator methods](current-generator-command.json) pass against the current pinned test/generator assemblies. The current full Shell run also includes all 34 banner tests. The preceding repository-wide architecture failure caused by loose historical C# evidence copies is retained in `review6-configured-shell.xml`; [the archive correction](archive-scan-correction.json) preserves those exact 52 source inputs without adding duplicate C# scan inputs.
- [Complete zero-retry browser command](browser-command.json): 705 passed / 18 failed / 3 skipped / 0 flaky / 726. There are 0 nonvisual failures and 18 failed ordinary image comparisons. The final focused all-engine type/header/fresh-row selection passed 18/18; complete outcomes remain authoritative.

| Engine | Passed | Failed | Skipped | Current state rows | Strict / capability-attributed errors | Bounded validator exit |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| chromium | 236 | 6 | 0 | 116 | 6 / 6 | 1 |
| firefox | 235 | 6 | 1 | 116 | 6 / 6 | 1 |
| webkit | 234 | 6 | 2 | 116 | 454 / 6 | 1 |

[State validation](verification/state-validation.json) binds every retained row to the successful final test attempt and exact run ID. [Gate status](verification/gate-status.json) preserves failed comparisons and actual-palette omissions. The six current Chromium comparisons each report 424 differing pixels; [exact RGB localization](current-chromium-visual-localization.json) confines every difference to header coordinates x1071–1260/y6–42, with unchanged full-page dimensions. Their required native-control changes still need visual approval; no baseline or threshold is changed. The twelve Firefox/WebKit comparisons retain their existing broader differences. Raw artifact roots exceed the existing 50MiB ceiling and fail that gate. Bounded per-engine JSON/PNG views retain all required legacy/state evidence and link to the complete raw HTML report; their results do not erase raw failures or establish overall readiness. WebKit media-rule execution does not establish its 116 missing actual forced-palette observations.

## Retained attempts

Historical packets remain intact. [Interrupted history](interrupted-browser-history.json) retains all earlier incomplete candidates. The [completed preceding candidate](superseded-complete-review3/history.json) records 686 passed / 22 failed / 3 skipped / 0 flaky, including the ten diagnosed nonvisual failures. Its exact helper/spec/CSS, complete report, raw failures and separate audit are preserved. The intermediate focused run before the avatar correction retains 75 passed / 3 failed; the next focused run retains 78 passed. Its subsequent complete candidate retains 699 passed / 24 failed / 3 skipped, including six readiness/header failures; all 52 source inputs are snapshotted in `superseded-complete-review4/source-snapshot/raw-artifacts.json`. The final 36px avatar and readiness correction has separate focused proof with 18 cases and complete candidate gate. Focused diagnostics do not replace current complete gates. Intermediate source/binary attribution and diagnostic roots remain explicit in the retained commands and archives.

All archive parts, combined archives, content-addressed blobs and logical paths were independently verified without extraction. [Evidence file hashes](evidence-sha256.json) seal this packet. Generated Pact handoffs were retained, then only this run's generated changes were restored. [Exact AppHost cleanup](cleanup.json) confirms the local host was stopped. Nothing was staged, committed, pushed or updated in submodules.

| Archive | File entries | Parts | Compressed bytes |
| --- | ---: | ---: | ---: |
| [c2-focused-raw](c2-focused-raw/raw-artifacts.json) | 1906 | 3 | 86,209,460 |
| [c3-focused-raw](c3-focused-raw/raw-artifacts.json) | 1747 | 6 | 171,790,398 |
| [c4-focused-raw](c4-focused-raw/raw-artifacts.json) | 123 | 1 | 26,557,549 |
| [current complete evidence](raw-artifacts.json) | 2781 | 3 | 75,389,390 |
| [resumed-focused-raw](resumed-focused-raw/raw-artifacts.json) | 540 | 1 | 19,190,073 |
| [superseded-browser-raw](superseded-browser-raw/raw-artifacts.json) | 1243 | 5 | 166,476,181 |
| [superseded-complete-review3](superseded-complete-review3/raw-artifacts.json) | 2527 | 3 | 75,184,534 |
| [superseded-complete-review4](superseded-complete-review4/raw-artifacts.json) | 2838 | 3 | 86,693,620 |
| [superseded-complete-review4/source-snapshot](superseded-complete-review4/source-snapshot/raw-artifacts.json) | 52 | 1 | 333,798 |
| [superseded-empty-paint](superseded-empty-paint/raw-artifacts.json) | 525 | 2 | 66,882,403 |

Replay the current complete report from repository root with the existing pack/restore tooling:

```sh
python3 _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/restore.py /tmp/story-13-6-restored --packet _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17
node _bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/audit.mjs --repo-root "$PWD" --evidence-root /tmp/story-13-6-restored/final-browser --output-root /tmp/story-13-6-replay
```

Audit exit 1 remains expected while recorded automated gates fail. Earlier archive packets use the same restore command with their own packet path.

## Open human, platform and owner gates

The [single current result and all 18 open manual checklist boxes](../../../../13-6-accessibility-result.md) remain authoritative. NVDA, JAWS, VoiceOver, actual 400% browser zoom, physical tablet/phone, human heard speech, Windows execution, OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and separate live EventStore fresh-row acceptance remain unperformed or unresolved. No human or owner approval is inferred from DOM, mock, geometry or automated evidence. The task is not marked done.
