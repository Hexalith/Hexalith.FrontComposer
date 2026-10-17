# Story 13.6 parent Build review and final verification

Readiness remains open. Final parent browser verification is **529 passed, 12 failed, 2 skipped / 543**. The configured Debug Shell lane passes **3693/3693** with its five existing exclusions. All nonvisual tests pass on their final attempt; the 12 Firefox/WebKit visual baseline cases remain failed against unchanged committed images. Human and platform acceptance remains open.

## Candidate and provenance

Final immutable source: `sha256:8a2058c7769a7761d377906fb32f0990c962bd85e2bfa20125e3e6573d5cdd69` on `main`, based on `0129bd39095892283371f97836ade5768fef3809`. [source.json](source.json) and the [final source patch](source.patch) bind all changed source/configuration files, exact root submodule heads and preserved dirty diffs, frozen intent and all 24 unchanged approved image bytes. Documentation and evidence are outside the source aggregate to avoid circular hashing. Final run: `gap-root-review-9c934194b041`. [Execution record](execution.json) retains exact argv, environment, durations, exits and raw log hashes. [Runtime focus script proof](runtime-focus.json) binds served production JS. The [cleanup record](cleanup.json) confirms the task-owned Test AppHost stopped. [Final evidence hashes](evidence-sha256.json) bind the dated packet. [Parent audit](parent-audit.json) checks the final candidate and evidence; [check reuse](verification-reuse.json) proves unchanged inputs for prior passing non-browser lanes.

The [initial implementation report](../../report.md) and [initial source](../../candidate-source.json) remain historical evidence. The earlier parent run was interrupted after it demonstrated unsupported valid layouts and a layered-shadow false pass; [interruption](../first-run-interruption.json), [outcomes](../first-run-verification.json), [source](../first-run-source.json), [retained bytes](../first-run-retention.json) and [direct shadow reproduction](../p02-repro.json) preserve that attempt. Its partial results are not used as a complete browser pass. A [second parent attempt](../second-run-verification.json) demonstrated offscreen explanation sampling with retained external focus; [interruption](../second-run-interruption.json), [source](../second-run-source.json) and [retention](../second-run-retention.json) preserve that capture. The final test uses explicitly labeled focus-before-root-scroll inspections in the same authoritative attempt. A [third complete browser run](../report.md) passed browser assertions but failed four required inventory cells per engine; its full packet remains separate. G18 makes those two demonstrated captures visible and explicitly asserts their required icons and boundaries; its [12-case proof](../g18-review-attempts.json) and [90-state audit](../g18-evidence-audit.json) are separate from this final complete run. [All intermediate paint diagnostics](../paint-review-attempts.json) and [G17 failed/passing fixture captures](../g17-review-attempts.json) are retained losslessly.

The [fourth complete attempt](../final/report.md) retains two Firefox timeouts and the debounced WebKit palette race. G19 awaits the exact filtered results before measuring selection; its [six-case proof with traces](../g19-review-attempts.json) and [selected-cue audit](../g19-evidence-audit.json) remain separate from this full run. This complete run applies the existing CI policy of two retries with unchanged test/action timeouts and retains every attempt.

## Final verification

| Engine | Passed | Failed | Skipped |
|---|---:|---:|---:|
| chromium | 181 | 0 | 0 |
| firefox | 174 | 6 | 1 |
| webkit | 174 | 6 | 1 |

No tests required a successful retry.

All twelve final failures are screenshot assertions. [Visual diagnostics](visual-diagnostics.json) retain earlier navigation/layout errors and final screenshot assertions separately.

[Complete browser outcomes](browser-verification.json), [authoritative raw JSON](playwright.json), [JUnit](junit.xml), and [HTML report](browser-report/index.html) retain all test identities, outcomes and retry attempts. No screenshot was regenerated, skipped or reclassified to improve the result.

| Check | Result | Evidence |
|---|---|---|
| Configured Debug Shell | 3693/3693 passed, no failures/skips; five standard exclusions unchanged; final configuration consumers rerun 85/85 | [parent raw log](../../raw/root-shell.log), [execution record](../first-run-verification.json), [XML retention](../first-shell-xml.json), [final configuration consumers](../shell-ci-governance.json) |
| Node artifact authority regressions | 12/12 passed | [parent raw log](../../raw/root-node.log) |
| SourceTools emitter class | 68/68 passed; relevant source inputs unchanged | [raw log](../../raw/sourcetools-handoff.log), [reuse proof](verification-reuse.json) |
| CI governance | 10/10 passed on final workflow | [parent raw log](../../raw/root-third-ci-governance.log), [execution record](../third-run-verification.json) |
| TypeScript, Debug build, visual policy, whitespace | Passed | [current execution](execution.json), [Debug build](../../raw/root-shell-build.log) |
| All 24 approved image bytes and frozen intent | Unchanged | [source manifest](source.json), [parent audit](parent-audit.json) |

The final browser run follows the last review patch. Only browser helpers/specifications and accessibility Test-run configuration changed after the parent Shell/Node/build passes; unchanged inputs are explicitly verified, and the Shell governance consumers of changed configuration pass a fresh 85/85 rerun. Unaffected suites are reused.

## Evidence authority and bounds

[State ledger](state-ledger.json) binds each state to engine/run/test/final retry; [validator audit](state-validation.json) uses only the final successful result's capability attachment. It does not pool fragments or select the first same-basename file.

| Engine | State rows | Strict validator errors | Capability-attributed measurement errors | Failed visual outcome errors |
|---|---:|---:|---:|---:|
| chromium | 116 | 0 | 0 | 0 |
| firefox | 116 | 6 | 0 | 6 |
| webkit | 116 | 454 | 0 | 6 |

WebKit's media-only forced-color capability remains uncovered actual palette coverage. The strict errors remain recorded and cannot be treated as successful actual forced-color evidence. Capability-aware attribution removes only that unsupported-palette claim; failed screenshot outcomes still reject Firefox/WebKit evidence.

[Artifact gate results](artifact-validation.json) record every raw and curated engine invocation with the unchanged 50MiB root limit. Raw trace/video output is retained losslessly and may exceed that limit. [Curation map](curation.json) retains every owned JSON/image and all report outcomes in bounded engine views; only retained attachment paths are relocated. The Chromium bounded view passes. Firefox/WebKit bounded views still reject their failed visual outcomes. The original HTML index remains intact; open the extracted original HTML report for trace/video links using the [lossless final archive parts](final-run-retention.json) and recorded `/tmp` paths. No failed diagnostic is discarded to obtain an artifact pass. [Test host configuration proof](testhost.json) records the existing 30000ms recovery-notice option used for this inspection; the production default remains 3000ms. Playwright and both CI accessibility jobs carry the same explicit fixture setting.

## Review disposition

All three independent layers completed before triage. The spec records all 33 individual findings, then 19 implementation patch groups. Patches cover SVG stroke envelopes; generated paint filters/geometry/large text; sibling backgrounds and clipping; concealed, permanent and layered focus shadows; pseudo background/margins; transformed descendants; authoritative capability retry selection; report-level errors; operator SVG focus; independently checked localized warning title/body; raw log retention; required-label inline margin; and visible/top-layer paint ownership.

The shadow helper supports a conservative, explicitly labeled black-layer composite bound for demonstrated Fluent focus paint. It records contributing layers and color intervals and preserves the 3:1 minimum; unsupported composition fails closed. Native top-layer exemptions apply only to lower page paint with an opaque containing backdrop. Same-plane and overlapping other-plane paint still requires evidence. Text samples are clipped to scrollports, and real feedback surfaces are scrolled into view before measurement.

The unused User Agent Control/Equivalent exception concerns were rejected as low-impact under the workflow rule. The pre-existing malformed-PNG validator gap is deferred in the append-only ledger; all current PNG files in the dated views were independently decoded; archived diagnostics are byte-hash verified. [Spec triage and execution](../../../../../spec-13-6-close-automated-verification-gaps.md) records each verdict and route.

## Remaining gates

The 12 unchanged Firefox/WebKit visual baseline cases, Windows execution, actual WebKit forced-palette coverage, manual NVDA/JAWS/VoiceOver and heard speech, actual 400% browser zoom, real tablet/phone observations, OI-16, G-4, FLUENT-APP-1, Product approval and live EventStore fresh-row acceptance remain open. Test fixtures do not close those gates. The story remains `in-review`; sprint tracking remains `in-progress`. No staging, commit, push, dependency update or human approval occurred.
