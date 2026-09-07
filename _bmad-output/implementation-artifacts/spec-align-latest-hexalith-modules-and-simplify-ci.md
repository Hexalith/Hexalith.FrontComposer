---
title: 'Align Latest Hexalith Modules and Simplify CI/CD Governance'
type: 'refactor'
created: '2026-08-11'
status: 'done'
baseline_commit: '984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — renegotiated 2026-08-14 by Administrator (bmad-review: Memories 2.21.1 + historical evaluator keep-vs-replace)">

## Intent

**Problem:** FrontComposer CI rejected a successful Release build because its policy duplicated an older EventStore version from the authoritative Builds catalog, and delayed activation then repeated the false failure. Builds CI also rejects 38 newer internal catalog rows when they are checked against an old audit snapshot, while FrontComposer CD still embeds obsolete Builds commit `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` in historical evaluator rows after workflows moved to `3f0e3595be693fce56a37648c0bd0f89390f5fd3`.

**Approach:** Update Builds-owned Memories to stable `2.21.1`, make internal-module checks structural and monotonic instead of duplicating selected literals, then align FrontComposer gitlinks, workflow pins, and active evaluator closures to the resulting immutable Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`. Preserve compatibility builds and exact release provenance, including historical evaluator rows that already-published releases still require.

## Boundaries & Constraints

**Always:** Change artifacts in their owning repository; keep Builds as package-version authority; retain family alignment, valid-version, no-downgrade, import/override, affected-module Release/NuGet builds, exact graph/catalog hashes, workflow pins, evaluator authorization, and release-byte checks. After human integration, use the full Builds commit on every in-scope FrontComposer execution pin and every active CI/Release/post-release evaluator closure. Retain historical `evaluator_authorizations` rows only when already-published release provenance still requires them.

**Ask First:** Creating or pushing the Builds commit; selecting another Memories release; proceeding after Builds or Memories `main` advances; altering external-package pins, graph boundaries, workflow trust, or publication controls.

**Never:** Add a FrontComposer-local package override, edit nested submodules, use recursive/remote submodule updates, make governance advisory, accept an internal downgrade, or remove exact dependency/workflow/release provenance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Internal module upgrade | An aligned `Hexalith.*` family advances beyond its audit snapshot | Catalog checks and consumer builds decide compatibility without stale-literal failure | Fail on malformed, split, unpublished, or build-incompatible input |
| Internal downgrade | Catalog selects a version below its audited baseline | Builds validation rejects it | Name the package and compared versions |
| Structural drift | A required property is missing, duplicated, conditional, or overridden | Semantic validation rejects it | Preserve owner/catalog diagnostics |
| Workflow drift | A CI/CD pin or closure differs from selected Builds | Release remains blocked | Report both identities |
| Historical evaluator rows | Policy already contains prior CI/Release/post-release closures while workflows pin the integrated commit | Active CI/Release/post-release closures name the integrated commit; prior rows remain only if already-published release provenance still requires them | Do not wipe historical rows; do not require every stored row to name the new commit |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:6-13,67-69` -- authoritative family properties (including Memories `2.21.1`) and the three Memories package rows. `HexalithFrontComposerVersion` is self-version / release-owned; `HexalithChatbotVersion` has no consumer here — neither joins the six governed names.
- `references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1`, `validate-package-version-audit.ps1`, `test-package-version-audit-generator.ps1`, `test-package-version-audit-validator.ps1`, and `Tools/README.md:61-101` -- separate owner-controlled internal advances from external dependency decisions.
- `eng/dependency-graph-policy.json:54-67,346-end` -- `selected_catalog_required_property_names` (six consumed `Hexalith*Version` names) plus empty `selected_catalog_required_properties`, then exact CI/Release/post-release `evaluator_authorizations` (active `3f0e3595…` rows and retained historical `a8a50859…` rows).
- `eng/dependency_graph.py:1139-1174,1438-1442,1784-1847` -- `assert_selected_catalog_property_shape`, the `required_property_names` loop, and closed name validation. Do not treat `1021-1057` / `1211-1325` as property-shape checks.
- `tests/eng/test_dependency_graph.py:557-611,1933-2037` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:35-55` -- semantic property-shape regressions and the single live `RunDependencyGraphValidate` consumer (`CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`).
- `.github/workflows/ci.yml:24-25`, `.github/workflows/release.yml:16-17,321-329`, `.github/workflows/release-evidence.yml:228-239` -- in-scope reusable/action execution identities that must move in lockstep to `3f0e3595…`. Out of scope (remain `@main` unless Ask First changes workflow trust): `commitlint.yml`, `codeql.yml`, `dependency-review.yml`, and other non-release reusable workflows.
- `references/Hexalith.Builds` (`3f0e3595be693fce56a37648c0bd0f89390f5fd3`) and `references/Hexalith.Memories` (`301041626f32d4fb9b6a1154e5e09d65a70a2fcc`) -- root-only gitlinks. Update with `git -c submodule.recurse=false submodule update --init`; never `--remote` or recursive. Regenerating an active closure uses `python3 eng/dependency_handoff.py draft-evaluator --stage {ci|release|post_release} --caller-commit <HEAD> --caller-workflow <path> --policy-commit <HEAD> --output <file>` and writes back caller blob, reusable commit, action commits, `closure_digest`, and `definition_digest`.

## Tasks & Acceptance

**Execution:**
- [x] Builds catalog, audit validator, fixtures, and tool docs -- adopt Memories `2.21.1` and allow aligned internal advances without allowing downgrades or weakening external-package decisions.
- [x] FrontComposer graph engine, policy, Python fixtures, and C# consumer -- make six module requirements presence/shape constraints and remove the duplicate live validation call.
- [x] Root gitlinks and three in-scope workflows -- use captured latest Memories/Builds identities, update every in-scope execution pin, regenerate active evaluator hashes/digests from exact source, and keep historical authorization rows that published-release provenance still requires.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- record that compatibility comes from authoritative structure plus actual builds, while hashes remain provenance.

**Acceptance Criteria:**
- Given Builds selects published Memories `2.21.1`, when its catalog/audit suites run, then they pass with all three Memories packages aligned and still reject an internal downgrade.
- Given a compatible Hexalith module version advance, when FrontComposer dependency governance runs, then governance does not fail on version-literal drift before the exact affected-module Release/NuGet build.
- Given a required catalog property is malformed or absent, when semantic validation runs, then it fails closed with the selecting owner and catalog coordinates.
- Given the integrated Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`, when CI, Release, and post-release provenance are evaluated, then every in-scope execution pin and every active authorized closure names that exact commit, historical `evaluator_authorizations` rows remain only when already-published release provenance still requires them, and the release contract accepts the integrated commit.

## Spec Change Log

- 2026-08-14 (`bmad-review` loop 1, Administrator): renegotiated frozen Memories target `2.20.7` → live published `2.21.1` and Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`; added keep-vs-replace for historical `evaluator_authorizations` (active closures name the integrated commit; retain prior rows only for already-published release provenance). Retargeted Code Map and Verification to the live property-name shape, named Builds validators, in-scope workflow pins, `draft-evaluator` inputs, and the single Governance consumer. Prose: delayed-activation subject, catalog-vs-audit-snapshot wording, governance-as-fail-subject, Design Notes condensed to the unique rationale.

## Design Notes

The shared catalog selects versions; repeating internal values in consumer policy or an old audit snapshot adds synchronization failures, not compatibility evidence. Family alignment, monotonicity, publication, consumer builds, and exact provenance still bind.

## Verification

**Commands:**
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1`, `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1`, plus `dotnet build Hexalith.Builds.slnx --configuration Release` -- expected: all pass.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_dependency_handoff.py tests/eng/test_workflow_source_closure.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py -v` -- expected: all pass.
- `approved=3f0e3595be693fce56a37648c0bd0f89390f5fd3`; `test "$(git -C references/Hexalith.Builds rev-parse HEAD)" = "$approved"`; `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved "$approved"` plus `actionlint` on `ci.yml`, `release.yml`, and `release-evidence.yml` -- expected: gitlink HEAD equals the recorded commit, exact identity and syntax pass.
- `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` plus `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds"` -- expected: green with zero warnings.
- Inspect `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- expected: compatibility is authoritative structure plus affected-module builds; hashes remain provenance.

## Review Triage Log

Review of the tree since `baseline_commit` `984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d` (2026-09-07). That window includes this story plus later catalog advances and unrelated product work. Verdicts below are for the cited claim, not for whether the later work should be reverted.

| ID | Layer | Verdict | Evidence |
| --- | --- | --- | --- |
| BH1 | blind-hunter | medium | `FcPageTab.OnInitialized` registers into `FcPageTabs` and never unregisters. `Register` throws on a new instance with the same `Id`. Dynamic tab lists and recreate-after-dispose can leak descriptors or throw. `FcPageTabsTests` does not cover disposal or duplicate-id replacement. |
| BH2 | blind-hunter | false | Invalid `[CommandTarget]` is a tested warning-and-clear path: `ParseCommandTarget` returns null and `CommandParserTests` assert `ClearsDescriptorAndEmitsHFC1005`. Generation does not keep a wrong target. HFC1005 is `DiagnosticSeverity.Warning` by design. |
| BH3 | blind-hunter | high | Provider-mode forms resolve `IEnumerable<ICommandTargetIdentityProvider<T>>` only on submit. `FailCommandTargetResolution` returns null, and `CommandFormEmitter` still calls `DispatchWithLifecycleObservationsAsync` with that null snapshot, so a missing provider dispatches without FC-NIP eligibility. |
| BH4 | blind-hunter | false | `INewItemIndicatorStateService.Subscribe` is a documented default-interface inert shim for source/binary compatibility; `Snapshot` is the initial-read path. Custom implementations that predate `Subscribe` behave as they did. |
| BH5 | blind-hunter | medium | Skip links set `@onclick:preventDefault="true"` and focus only through `fc-keyboard.js` (`FocusElementAsync` / `FocusNavigationAsync`). A null module or failed JS leaves native hash navigation blocked. Accessibility tests still only suffix-match `href`. Same defect as VG1. |
| BH6 | blind-hunter | low | `CommandFormEmitter` / `CommandLifecycleBridgeEmitter` still emit hardcoded English `"Command rejected."` / `"Review the command and retry."`. Real i18n gap, but English-first shell copy; fix is new localizer surface, not a direct correction. Rejected as low. |
| BH7 | blind-hunter | low | `CommandServiceExtensions.DispatchWithLifecycleObservationsAsync` swallows non-fatal observer exceptions on the legacy adapter path. Telemetry gap on a compatibility overload; adding Contracts logging is extra surface. Rejected as low. |
| BH8 | blind-hunter | medium | `AuthorizingCommandServiceDecorator` constructs a new `LegacyLifecycleObservationCommandServiceAdapter` per dispatch when the inner service is lifecycle-only, using `Options.Create(new FcShellOptions())` when `shellOptions` is null, so configured pending-command limits/timeouts are ignored on that path. |
| BH9 | blind-hunter | false | `CounterSampleCommandService` implements `ICommandServiceWithLifecycle` and `ICommandServiceWithLifecycleObservations`. Replacing `ICommandService` with that wrapper is the decorator pattern so the constructor can take the inner observations client. Generated forms resolve `ICommandService`. |
| BH10 | blind-hunter | medium | `EventStoreQueryClient.EnsureSuccessfulEnvelope` returns when `success` is absent, so a semantic-failure payload without that property still deserializes. `ReadTotalCount` fails closed on a bad number. |
| BH11 | blind-hunter | false | HFC1016 aborting generation for init-only command properties, including `[DerivedFrom]`, is the documented unshipped breaking policy (`AnalyzerReleases.Unshipped.md`: suppression cannot restore generation). Missing adopter samples are docs, not a runtime defect. |
| BH12 | blind-hunter | low | The diff copies BMAD skill trees across `.agent/skills`, `.agents/skills`, and `.claude/skills`. Agent-context duplication; not this story's catalog/CI work. Routed defer because the fix edits agent-context trees. |
| EC1 | edge-case-hunter | medium | `HasMeaningfulQueryPayload` ignores `SortDescending`. A descending-only sort without `SortColumn` or other criteria omits the entire EventStore payload. |
| EC2 | edge-case-hunter | low | `DropPublishedFrontComposerAssemblies.targets` matches `.nuget/packages/hexalith.frontcomposer` in the asset path, not `NuGetPackageId`. Everyday caches use that path; a custom `NUGET_PACKAGES` root is uncommon. Cited `NuGetPackageId.StartsWith` snippet is not in the file. Rejected as low. |
| EC3 | edge-case-hunter | maybe-false | `summarize_quarantine` returns 1 when no TRX files exist (`missing evidence`). Gate 3d is `continue-on-error`, but Quarantine Summary is not. Unverified whether MTP `--filter-trait Category=Quarantined` with zero matches writes a TRX (if it does, classification stays `zero-quarantined` and the job passes). Would be medium if no TRX is written. |
| EC4 | edge-case-hunter | medium | `_wait_until_host_absent_or_ports_closed` returns after timeout without failing. `_capture` then `aspire start`s even if the prior host is still present. The cited `apphost.prior-host.still-running` guard is not in the file. |
| EC5 | edge-case-hunter | high | `ProjectionSubscriptionService.SubscribeAsync` returns while Phase is `Connecting` after retaining a pending group. `OnConnectionStateChangedAsync` for `Connected` does not call `RejoinActiveGroupsAsync` (rejoin is on `Reconnected`). A second subscribe during initial StartAsync can stay Pending forever. |
| EC6 | edge-case-hunter | maybe-false | `parse_trx` rejects when `Counters.total != len(UnitTestResult)`. Would be medium if MTP TRX can record totals for tests that have no `UnitTestResult` node. No fixture in this review proved that shape. |
| EC7 | edge-case-hunter | false | `EventStoreCommandClient.cs:193-198` is the lifecycle observation callback using `result.MessageId`. There is no `CorrelationId ?? MessageId` coalescing in Shell. |
| EC8 | edge-case-hunter | medium | `_TRANSIENT_NETWORK_MARKERS` has `"the requested url returned error: 5"` (5xx) but not `429`. `"error: 429"` does not contain `"error: 5"`, so HTTP 429 fail-closes without the three-attempt retry. |
| EC9 | edge-case-hunter | medium | Root `global.json` uses `rollForward: latestPatch` while quality.yml Epic 9 live lane requires `dotnet --version` equal to `10.0.400`. A newer 10.0.4xx SDK on the runner fails that exact-string check. |
| EC10 | edge-case-hunter | medium | `scripts/pack-release-packages.py` deletes existing `.snupkg` files then packs and returns 0 with no check that each required symbol package exists. |
| EC11 | edge-case-hunter | false | `PendingCommandOutcomeResolver.ApplyObservation` fail-closes to `Unknown` when `MessageId` is empty. Matching terminals by entity keys without a message id would mis-associate; current behavior is correct. |
| EC12 | edge-case-hunter | false | In-scope workflows pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, not `3f0e3595…`. That is later lockstep (`e787690f` landed `3f0e3595`, then `8cb9f06a` / `05f77621` advanced execution). Active closures name the current execution SHA; `3f0e3595` remains in historical `evaluator_authorizations`. Fixing the spec's frozen SHA would edit this spec. |
| EC13 | edge-case-hunter | false | Builds gitlink is `071ef99733ba398362ae838e4696b4b1641ed07a`. `release_contract.validate_builds_identity` requires workflow pins to match `--approved`, and only checks that the gitlink is a SHA, not that it equals the execution pin. Later catalog advances superseded the story's captured commit. |
| EC14 | edge-case-hunter | false | Builds catalog `HexalithMemoriesVersion` is `2.26.1` with all three Memories packages aligned. Frozen `2.21.1` was the 2026-08-14 published target; later catalog advances are the same monotonic pattern this story introduced. Ask First for a new Memories release was consumed by those later main commits. |
| EC15 | edge-case-hunter | false | `python3 eng/release_contract.py builds --approved 3f0e3595…` would reject current `release.yml` pins. That command is a stale spec verification line. Against `--approved 4eb33928…` the contract accepts the live execution identity. |
| VG1 | verification-gap | medium | Pre-verified: skip-link tests never click, never assert `data-enhance-nav="false"`, and never assert `focusElement` / `focusVisibleElementById`. Same root cause as BH5. |
| VG2 | verification-gap | medium | Pre-verified: `UlidFactoryTests.NewUlid_EntropyIsCryptographic_NotPredictableFromPriorOutputs` only asserts `EntropySource` is `CSUlidRng` and never calls `NewUlid()`, so `NewUlid()` can stop passing that RNG and stay green. |

**Grouping / route:** No intent_gap or bad_spec for this story. No in-scope patch. Net HEAD still has structural catalog property names, empty `selected_catalog_required_properties`, family-aligned Memories, in-scope pins lockstepped with the current Builds execution SHA, and historical `3f0e3595` evaluator rows. Survivors are other-story or pre-existing defects from the oversized baseline window and are deferred.
