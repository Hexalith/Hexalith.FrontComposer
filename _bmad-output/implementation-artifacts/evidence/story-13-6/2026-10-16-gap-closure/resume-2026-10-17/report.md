# Story 13.6 resumed Build verification — 2026-10-17

Story readiness remains **open**. Current complete browser run: **567 passed, 12 failed, 1 flaky, 2 skipped / 582**. Current configured direct Debug Shell lane: **3692 passed / 3 failed / 3695**, no skips. Neither gate is promoted to a pass.

## Candidate

Current source identity: `sha256:d9728b3ffb86041c9e49906a72b374f1c65f478847abc48cf6df6a142ad2677b`; root `8a2164e64566c9eb89c633652b34e1a12d84f446` on `main`. [Source manifest](source.json) seals 27 source/configuration inputs, root-declared submodule heads and dirty diffs, all 24 approved image hashes, frozen intent, the executed Debug binaries and existing restore assets. [Source patch](source.patch) contains this continuation's three implementation/test file changes. All three frozen 13.6 intent blocks, image bytes and submodule working trees are preserved.

**Dependency limit:** the serialized Debug build used `--no-restore` and existing Fluent **5.0.0** assets. The Builds catalog now declares **5.0.1**, while the conformance gate requires **5.0.0**. The separate central-catalog gate also rejects Localization **10.0.13** against its expected **10.0.12**. These results are conditional on the sealed existing assets; no clean-restore compatibility, dependency upgrade or catalog-policy approval is claimed.

## Change and review

All three independent review layers returned before triage. The selected spec records 17 individual verdicts: eleven patch groups H01–H11, two carried low findings with unchanged rejection, and one new deferred effective-AppHost-configuration harness. The implementation fixes covered/offset shadows, oversized flow obstructions, clipped pseudo borders, overflowing generated text, descendant/cross-root backgrounds, text-stroke composition, degenerate SVG fills/dashes and transformed pseudo containing blocks. Runtime warning tests now cover null titles and an available entity label in both supported cultures.

Real-surface validation additionally distinguishes clipped grid content and Fluent-owned transparent labels/carets. WebKit slot hit testing follows assigned elements so a slot cannot hide an oversized opaque projected child. Thirteen new regression cases run on each engine; the final focused runs pass **46/46** (39 new regression executions plus seven real WebKit journeys). The earlier focused scroll/reflow run passed **48/48** on its separately attributed source. Strict contrast, target and focus thresholds are unchanged.

## Executed checks

| Check | Result | Evidence |
|---|---|---|
| Serialized Debug Shell project build | Passed, 0 warnings/errors; existing restore assets | [build.log](build.log.txt) |
| Generated renderer class | 70/70 passed, including the two new warning payload rows | [wrapper.xml](wrapper.xml), [wrapper.log](wrapper.log.txt) |
| Emitter class | 68/68 passed | [emitter.xml](emitter.xml) |
| Configured direct xUnit Shell lane | 3692 passed, 3 failed / 3695; five standard exclusions retained | [shell.xml](shell.xml), [shell.log](shell.log.txt), [failure summary](dotnet-outcomes.json) |
| Focused cancellation fallback | 4/4 passed; does not replace the full-lane failure | [cancellation.xml](cancellation.xml) |
| TypeScript | Passed on final helper/spec | [typecheck.log](typecheck.log.txt) |
| Artifact authority regressions | 12/12 passed | [node.log](node.log.txt) |
| Visual baseline governance | Passed; no image update | [visual-policy.log](visual-policy.log.txt) |
| Focused slot/current regression checks | WebKit 20/20; Chromium/Firefox 26/26 | [slot.log](slot.log.txt), [slot-others.log](slot-others.log.txt) |
| Complete three-engine browser lane | 567 passed, 12 failed, 1 flaky, 2 skipped / 582 | [raw JSON](playwright.json), [JUnit](junit.xml), [browser.log](browser.log.txt), [outcomes](browser-outcomes.json) |

The project-level MTP Shell attempt recorded the Fluent failure but did not finish; it was interrupted and replaced by the complete direct xUnit fallback. [Its log](shell-project-interrupted.log.txt) remains separate. The other full-lane Shell failures are `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` and `UncooperativeAcceptedRecoveryReleasesCleanupAfterFiniteBoundAndIgnoresLateEvidence(duringRetry: True, lateFault: False)`. The focused cancellation pass does not erase that observed failure.

## Browser and state authority

Run `story-13-6-resume-final4-20261017` uses the configured two-retry CI policy with unchanged timeouts, three workers and the Debug Test AppHost. Results are not pooled across attempts or engines. The full failed pre-slot run remains separate: **556 passed / 20 failed / 1 flaky / 2 skipped** on source `6be6151b679567814bced17d77b466a3dd669f3434b0ef334c630904bc288caf`. Seven false WebKit obstruction failures from that run are corrected and covered by the targeted slot proof; the current complete run above is authoritative. The two interrupted earlier runs are not completion evidence.

| Engine | Passed | Failed | Flaky | Skipped | Authoritative state rows | Capability-attributed errors |
|---|---:|---:|---:|---:|---:|---:|
| chromium | 194 | 0 | 0 | 0 | 116/116 | 0 |
| firefox | 186 | 6 | 1 | 1 | 116/116 | 6 |
| webkit | 187 | 6 | 0 | 1 | 116/116 | 6 |

[State validation](state-validation.json) binds each row to the successful final test attempt. Strict errors and capability-attributed errors remain separate. WebKit's actual forced-palette coverage remains uncovered, regardless of media-only assertions. Both raw and bounded artifact gates are retained in `artifact-raw-*.log.txt` and `artifact-*.log.txt`; curation preserves outcomes and only relocates retained JSON/image attachment paths. Each bounded artifact root keeps the 50MiB ceiling. The first curation correctly failed because four legacy axe/focus artifacts were omitted; its receipts remain in `curation-first/`. The corrected views retain those unchanged global legacy artifacts as well as engine-owned state records. Chromium now passes the bounded gate; Firefox/WebKit retain their six failed visual outcomes each. Raw oversized-root rejection remains recorded. Its HTML index does not contain omitted trace/video data; the original complete report is retained losslessly.

Earlier Chromium dark Settings failures remain recorded in the pre-slot canonical run and [instrumented diagnostic](settings-probe.log.txt). The instrumented run passed, so no causal production focus fix is inferred from it. Current nonvisual failures: **0**. See the current outcomes for exact identities and retry histories.

## Retention and remaining gates

[Raw artifact manifest](raw-artifacts.json) maps every retained browser test file to a SHA-256 blob in the numbered archive parts. Identical bytes are stored once; failed and interrupted runs have separate trees. `python3 restore.py <destination>` restores those trees with byte checks. The archive is 344,042,602 bytes in eleven parts of at most 32MiB, preserving 4,646 file entries as 1,546 unique blobs. The manifest records original roots for replay; original raw JSON keeps its original attachment paths. The audit script creates bounded engine views by relocating those paths. [Execution record](execution.json) records exact commands, exits and raw log hashes. [Final audit](audit.json) verifies source/image/intent identity, retained bytes, platform limits and cleanup.

Story and release accessibility readiness remain open for the failed configured gates, twelve unchanged Firefox/WebKit image comparisons, Windows execution, actual WebKit forced colors, manual NVDA/JAWS/VoiceOver speech, actual 400% zoom, real tablet/phone observations and the separate OI-16/G-4/Fluent/Product/live-backend owner gates. All manual checklist boxes stay open. No staging, commit, push, dependency upgrade, image regeneration or human approval occurred. Build halts at its unresolved-verification rule; status remains `in-review` and sprint tracking remains `in-progress`.
