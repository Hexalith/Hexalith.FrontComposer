# Story 13.6 automated verification stabilization — 2026-10-17

Readiness remains **open**. Current complete browser verification: **571 passed, 12 failed, 0 flaky, 2 skipped / 585**. Current configured direct Debug Shell lane: **3693 passed / 2 failed / 3695**, no skips. Passing subsets establish no story completion.

## Candidate and runtime

Current source identity: `sha256:babffe65f80705c4661aebcad2692ac688543f30dc6e3a002a091d3906e3b08b`; root `489f3ad4cbf09965c675d7bf2007c6a8b7a0861e` on `main`. [Source manifest](source.json) seals 30 source/configuration inputs, root-declared submodule heads and dirty diffs, all 24 approved image hashes, four frozen intent blocks, existing restore assets and Debug binary inventory. [Source patch](source.patch) contains the three implementation/test changes; production code is unchanged. The additional manifest inputs are the cleanup test, its existing uncooperative-content double and the production command client. The inherited SourceTools.Tests binary is historical inventory only; it was not run in this stabilization.

**Dependency limit:** the Debug build used `--no-restore` and existing Fluent **5.0.0** restore assets. The selected catalog asks for **5.0.1**; the conformance gate requires **5.0.0**, and the catalog gate rejects Localization **10.0.13** against **10.0.12**. No clean-restore compatibility, policy approval, dependency upgrade or Parties-owned override reconciliation is claimed. The prior five-mismatch/Parties diagnostic remains historical; the current canonical evaluator fails closed at the first Localization mismatch in [policy.json](policy.json).

The exact AppHost was started and inspected before edits using `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --no-build --non-interactive --format Json -- --environment Test` with Test environment variables and `EnableKeycloak=false`. `aspire wait counter-web --apphost ... --non-interactive` reported Healthy; [sanitized resource state](apphost.json) records the Test endpoint `http://localhost:5201`. EventStore and Admin were Finished/134; no full-topology health or live-backend acceptance is inferred.

After the Shell build, only `counter-web` was restarted through `aspire resource counter-web restart --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`, then awaited and inspected. [Startup binary proof](startup-binaries.json) binds the running Counter and Shell mapped-file inodes to the exact sealed DLL hashes. No build or implementation edit occurred during final browser verification. The initial browser continuation was interrupted because it still mapped replaced pre-build DLLs; its partial outcomes remain separately retained.

## Corrections and causal proof

The four cleanup scenarios now register cancellation observation after `ReadAttempted` and before advancing fake time. They await bounded accepted dispatch first, then await cancellation independently within five seconds. Identity, send counts, unfinished read, finite disposal and both late outcomes remain asserted; production acceptance/cleanup bounds and late-fault handling are unchanged.

The original Firefox dark-shell case reproduced **3 failures / 2 passes** over five repeats with **zero retries**. [Reproduction](reproduction.json), raw reports and traces retain all attempts. The real paint probe found the original footer range ending at y=891.5 and a Fluent shadow background starting at y=889.79999, a 1.70001px raw intersection. Its `.fc-shell-content` ancestor clips paint at y=864. The [current-runtime probe](real-paint-probe-final.json) reproduces the same clipping cause with another raw footer intersection. There is no demonstrated production overlap.

The shared contrast audit now clips competing backgrounds through the same composed-tree paint geometry used for sampled text. It leaves unresolved out-of-flow backgrounds subject to the overlap rejection. The new regression passes clipped backgrounds for hidden/auto/clip overflow, rejects a genuinely visible **1.7px** intersection, and rejects a fixed background escaping overflow. [The exact final fixture replayed against the original helper](exact-red/) fails at its clipped positive; the current helper passes it. The final exact-runtime Firefox lane passes **10/10**: five dark-shell journeys plus five regression executions, with retries disabled. Existing obstruction negatives run unchanged in the full lane. No contrast, focus, target, typography/density or measurement threshold changed; no blind delay was added.

## Executed checks

| Check | Result | Evidence |
| --- | --- | --- |
| Serialized Debug Shell.Tests build | Passed, 0 warnings/errors; existing restore assets | [build.log.txt](build.log.txt) |
| Rebuilt EventStoreClientTests | 69/69 passed, including all four cleanup rows | [eventstore.xml](eventstore.xml), [log](eventstore.log.txt) |
| Configured direct xUnit Shell lane | 3693 passed, 2 failed / 3695; five standard exclusions retained | [shell.xml](shell.xml), [log](shell.log.txt), [outcomes](dotnet-outcomes.json) |
| TypeScript | Passed | [typecheck.log.txt](typecheck.log.txt) |
| Artifact-authority regressions | 12/12 passed | [node.log.txt](node.log.txt) |
| Visual baseline governance | Passed; no image update | [visual-policy.log.txt](visual-policy.log.txt) |
| Exact-runtime Firefox five-repeat/no-retry lane | 10/10 passed | [five-final/playwright.json](five-final/playwright.json), [log](five-final.log.txt) |
| Complete three-spec, three-engine browser lane | 571 passed, 12 failed, 0 flaky, 2 skipped / 585 | [playwright.json](playwright.json), [JUnit](junit.xml), [log](browser-final.log.txt), [outcomes](browser-outcomes.json) |

The two Shell failures are `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` and `Fluent_package_versions_are_v5_only_and_icons_match_components`. The previously observed retry-cleanup scheduling failure is absent from both the rebuilt class and configured lane. Historical Settings, cleanup, visual and interrupted attempts remain retained; no diagnostic pass replaces a failed complete gate.

## Browser and artifact authority

Run `story-13-6-stabilize-final2-20261017` uses three workers, the existing two-retry CI policy and unchanged timeouts. Results remain separated by engine, run and attempt. [State validation](state-validation.json) attributes each authoritative row to its successful final attempt; missing/failed rows cannot borrow earlier evidence.

| Engine | Passed | Failed | Flaky | Skipped | Authoritative states | Capability-attributed errors |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| chromium | 195 | 0 | 0 | 0 | 116/116 | 0 |
| firefox | 188 | 6 | 0 | 1 | 116/116 | 6 |
| webkit | 188 | 6 | 0 | 1 | 116/116 | 6 |

Strict state errors, capability-attributed errors and raw artifact failures remain separate. WebKit actual forced-palette coverage is still uncovered. The existing validators ran against raw roots and bounded engine views; curation relocates retained JSON/image paths without changing any outcome. Bounded roots retain the 50MiB ceiling and required global axe/focus artifacts. Chromium's bounded gate can pass while Firefox/WebKit retain failed visual outcomes; none of these subset receipts establish overall completion.

## Retention and remaining gates

[Raw artifact manifest](raw-artifacts.json) preserves 4406 file entries as 1046 unique SHA-256 blobs in 5 archive parts, each at most 32MiB. `python3 restore.py <destination>` restores every tree with byte verification. Original failed reproductions, both interrupted runs, pre-restart diagnostics, the exact-runtime runs and bounded views remain separate. Raw report paths retain their original roots; [audit.mjs](audit.mjs) records bounded attachment relocation. [Execution](execution.json) records exact commands, exits and log hashes; [final audit](audit.json) verifies source/image/intent identity, retained bytes, platform limits and cleanup.

All manual boxes remain open. Windows, actual WebKit forced colors, NVDA/JAWS/VoiceOver heard speech, real 400% zoom, physical tablet/phone observations and OI-16/G-4/Fluent/Product/live-backend owner decisions remain incomplete. Prior review decisions are preserved; new human or platform approval is not inferred. The configured Shell and visual failures remain blockers. No staging, commit, push, dependency/policy/submodule change or approved-image regeneration occurred.
