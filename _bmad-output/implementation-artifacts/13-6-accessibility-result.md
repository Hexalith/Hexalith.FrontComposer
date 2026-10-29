# Story 13.6 accessibility result and human checklist

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
| UX-136-REVIEW | Not a manual Pass record | `performed: scoped automated evidence review` | Both independent Build review passes and parent verification are retained in the prior complete-candidate report and current spec. Human UX/Product/Fluent sign-off remains open and is not inferred. |

All gate rows above share task/story `13.6` and release branch `main`; future manual runs name the candidate actually tested, while the seven existing reports retain the explicit supporting-details waiver. Jérôme Piquot is the named Owner/reporting human and has explicitly confirmed Pass for all seven records. The seven [human-reported records](#human-reported-verification-records--2026-10-25) retain his exact statement. Their canonical qualification is `completed` under the dated supporting-details waiver; their reported result is Pass. The eighteen detailed evidence checkboxes below remain unchecked as an inventory of observations not itemized in the reports, and do not reinstate the waived metadata blocker. UX-136-REVIEW continues to record only its scoped automated review. AE-01–AE-09 technical behavior, the actual-zoom scope and independent acceptance requirements remain unchanged.

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
