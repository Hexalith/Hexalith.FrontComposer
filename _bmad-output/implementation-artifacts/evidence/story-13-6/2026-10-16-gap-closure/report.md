# Story 13.6 automated verification gap closure — 2026-10-16

Readiness remains open. The current three-engine lane is **457 passed, 12 failed, 2 skipped / 471**. All current nonvisual regressions pass; twelve preserved Firefox/WebKit visual comparisons remain failed. Configured Debug Shell passes 3693/3693. No human or release approval is inferred.

## Candidate and preservation

The source candidate is `sha256:2ad017457f917b89770f291b2deea9c451af0779afd45748478d8010a0e6bef9`, based on `0129bd39095892283371f97836ade5768fef3809` on `main`. [candidate-source.json](candidate-source.json) records raw SHA-256 for all changed source/configuration inputs, the exact root submodule heads and dirty tracked-diff hashes, the 24 unchanged committed image hashes and unchanged frozen-intent hash. Source files use existing EOL policy; this dated evidence tree is protected with `-text`. Documentation/evidence are excluded from the source aggregate to avoid a circular identity. [candidate.patch](candidate.patch) records tracked changes; the new internal fixture accessor is separately hash-bound in the manifest.

[Browser source](browser-source.json), [input reuse](input-reuse.json), [browser outcomes](browser-verification.json) and [runtime-focus.json](runtime-focus.json) bind the executed browser assertions/assets to this final source. Post-browser input changes strengthen the Shell-only held-dispatch proof and allow explicitly expected unrelated skips in the Node artifact validator without giving skipped owners state authority. Browser/runtime inputs remain byte-identical. The final rebuilt Shell execution includes that change. Existing EventStore/Tenants edits remain untouched. No staging, commit, push, dependency upgrade or baseline regeneration occurred.

## Executed verification

| Check | Result | Evidence |
|---|---|---|
| Exact three-spec, three-engine lane, workers=3 | 457 passed, 12 failed, 2 skipped / 471; see per-engine rows below | [browser log](raw/browser-final2.log), [outcomes](browser-verification.json), [raw report](playwright.json), [JUnit](junit.xml) |
| Focused complete modal/reflow/host reuse | 15/15, all three engines, 28.2s | [modal-final2.log](raw/modal-final2.log) |
| Configured Debug Shell, all five exclusions preserved | 3693/3693, no failures/skips; final stronger scope test included | [shell-configured-handoff.log](raw/shell-configured-handoff.log) |
| Focused Shell generated feedback/scope/recovery plus fixture guards | 166/166 before the final test-only drain; final scope class rerun separately | [shell-focused-final.log](raw/shell-focused-final.log), [scope-handoff.log](raw/scope-handoff.log) |
| SourceTools emitter class | 68/68 | [sourcetools-focused.log](raw/sourcetools-handoff.log) |
| Node artifact regressions | 10/10 | [node-final.log](raw/node-final.log) |
| TypeScript | Passed | [typecheck-final.log](raw/typecheck-final.log) |
| CI governance | 10/10 | [ci-governance-final.log](raw/ci-governance-final.log) |
| Debug builds | AppHost/Counter.Web, SourceTools.Tests and Shell.Tests passed | [AppHost](raw/apphost-build-test.log), [SourceTools](raw/sourcetools-handoff-build.log), [Shell](raw/shell-handoff-build.log) |
| Artifact policy | Each engine/run attempted; raw roots exceed the unchanged 50MiB bound; bounded per-engine Chromium passes and Firefox/WebKit reject six visual outcomes each | [artifact-validation.json](artifact-validation.json), [state-validation.json](state-validation.json) |
| Visual governance / whitespace | Passed; all 24 baseline image bytes unchanged | [visual-governance-final.log](raw/visual-governance-final.log), [diff-check-final.log](raw/diff-check-final.log) |

- chromium: 157 passed.
- firefox: 150 passed, 1 skipped, 6 failed.
- webkit: 150 passed, 1 skipped, 6 failed.

The two skips are the existing Chromium-serialized Story11.5 computed-style/reduced-motion case in Firefox/WebKit. They are not counted as passes. All 116 state rows per engine are retained with test/run/retry identity and raw hashes in [state-ledger.json](state-ledger.json). Linux WebKit reports media rules without authored-color replacement: its 116 actual system-palette cells remain uncovered. Capability-limited media checks never supply a system-palette pass. Failed outcomes are rejected by the authoritative validator; state files cannot turn a failed browser lane into a pass.

## Findings and implementation

- **R01: implemented and registered.** Reject application refocus after an unprevented boundary Tab; inspect actual focus calls and focus events.
- **R02: implemented and registered.** Composite fractional pseudo-element opacity; .01/.1 fail and opaque paint passes.
- **R03: implemented and registered.** Inventory ellipse, line and polyline; SVG use remains explicitly unsupported and fails closed for pixel evidence.
- **R04: implemented and registered.** Exclude zero-width SVG strokes; an actual 2px stroke supplies the positive control.
- **R05: implemented and registered.** Reject filter, backdrop-filter and blend composition for pixel evidence; simple unfiltered paint passes.
- **R06: implemented and registered.** Measure effective glyph fill, including authored webkit text fill; system color replacement is capability checked.
- **R07: implemented and registered.** Reject positioned unrelated sibling background composition for pixel evidence; removing the layer supplies the positive control.
- **R08: implemented and registered.** Exclude invisible ancestor groups from focus geometry; visible ancestry passes.
- **R09: implemented and registered.** Measure opaque descendant obstruction across the target/ring. Fluent native contained parts and unshifted normal-flow tabs retain actual ownership.
- **R10: implemented and registered.** Inventory generated meaningful text, with generated-content opacity and effective glyph fill.
- **R11: implemented and registered.** Exclude collapsed negative-spread shadows regardless of offset.
- **R12: implemented and registered.** Compare individual changed shadow layers and exclude permanent or fully hidden layers.
- **R13: implemented and registered.** Compare inset pseudo borders against the control paint; unsupported straddling composition fails closed.
- **R14: implemented and registered.** Composite placeholder opacity before measuring contrast.
- **R15: implemented and registered.** Exercise the full dark toolbar→palette→Settings sequence, hold the actual Fluent 25ms callback, and preserve a later operator Tab. Native close resets modal input tracking for the current reservation on each reused host opening.

Additional regressions check clipping with and without spacing, labelled grid/tab scrollports, stale and unjustified Essential target exceptions, mirrored target geometry, and retry/mixed/partial evidence. The validator binds one final successful attempt per test/engine/run to its authoritative JSON attachment bytes; failed attempts remain retained and cannot fill missing final states. The WebKit capability record is similarly bound. Linux CI validates each engine and run; existing Windows validation and artifact bounds remain.

Ordinary generated warnings now replace blank/whitespace Title and body with the existing safe localized fallback; retry guidance and entered values persist. English/French rendered tests cover ordinary and zero-field warnings. Actual shell tenant/user scope remounts clear feedback, cancel held dispatch, drain its completion, and inspect both old and replacement announcement surfaces. Held missing-detail recovery cannot focus or announce after generation changes.

The new nested-scroll inventory reproduced a 96px Fluent more-options button overflowing the 48px rail. Fluent `IconOnly` preserves the 72/48px contracts and fixes it; the browser checks both widths and both media states. No custom Fluent theme/component implementation was added.

The full lane also exposed a test synchronization race in the polling cancellation proof. Provider completion could unregister the queued CancelAsync callback before the test observed it. Waiting for the existing CancellationFinished signal before releasing provider completion preserves the original five-second budget and assertions. Single-worker reproduction failed 3/3 before and passed 3/3 after; the final configured Shell run passes. [Before](raw/polling-constrained-before.log), [after](raw/polling-constrained-after.log), [original full failure](raw/shell-configured-2.log).

## Aspire baseline and Test fixture

The exact target is `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`. Initial `aspire start --apphost <target> --non-interactive` failed in a parallel Shell build with MSB4018/IOException; inspection found no running target. Serialized `dotnet build <target> --configuration Debug -m:1 -p:NuGetAudit=false` passed, then Aspire `start --no-build`/`describe` established a healthy Counter.Web baseline. Initial failure and inspection logs remain under raw/.

The launch profile forces Development, so inherited Test variables did not enable gated controls. After API/documentation checks, the AppHost explicitly forwards Test/specimen/timing flags only when both local run mode and explicit Test environment are selected. Development and publish retain their behavior; source guards assert that boundary. `aspire start --no-build --apphost <target> --non-interactive -- --environment Test`, `aspire describe --apphost <target> --non-interactive` and `aspire wait counter-web --apphost <target> --non-interactive --timeout 60` established the Test endpoint `http://localhost:5201`. [Test startup](raw/apphost-test-start.log), [describe](raw/apphost-test-describe.log). Other backend-dependent resources are not live-backend acceptance evidence.

The fixed Test-only user accessor is registered after the ordinary demo accessor so scope controls exercise the same real `ScopeBoundaryService` consumed by the shell. It has no production/public API. These implementation dependencies and the rail fix are recorded in the spec outside frozen intent.

## Failed attempts and retention

[retained-attempts.json](retained-attempts.json) gives lossless output archives, original output/report paths, per-file hashes and archive hashes. [curation.json](curation.json) records raw and derived report identities, every copied file and restoration mapping for optional HTML links. The per-engine bounded views preserve all engine-owned JSON/images and all authoritative outcomes; only attachment paths are relocated, with the exact mapping and raw/derived hashes retained. Optional trace/video bytes remain in the lossless archives. Every failed browser attempt in this implementation remains retained; raw logs preserve failed/interrupted Shell and Aspire attempts as well. The preliminary full lane was interrupted only after deterministic helper over-rejection was established; it supplies no complete acceptance claim. The next complete run (449 passed/17 failed/2 skipped) reproduced two precise ownership false positives and Firefox native Escape return, and remains separately attributed by [previous-full-source.json](previous-full-source.json). Current results supersede it without pooling artifacts. The host-reuse fixture failure is retained; its second cycle lacked an actual focus change, now supplied explicitly.

Generated Pact validation output is retained in [raw/pact-handoff-generated.md](raw/pact-handoff-generated.md) and [identity](raw/pact-handoff-identity.json); only this run's generated modification was restored. [redactions.json](redactions.json) records raw startup-export identities before removal of local dashboard login tokens. No application/tenant/user secret is included.

## Remaining acceptance gates

- All twelve Firefox/WebKit Light/Dark × Compact/Comfortable/Roomy visual comparisons remain failed against preserved images. Retain expected/actual/diff and obtain hosted environment/visual-owner reconciliation before any separately authorized baseline change.
- Linux WebKit actual forced-palette replacement is unsupported. Windows execution is unperformed on this Linux host; run the existing hosted job.
- Manual NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari, heard speech, actual 400% zoom from measured 1280 CSS pixels, and physical tablet/phone use remain unperformed.
- OI-16, G-4, FLUENT-APP-1, Product approval and separate live EventStore fresh-row acceptance remain open. Parent Build review follows this implementation handoff.
- Closed shadow roots, SVG use, unsupported filters/blends/sibling background composition, fractional group opacity, complex clips and unsupported focus composition require supported platform/pixel evidence. Helpers fail closed; this is not blanket accessibility certification.
