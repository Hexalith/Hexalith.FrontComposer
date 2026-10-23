---
title: 'Refresh Hexalith Dependencies and Fix Memories Resolution'
type: 'bugfix'
created: '2026-10-23'
status: 'draft'
route: 'dispatch'
baseline_commit: '3fd62e497ed9e1ae13811af813487fb97e4adc16'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The workspace's root submodules and shared package catalog lag published Hexalith updates. AppHost reaches Tenants.UI using conflicting Memories source/package modes, causing duplicate Contracts and Client.Rest assemblies.

**Approach:** Pull every root-declared submodule to its configured main branch without recursion. Use the shared Builds catalog for the latest listed stable Hexalith packages; preserve unavailable packages without inventing versions. Align the AppHost UI references on the root Memories source graph in Debug, preserve package mode, and keep the AppHost SDK pin aligned with the updated catalog. Reuse existing audit/validation tooling and prove actual MSBuild selection plus the affected build. Do not push, publish, initialize nested submodules, or change approval/evidence identities.

</frozen-after-approval>

## Implementation Notes

- No intent gaps or irreversible operations. This is one cohesive workspace dependency repair with small configuration changes and no new runtime APIs. The existing direct AppHost UI edges omit HexalithMemoriesRoot/HexalithMemoriesFromSource while the combined UI edges provide them. Regression checks will evaluate the real edges and child project selections, including package mode.
- Initial root and eight submodule revisions are preserved in `/tmp/frontcomposer-dependency-refresh-initial.json`. All repositories were clean on main. Eight fast-forward-only, non-recursive pulls succeeded; five root gitlinks advanced and three were already current. Initial parent HEAD remains the baseline above.
- Live official NuGet metadata for all 87 cataloged Hexalith IDs shows only Memories needs an additional bump after the pulls: all three family packages 2.29.8 to 2.29.9. Hexalith.Chatbot.Contracts is unavailable (404); its existing declaration remains unchanged. Other current catalog selections are already latest stable.
- Builds owns catalog versions. Preserve its BOM+CRLF, commit the reviewed catalog declaration before generating its revision-bound incremental hexalith-memories audit, then validate and commit that audit separately. Parent records the resulting root gitlink; consumers receive no version overrides.
- `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` on the pulled tree exited 2 with the same two CS1704 Memories duplicates. Log: `/tmp/frontcomposer-dependency-refresh-aspire-baseline.log`.
- Added real MSBuild edge-evaluation regressions in `tests/eng/test_apphost_dependency_modes.py`. Before the fix, Debug failed specifically for the direct AppHost path; the combined UI and Release controls passed. After forwarding the root Memories path/source flag on both direct UI edges, both tests pass. Logs: `/tmp/frontcomposer-dependency-modes-red.log` and `/tmp/frontcomposer-dependency-modes-green.log`.
- Aligned the root AppHost SDK to 13.6.3, matching the pulled catalog and the existing FrontComposer entry in Builds Tools/package-version-exceptions.json. No third-party package declarations were independently changed.
- Builds catalog validation passed for all 307 entries. Catalog source commit: `4e80856933c7db69a8c3de706b2367b63993d772`; its exact message passed Builds' pinned commitlint 21.2.4 before commit. Successful evidence: `/tmp/frontcomposer-builds-memories-catalog-commitlint.log`.
- The first incremental audit request correctly refused an already-stale upstream EventStore audit selection (3.126.1 vs pulled catalog 3.127.0). The retry refreshes exactly hexalith-eventstore and hexalith-memories, preserving all other families and history. No validator was weakened.
- AppHost Debug restore passed using the updated shared catalog and aligned Memories graph; `/tmp/frontcomposer-dependency-refresh-debug-restore.log`.
- Full build disproved that forwarding alone was sufficient: the existing NuGet restore walk re-evaluates child projects without every parent edge property, leaving Memories package assets beside source references. The regression now includes the default restore evaluation, explicit Debug package override, unavailable-source fallback and Release mode.
- Extended Tenants' existing non-packable UI source-availability exception to the complete Memories clients in Debug/default configuration. It preserves explicit Memories=false, missing-source fallback, non-UI projects and Release defaults. The change belongs to Tenants Directory.Build.props; no nested submodule is touched.
- Four actual MSBuild tests pass after the restore-default correction. Fresh AppHost Debug restore now records Memories.Contracts and Client.Rest exclusively as project libraries (1.0.0), with no Memories NuGet libraries. Logs: `/tmp/frontcomposer-dependency-modes-restore-green.log`, `/tmp/frontcomposer-dependency-refresh-final-debug-restore.log`.
- Incremental audit now passes for 307 packages, 151 families and one source. It refreshed exactly EventStore and Memories, preserving the other 149 families and bound history. Logs: `/tmp/frontcomposer-builds-memories-audit-refresh.log`, `/tmp/frontcomposer-builds-memories-audit-validation.log`.
- Final serialized AppHost Debug build passes with zero warnings/errors, including Tenants.UI, Parties.UI and both Memories source projects. Command: `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore --nologo -m:1 -v:minimal`; log `/tmp/frontcomposer-dependency-refresh-final-debug-build.log`.
- Builds authoritative catalog tests pass for 50 approved identities and three shared versions; `/tmp/frontcomposer-builds-authoritative-catalog-tests.log`.

## Review Triage Log

Blind Hunter reviewed all locally authored configuration, tests and audit changes at the parent model capability; upstream pulled source was treated as a dependency input. Other layers were skipped by the oneshot route. A follow-up verified the fixes to findings 1–9 and reported no remaining concrete issue.

- BH-1 — medium / patch: the initial configuration-specific Memories default disagreed between default restore and Release while sharing assets. Memories now follows the existing FrontComposer UI source choice across configurations. Unset-Configuration and Release evaluations agree; explicit package-mode validation remains supported.
- BH-2 — low / patch: documented that the existing non-packable UI source exception also retains Memories sources when UseNuGetDeps alone is set. HexalithMemoriesFromSource=false is the narrow override; full package validation also sets HexalithFrontComposerFromSource=false.
- BH-3 — medium / patch: hardcoded true on UI parent edges discarded an explicit Memories package override. AppHost and combined UI now forward the effective value; tests cover both parents and both Tenants/Parties edges.
- BH-4 — low / patch: added evaluation with no Configuration property, rather than labeling an explicit Debug evaluation as the default restore case.
- BH-5 — low / patch: tests now cover the changed Parties edge as well as Tenants.
- BH-6 — medium / patch: wired the seven-case suite into the existing blocking Quality Gate 2b after root submodule initialization. The YAML parses and the exact command is present.
- BH-7 — medium / patch: added actual isolated Debug restore and exact library-count/type assertions, preventing a mixed source/package graph from passing. The test preserves normal workspace assets.
- BH-8 — medium / patch: isolated Release/package-mode Tenants.UI restore/build passes with Memories.Contracts and Client.Rest 2.29.9 as NuGet libraries, zero warnings/errors. Log: `/tmp/frontcomposer-dependency-refresh-package-build.log`; outputs `/tmp/frontcomposer-dependency-refresh-package-validation`.
- BH-9 — low / patch: package and missing-source fallbacks now assert absence of Memories project references as well as package presence.
- BH-10 — low / reject: retaining a second per-ID live-feed inventory adds no normal validation value. The existing central audit retains the refreshed families and preserves other observations explicitly. Full metadata discovery is reproducible from Builds with `pwsh -NoProfile -File Tools/audit-central-package-versions.ps1 -PriorAuditPath Tools/package-version-audit.json -OutputPath artifacts/latest-package-check.json`; no claim is made that the other 149 family observations were refreshed. No work deferred from review.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Tenants`
- `references/Hexalith.Memories`
- `references/Hexalith.Parties`
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj`
- `.github/workflows/quality.yml`
- `tests/eng/test_apphost_dependency_modes.py`
- `_bmad-output/implementation-artifacts/spec-refresh-hexalith-dependencies-and-fix-memories.md`

## Verification

- Final mode/restore regressions: `python3 -m unittest discover -s tests/eng -p test_apphost_dependency_modes.py -v` — 7 passed; `/tmp/frontcomposer-dependency-modes-final.log`. Real restore runs in isolated artifacts and asserts exactly two Memories project libraries, preventing duplicate package rows from being hidden by dictionary deduplication.
- Reviewed Debug AppHost build: `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore --nologo -m:1 -v:minimal` — exit 0, zero warnings/errors; `/tmp/frontcomposer-dependency-refresh-reviewed-debug-build.log`.
- Published-package validation: `dotnet build references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --configuration Release --artifacts-path /tmp/frontcomposer-dependency-refresh-package-validation -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false --nologo -m:1 -v:minimal` — exit 0, zero warnings/errors; `/tmp/frontcomposer-dependency-refresh-package-build.log`. Isolated assets contain both Memories packages at exactly 2.29.9 and do not alter normal Debug assets.
- `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1`, `Tools/test-authoritative-package-catalog.ps1` and `Tools/validate-package-version-audit.ps1` from Builds all passed. Catalog covers 307 rows; audit covers 151 families. Existing validator and focused catalog-test logs are under `/tmp/frontcomposer-builds-*`.
- Exact commit messages passed each owning repository's pinned commitlint before use. Builds audit commit: `65ad1b41033036ba16a2f1e5ef99c5ee8baaaa03`; Tenants source-mode commit: `01b8c79d48a73c44d5ffd349f241a0c059eb34a8`. Commitlint evidence: `/tmp/frontcomposer-builds-audit-commitlint.log`, `/tmp/frontcomposer-tenants-memories-commitlint.log`, `/tmp/frontcomposer-dependency-refresh-commitlint.log`.
- AppHost starts successfully after the fix. FrontComposer UI, Tenants UI, Parties, Tenants and Counter resources reported Running/Healthy. EventStore exits 134 because the upgraded gateway requires Authentication:WorkloadIssuer:ClientId and ClientSecret in OIDC authority mode; the local realm has no service-account workload client. Logs: `/tmp/frontcomposer-dependency-refresh-eventstore-console.log`, `/tmp/frontcomposer-dependency-refresh-eventstore-otel.log`. No security validation or existing approval gate was disabled. This separate runtime credential migration is not part of the Memories reference repair.

- Committed-object compatibility preview: `2e74d8bbaab8af0f6af1f29667ca0888bcdcd5b4` from staged tree `62071500b9ecf9444a58a26630262b3670a17069` (no branch moved). `python3 eng/dependency_graph.py --root . validate --commit 2e74d8bbaab8af0f6af1f29667ca0888bcdcd5b4` exits 1 because the approved profile expects FsCheck.Xunit.v3 3.5.0 while the pulled catalog supplies 3.6.0. Comparison also identifies ModelContextProtocol.AspNetCore 2.2.0 versus 2.3.0. Exact output: `/tmp/frontcomposer-dependency-refresh-graph-validation.json`. No policy was changed or acceptance inferred.
- Workflow replan: requested pulls, package updates and Memories repair are concrete and focused validation passes. Closing the broader gate now requires a third-party policy-baseline migration; complete EventStore runtime startup additionally requires the new local workload-client authentication configuration. These expand the planned configuration repair, so implementation is paused at the workflow's replan checkpoint pending the user's scope decision. The running AppHost was stopped after inspection.

## Code Map

- `eng/dependency-graph-policy.json`: frontcomposer-catalog-v1 owns the two exact third-party expectations. Architecture requires base-policy activation; a candidate policy cannot authorize itself.
- `.github/workflows/quality.yml`: existing engineering Gate 2b now invokes the real dependency-mode/restore tests.
- `src/Hexalith.FrontComposer.AppHost/Program.cs` and `KeycloakRealms/hexalith-realm.json`: local OIDC wiring lacks EventStore's new confidential workload client. Read EventStore's existing workload-assertion guidance before any migration; do not invent credentials, widen scopes or disable the startup gate.
- Root-declared Builds and Tenants own the already-committed catalog/audit and restore-mode changes. Preserve their local commits and all five advanced root gitlinks.

## Open Questions

- Extend the change to reconcile the approved FsCheck/MCP expectations and migrate the local workload-client wiring required by EventStore 3.127, or retain the requested dependency/Memories changes with these explicit remaining blockers?
- Read-only proposal check: a temporary policy copy changing only FsCheck.Xunit.v3 to 3.6.0 and ModelContextProtocol.AspNetCore to 2.3.0 validates the full 51-edge candidate graph successfully. This does not alter the repository policy or satisfy base-policy authorization. Evidence: `/tmp/frontcomposer-dependency-refresh-proposed-policy-validation.json`.
- Additional compatibility evidence while the scope decision is pending: SourceTools.Tests builds in Debug with the refreshed catalog, zero warnings/errors; `/tmp/frontcomposer-dependency-refresh-sourcetools-tests-build.log`. Scoped story-artifact validation and whitespace checks pass. Root changes remain staged and uncommitted; child-owned catalog/audit and Tenants fixes are committed locally, with no pushes.
