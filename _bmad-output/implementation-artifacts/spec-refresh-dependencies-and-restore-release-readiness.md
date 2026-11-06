---
title: 'Refresh dependencies and restore release readiness'
type: 'chore'
created: '2026-11-06'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
context: []
baseline_commit: '2ba32f42f48be617e7c621fbf102c65376253cd5'
---

<frozen-after-approval reason="User authorized maintenance and release without human validation">

## Intent

**Problem:** The user wants all changes pushed, the repository and its nine root submodules synchronized, the latest stable .NET SDK and package versions selected, CI failures repaired, and a new Release verified on NuGet. Current CI rejects a stale Platform source projection after submodule changes.

**Approach:** Refresh the compatible dependency selections and committed-object seals, verify the implementation, then let the primary agent commit, push, monitor hosted CI, dispatch Release, approve authorized production deployments, and verify the eight published packages. The user explicitly waives interactive human validation checkpoints.

## Boundaries & Constraints

**Always:** Preserve unrelated changes. Work in each owning repository. Retain latest stable SDK 10.0.403, already pinned in all SDK-bearing repositories. Keep documented Microsoft.Build/Framework 18.9.6 and Microsoft.OpenApi 2.13.0 compatibility pins; newer majors require unsupported .NET/API changes. Retain Node24 type declarations. Keep centralized NuGet ownership in Hexalith.Builds and preserve BOM/CRLF where required. Use exact committed Git objects for all dependency fingerprints. Run tests per project. Validate every exact commit message with the owning pinned commitlint before committing. The primary agent owns commits and all release mutations.

**Never:** Initialize nested submodules, load skills under references, recursively update dependencies, overwrite user work, weaken automated release/build checks, or claim unverified publication. Implementation subagent must not stage, commit, push, trigger workflows, or change remote environments. Release is an explicitly authorized later operation.

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props`: authoritative shared catalog, UTF8 BOM and CRLF. Five compatible stable deltas and the existing StyleCop beta update; retain the verified AngleSharp compatibility pin.
- `tests/e2e/package.json`, `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package.json` and adjacent locks: Playwright packages 1.64.1 to 1.65.0; other direct npm packages current.
- `eng/dependency-graph-policy.json`: consumer_source_projections seals Platform owner, inventory, root selectors, Builds and consumer configuration. FrontComposer selected_catalog_required_packages uses exact external values, currently MCP.AspNetCore2.3.0 versus selected catalog2.3.1.
- `eng/dependency_graph.py`: canonical_digest, consumer_configuration, evaluate_consumer_projections and committed inventory helpers define authoritative hashes; reuse them. The event-before policy validates both graphs. Updated seals need a separate later-base push to activate.
- `eng/dependency-governance.md`: record exact synchronized selections and transition behavior; no fabricated CI outcomes.
- `.github/workflows/ci.yml`, `quality.yml`, `release.yml`, `release-publish.yml`: retain automated gates. Release is workflow_dispatch on exact main, after successful push CI and Quality.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore`, `references/Hexalith.Platform` -- fast-forward clean main worktrees to fetched origin/main, currently 0f22b3ed8 and c1997c60 respectively; verify other seven root selections match origin/main without moving nested checkouts.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- retain AngleSharp1.9.1 under the verified bunit compatibility exception below; update MediatR14.2.0 to14.3.0, Microsoft.Azure.Cosmos3.63.3 to3.63.4, Microsoft.Identity.Web4.17.0 to4.18.0, OpenTelemetry.Instrumentation.Http1.20.1 to1.20.2, Radzen.Blazor12.1.11 to12.1.12, StyleCop.Analyzers1.3.151-beta to1.3.171-beta after registry confirmation. Verify any Hexalith family property updates from the registry audit and apply current compatible public versions if needed.
- [x] `tests/e2e/package.json`, `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package.json` and locks -- update Playwright to1.65.0 using npm. Verify typecheck and focused runner tests.
- [x] `eng/dependency-graph-policy.json` -- compute final Platform seals from committed objects after root synchronization and match strict external expectations to selected catalog. If Builds catalog edits remain uncommitted, record that the primary agent must finish builds_commit/root-selector seals after its owning commit.
- [x] `eng/dependency-governance.md` -- document exact selections, retained compatibility pins and required later-base activation.
- [x] `tests/eng/test_dependency_graph.py`, `tests/eng/test_apphost_dependency_modes.py`, `tests/eng/test_release_contract.py` -- run focused Python checks; restore/build FrontComposer with Release/NuGet settings and run appropriate test projects individually. Existing automatic gates remain authoritative.

**Acceptance Criteria:**
- Given clean root-declared modules, when synchronization completes, then each selected module matches its fetched main and nested modules remain untouched.
- Given registry-audited versions, when dependencies are refreshed, then selected SDK and dependencies are latest compatible releases with documented exceptions.
- Given the final committed selections, when a descendant is evaluated under the landed policy, then exact acquisition and graph validation pass without weakened checks.
- Given the refreshed implementation, when the relevant local checks run, then changes build and focused regression checks pass or an exact actionable blocker is reported.

## Implementation Notes

- Compatibility exception observed during implementation: AngleSharp 1.10.0 is published but incompatible with latest stable bunit 2.13.3. Shell tests fail with `TypeLoadException` because `Bunit.Web.AngleSharp.HtmlAnchorElementWrapper` does not implement `add_BeforeToggle`. Retain patched AngleSharp 1.9.1 until a compatible bunit release passes the Shell suite; all other requested package deltas remain. This follows the frozen latest-compatible constraint.

- The updated StyleCop beta detects multiline collection-expression layout errors in Builds source; applied whitespace-only corrections to 28 collection expressions in 20 files while retaining all analyzer gates.
- A Shell regression run exposed a test-only race while enumerating bunit invocation keys during asynchronous cleanup. The two cleanup helpers now poll the existing per-identifier invocation list and retain exactly-once and owner assertions.
- Concurrent external root commits `b08612337b791fef0d819094d6c345c3d8ab4562` and `51e87aeea2c655c01702130ad6d8061639d845bb` landed during implementation. Their history and dependency-documentation intent are preserved while the authorized current source reseal selects the synchronized commits.

- Concurrent Builds main commits were merged by the primary agent without changing the validated compatible catalog or audit selections. The final Builds source also preserves the incoming CI initialization improvements; validators passed after the merge.

## Spec Change Log

- 2026-11-06: Applied the frozen latest-compatible constraint to retain AngleSharp 1.9.1 after the 1.10.0 runtime failure. Added only the analyzer-required whitespace fixes, the observed Shell test race correction, and dependency documentation reconciliation needed by the requested CI repair.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1: bunit indexer and live list remain unsynchronized | maybe-false | Upstream bunit 2.13.3 uses a normal dictionary/list. The observed cross-identifier enumeration exception is removed and all 13 focused / 3,673 selected Shell tests pass; no bad outcome for the new single-identifier reads was demonstrated. Defer the pre-existing vendor collection concurrency limitation as unverified medium; settle with a controlled concurrent-read/write reproducer or completion-aware test recorder. |
| Blind 2: a delayed duplicate could arrive after the assertion | low | The old helper also asserted immediately after observing the first call. No delayed duplicate path or product failure was demonstrated; a completion protocol would add complexity for this pre-existing test limitation, so reject the proposed expansion. |
| Blind 3: AngleSharp audit rationale says pending compatibility | low | The generated discovery audit correctly retains 1.9.1 and records candidate 1.10.0; pending compatible validation remains true. The catalog comment and governance document record the actual failed runtime validation. Adding a new consumer-evidence mechanism exceeds a direct cosmetic correction, so reject. |
| Blind 4: runtime families lack representative consumer evidence | false | The audit is explicitly registry discovery, with empty consumer acceptance evidence rather than fabricated qualification. The recorded test scope names the actual FrontComposer and Builds consumers; no claim of all ecosystem runtime consumers is made. |
| Blind 5: complete Debug/UI qualification absent | false | The exact Debug AppHost source-recovery test passed after the analyzer repair and the Builds Release solution passes. The existing recorded results explicitly exclude the Release-excluded UI lane; further hosted gates remain required. |
| Blind 6: new Playwright browsers not locally exercised | false | Hosted Quality already exercises both updated workspaces against real browser hosts. These required post-push gates remain pending and publication is not claimed complete; there is no missing automated verification path. |
| Blind 7: spec needs an unchecked graph task | false | A spec-only edit is rejected under the review routing rules. Recorded results explicitly leave committed-root graph validation and delayed activation to the primary agent before publication. |
| Blind 8: activation procedure lacks commands | low | Patch the activation documentation with its exact sealed event base, descendant relationship and acquisition/validation commands during the required activation commit. |
| Blind 9: temporary logs are not durable proof | false | Temporary logs supplement the source-controlled tests. Required hosted CI and Release/Release Evidence workflows produce durable run-linked results and publication evidence before the user task is complete; those operations are still pending. |
| Blind 10: spec lacks external release acceptance criteria | false | A spec-only edit is rejected under review routing. The frozen user intent explicitly requires successful hosted gates, a Release and verification of eight published packages; the primary agent continues those authorized operations. |
| Edge 1: EventStore main advanced after initial selection | medium | Verified fetched descendant 0f22b3ed86a2ecc4a98f09b4cc76bc5a9bba38c9 adds only a packaging test. Patch the selection and canonical selector seal before the root commit. |

Verification-gap reviewer: no verification gaps found. Automated review did not waive any hosted gate.

## Verification

- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_apphost_dependency_modes.py tests/eng/test_release_contract.py`
- `npm --prefix tests/e2e run typecheck`
- `npm --prefix tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd run test:runner`
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:UseNuGetDeps=true`
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -p:UseNuGetDeps=true`

### Recorded results

- Central package validation passed for 316 entries; deterministic audit validation passed for 316 packages, 156 families and one source. The primary agent committed the catalog, audit and formatting in Builds; final selection `0577f0c2d70bd4c2999aa3d98a705c83919bfc00`.
- The full Python command ran 180 tests: 179 passed and the AppHost source-recovery build failed on the newly enforced StyleCop layout. After the whitespace-only fix, `python3 -m unittest tests.eng.test_apphost_dependency_modes.AppHostDependencyModeTests.test_debug_apphost_restore_uses_source_recovery_and_unique_ids` passed (1 test, 137.995 seconds). Logs: `/tmp/frontcomposer-maintenance-python-tests.log`, `/tmp/frontcomposer-maintenance-apphost-focused.log`.
- Both npm checks passed; the focused runner passed all six tests. Playwright locks resolve 1.65.0 in both workspaces.
- FrontComposer Release/NuGet restore and solution build passed with zero warnings/errors. The corrected Shell project rebuilt clean. The seven selected individual Release test projects passed 5,815 tests in total: Shell 3,673; SourceTools 1,149; Mcp 420; Contracts 227; Testing 185; Cli 151; Contracts.UI 10. Governance and advisory categories remain reserved for the authoritative hosted gates. The Release-excluded UI test project was not part of this lane.
- The Shell cleanup-race class passed all 13 tests after the helper correction; the entire selected Shell suite then passed. The MCP package-boundary prerequisite was restored with `dotnet restore src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj -p:Configuration=Release -p:UseNuGetDeps=true -p:EnableFrontComposerPackageValidation=true`; all 420 MCP tests then passed.
- Owning Builds Release solution validation passed with `dotnet build Hexalith.Builds.slnx --configuration Release --no-restore -m:1`, zero warnings/errors. Logs: `/tmp/frontcomposer-maintenance-builds-solution.log`.
- Final Platform inventory seal `0d36101a45831e4074be812adeb5275ef483d239e62f79ccf41af88fbae15f9b`; root-selector seal `ecac30d704129430dc99e137ace73f09e117ffb16ae361406c2900418981ccdd`; unchanged consumer-configuration seal `761443f7449d0d15bfd30beeabccaf9823c34a82657648da06efc1bfd1fb3d52`. Fingerprints use committed objects. Primary-agent validation of the resulting committed root and a later-base activation push remain required.

- Post-review patch: included EventStore main `0f22b3ed86a2ecc4a98f09b4cc76bc5a9bba38c9` (packaging test only) and recomputed the selector seal. Its focused seal regression and diff check pass.

- Primary-agent exact-source validation passed for committed seal `8a51e0a1891f08bbfb51e168b69f766c787b1f46`. The separate activation documentation now includes the exact event-base relationship and acquisition/validation commands; hosted gates and publication verification remain the next operational phase.
