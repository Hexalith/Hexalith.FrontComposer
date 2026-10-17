# Story 13.6 parent review verification — 2026-10-17

Readiness remains **open**. The corrected candidate executes **588 passed / 12 failed / 0 flaky / 3 skipped / 603** browser cases. Every executed nonvisual case passes on its first attempt. The configured direct Debug Shell lane retains **3693 passed / 2 failed / 3695**. Failed complete gates and missing human evidence prevent story completion.

## Candidate and configuration

Current identity: `sha256:e23d846e6e8f87f26f997866a8e82aa355e8ae89f1f94b67d4fa793add1db7b0`; root `489f3ad4cbf09965c675d7bf2007c6a8b7a0861e`, branch `main`. [Source manifest](source.json) binds 41 inputs, approved image bytes, frozen intent, restore assets, binaries and the explicit Test setting. [Loaded binary and environment proof](startup-binaries.json) verifies the actual Counter runtime child, rather than only its Aspire launcher.

The existing Playwright configuration specifies `ProjectionReconnectedNoticeDurationMs=30000`. The first parent launch omitted that override, leaving the 3000 ms production default; it retained two Firefox reconnect failures, one Chromium flake and incomplete cases. [Interrupted outcomes](../interrupted-outcomes.json) and the archive preserve that attempt. The final launch supplies the existing Test setting and verifies `30000` in the actual runtime environment. No source, production default, timeout, threshold, dependency or policy changed for this correction. All six final reconnect cases pass without retries. This observation covers this explicit setting; it does not replace the previously deferred general effective-configuration harness.

The Debug build and direct .NET lanes ran before AppHost startup. [Input reuse](input-reuse.json) establishes unchanged source, binary, restore and policy inputs for those lanes when the browser-only environment setting was corrected. Debug assets still use existing Fluent 5.0.0 restores; the catalog requests 5.0.1 and Localization 10.0.13 while policy expects 10.0.12. No clean-restore or dependency compatibility approval is claimed.

## Review corrections

All three independent review layers returned before triage. The verification-gap layer found no gaps. Thirteen individual findings were verified and grouped into ten patches; every finding is recorded in the [build spec](../../../../../../spec-13-6-stabilize-remaining-automated-verification.md). Geometry corrections reject unsupported scaled/expanded clips, preserve fractional edges and prevent boxless inline/contents ancestors from erasing visible paint. The original 1.7px clipped positive and real-overlap negative remain covered. [Outcome summary](outcome-summary.json) records all 21 current clipping executions: 20 pass and WebKit explicitly skips its unsupported overflow-clip-margin fixture.

Evidence scripts now provide portable replay, bounded HTML projections linked to the complete raw report, failing validator exit status, required-input checks before archive publication, and explicit integrity checks under optimized Python. [Focused receipts](../../review-fixes/report.md) retain five passing tool tests, including corrupt/missing-input negatives and portable causal replay. Historical raw artifacts and prior outcomes remain unchanged.

## Executed verification

| Check | Result | Evidence |
| --- | --- | --- |
| Serialized Debug build, existing assets | Passed, 0 warnings/errors | [Build](../build.log.txt) |
| Rebuilt EventStore client class | 69/69; all four initial/retry × late-success/fault cleanup rows pass | [Focused XML](../eventstore.xml) |
| Configured Shell lane, five standard exclusions | 3693 passed, 2 catalog/Fluent failures, no skips | [Shell XML](../shell.xml), [outcomes](../dotnet-outcomes.json) |
| TypeScript / artifact authority / visual governance | Passed / 12 passed / passed with unchanged images | [Typecheck](../typecheck.log.txt), [Node](../node.log.txt), [governance](../visual.log.txt) |
| Final Firefox five repeats, no retries | 10/10: five dark-shell journeys and five clipping fixtures | [Report](five/playwright.json), [log](firefox.log.txt) |
| Three accessibility specs, three engines | 588 passed, 12 visual failures, 0 flaky, 3 skipped | [Report](playwright.json), [JUnit](junit.xml), [log](browser.log.txt) |

| Engine | Passed | Failed | Flaky | Skipped | Authoritative state rows |
| --- | ---: | ---: | ---: | ---: | ---: |
| Chromium | 201 | 0 | 0 | 0 | 116/116 |
| Firefox | 194 | 6 | 0 | 1 | 116/116 |
| WebKit | 193 | 6 | 0 | 2 | 116/116 |

All twelve failures are unchanged Firefox/WebKit visual comparisons; every configured retry is retained. Two skips are existing Fluent-root hook capability cases; the third is WebKit's unsupported overflow-clip-margin negative. No skip supplies state authority or palette coverage. The final Firefox repeat lane and all executed nonvisual cases pass without retries.

[State validation](state-validation.json) and [gate status](gate-status.json) preserve raw, strict, capability-attributed and bounded outcomes separately. Chromium's bounded artifact gate passes. Firefox/WebKit each retain six visual errors; WebKit additionally lacks actual forced-palette observations. All 116 rows per engine bind to successful final attempts. The audit exits **1** for failed gates; it does not certify a failed packet as successful.

## Retention and remaining gates

[Archive manifest](raw-artifacts.json) preserves 4309 entries as 1026 unique blobs in 7 parts, each at most 32MiB. It includes current browser/repeat runs, the interrupted parent attempt and its repeat diagnostics, plus linked bounded views. `python3 restore.py <destination>` restores bytes with explicit hash, size and path checks. `PYTHONOPTIMIZE=1` retains those checks. Replay using `node audit.mjs --repo-root <checkout> --evidence-root <destination> --output-root <new-receipts>`; known failed gates still return failure. Bounded indexes link to the complete raw report and retained attachments.

The [preceding implementation packet](../../report.md) remains historical. Approved images, frozen intent, submodules and all 18 manual checkboxes remain unchanged. NVDA/JAWS/VoiceOver heard speech, actual 400% zoom, Windows, physical tablet/phone, WebKit actual palette paint, OI-16/G-4/Fluent/Product decisions and separate live-backend acceptance remain open. The exact AppHost stopped cleanly. No staging, commit, push, dependency/policy/submodule update or image regeneration occurred.

[Final integrity audit](audit.json) records post-retention identity and restoration checks.

The bounded stabilization spec is complete; the sprint story remains in `review` with acceptance gates open. The explicit no-staging/no-commit/no-push constraint takes precedence over the generic Build completion commit step.
