---
title: 'restore CI and publish FrontComposer packages'
type: 'bugfix'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c4f5c12e32182739ce1478a6cdefe602c4daeb18'
context: []
---

<frozen-after-approval reason="user authorizes fixes, release and bypassing human authorization in invocation">

## Intent

**Problem:** Current main cannot release because package validation detects removed Shell constructors, Quality guards are stale, and release compatibility/evaluator metadata may lag the candidate.

**Approach:** Restore binary-compatible constructor overloads, align guards with the already selected catalog and current scheduler behavior, update release metadata using actual source evidence, then publish the semantic-release version and independently verify all eight NuGet packages. User explicitly authorizes release and human-approval steps without another checkpoint.

## Boundaries & Constraints

**Always:** Preserve existing user edits to spec-actions-39123696668-fix-cicd-release.md. Keep API compatibility checks, audit, exact-main successful push CI, candidate sealing and NuGet verification enabled. Commit only changes for this task after review. Use authenticated production-reviewer approval under the user's authorization. Preserve existing catalog dependency versions. Required owning-repository configuration corrections may advance a root gitlink. Implementation subagent performs local changes and checks only; parent owns commits, pushes and publication.

**Never:** Fabricate CI success, evaluator authorization, attestations or package observations. Do not initialize nested submodules, suppress real API breaks, erase immutable evidence, or modify the older user-owned spec. Do not weaken product behavior to satisfy obsolete source-text assertions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Binary compatibility | Published six-argument Shell constructors | Existing compiled consumers can resolve the same signatures; newer context-aware paths remain available | Real ApiCompat gate fails if an overload is missing |
| Quality census | Archived .cs evidence plus compiled source | Archived snippets do not add live suppression ledger entries; live suppressions remain inventoried | Untracked live exceptions still fail |
| Release source | Exact main SHA with green push CI | Genuine matching evaluator handoff and prepared packages | No publication from red or mismatched source |
| Publication | Eight inventory package IDs | All released versions downloadable and listed on NuGet | Report missing or mismatched packages |

</frozen-after-approval>

## Code Map

- src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs -- restore six-argument constructor forwarding to current implementation; keep current tenant/time-provider semantics.
- src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs -- restore published six-argument constructor (optional time provider/localizer), retaining user-context-aware constructor without ambiguous short calls.
- tests/Hexalith.FrontComposer.Shell.Tests -- existing projection loading, scheduler, DI and governance coverage; add meaningful binary signature/DI coverage where missing.
- eng/dependency-graph-policy.json -- selected_catalog_required_packages aligns FsCheck.Xunit.v3=3.5.0, Microsoft.Extensions.Localization=10.0.13, Microsoft.Extensions.TimeProvider.Testing=10.11.0, System.Collections.Immutable=10.0.13, Verify/Verify.XunitV3=33.3.4. Already selected catalog supplies these versions. Evaluator authorizations must match actual descriptors, never invented evidence.
- tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs -- expected Fluent version is stale; selected Components/Icons are 5.0.1.
- tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs -- tracked-source census around line1743 includes archived evidence; exclude only archived _bmad-output/implementation-artifacts/evidence/ snapshots, retaining active eng/*.cs and all other ledger invariants.
- tests/e2e/specs/fc-nip-command-target-identity-contract.spec.ts -- line154 obsolete budget expression; current single-slot fairness uses _singleSlotTurn. C# BudgetOneAlternatesTransportAndTerminalConvergenceWhilePendingPersists verifies behavior.
- docs/diagnostics/compatibility-suppressions.json, Directory.Build.targets, eng/release_compatibility.py and any package baseline overrides -- current release v4.6/baseline4.5; next feature release is v4.7 and must validate against published4.6.
- .github/workflows/ci.yml, release.yml, release-publish.yml; eng/dependency_handoff.py, workflow_source_closure.py; tools/release-packages.json -- retain existing release mechanism and trusted publishing identity.

## Tasks & Acceptance

**Execution:**
- [x] Restore compatible Shell constructors and verify public signature/DI behavior.
- [x] Align catalog/Fluent guards and exclude archived evidence from live analyzer census; verify focused governance tests.
- [x] Update obsolete browserless scheduler guard and run its focused Playwright test.
- [x] Advance compatibility line/baseline to the actual planned semantic version, preserving suppression lifecycle and real validation.
- [x] Diagnose and repair genuinely stale release evaluator descriptors or workflow handoff defects, retaining truthful exact-source proof. Generate actual caller/closure policy rows from committed objects for CI, Release and release-evidence; parent must land policy before a subsequent candidate commit because CI uses EVENT_BASE policy.
- [x] Parent: review changes, validate exact commit message, commit and push; obtain successful push CI on exact main.
- [x] Parent: dispatch Release, approve protected jobs using explicit user authorization, verify immutable GitHub Release and all eight NuGet packages.
- [ ] Complete fresh hosted Quality and CI verification for the reviewed browser and AppHost helper follow-up.

**Acceptance Criteria:**
- Given existing published constructors, when package compatibility validation runs, then no removed-member errors remain.
- Given current catalog and archived evidence, when Quality guards run, then genuine live invariants pass without counting archived source.
- Given successful exact-source push CI, when Release runs, then the semantic version is published and all eight package IDs are confirmed on NuGet.org.

## Implementation Notes

Baseline AppHost startup attempted with aspire start --non-interactive; failed MSB4018 file sharing during parallel source-resource build. Exact-apphost stop/describe confirms no running AppHost for this checkout. Use serialized focused Debug builds and real CI Release validation. User authorizes continuing without planning checkpoints. Parent will preserve user-owned modified spec.

## Spec Change Log

## Review Triage Log

- Blind named-source-call finding: low, rejected as an unlikely pre-release-only call shape requiring additional public overloads. Published4.6.0 has only the six-parameter constructor; all current repository context-aware calls supply the full seven arguments. Published optional six-parameter metadata remains intact.
- Blind pre-registered-type finding: high, patch. Reproduced default DI selecting the legacy overload when localization is absent; IsCurrentOrigin cancels valid tagged requests without the user accessor. Upgrade type registrations while preserving lifetime.
- Blind keyed-registration finding: high, patch. A keyed-only registration is incorrectly captured as the unkeyed override; filter it out so the default context-aware unkeyed effect remains installed.
- Blind duplicate-registration finding: medium, patch. Replace removes only one descriptor after Fluxor appends another, exposing stale effect activation through enumerable resolution; retain exactly one intended unkeyed effect registration.
- Blind archived-compile-input finding: false. The evidence tree lies outside all project source roots and no tracked explicit Compile Include adds those snapshots; future hypothetical project changes do not demonstrate an existing omitted live source.
- Blind in-flight identity verification finding: medium, patch. Existing new DI test verifies only the initial guard; add a held-loader check to prove stale completion cancels after identity changes.
- Blind release-policy documentation finding: low, patch. The README directly describes the active baseline and planned ledger; update its current coordinates to4.6/v4.7 while retaining generic lifecycle examples.
- Edge pre-registered-type finding: high, same patch. Independent probe confirms a pre-registered type loses user context and cancels valid tagged requests.
- Verification custom-factory-preservation finding: medium, patch. Add direct identity assertions for factory/instance preservation and repeated registration; current tests never exercise the captured-descriptor branch.

## Verification

- Focused Shell constructor/DI, loading/scheduler and Governance tests, built serialized in Debug.
- Playwright FC-NIP browserless contract guards.
- scripts/pack-release-packages.py with real ApiCompat gate, package validators and isolated consumers in CI.
- Exact SHA GitHub CI/Quality status and Release job logs.
- Independent NuGet flat-container/registration checks for every tools/release-packages.json entry.

Release investigation: CI caller digest bcb8098ab7e679a67af47ff40e151a63a1c9dd9e768e697ee5becc674c02edd9; Release caller digest 0ffbe220c4625235b425369ff5660a319bca7d8945ab074cf436fcb3f6508166; Evidence caller digest e3a7050f332ff456e899c8a01d35df58e8a690e6601dfa6390f33bf30acc3491. Recompute before policy edit. Existing source collector supports runtime Builds prefix. Release closure must bind current same-repo release-publish.yml revision, not old 2670339e.

Investigation correction: earlier raw working-tree caller digests included CRLF checkout translation. Exact committed Git blobs already match CI and Release evaluator authorizations. Preserve existing caller rows and workflow bytes unless a real exact-blob mismatch is demonstrated. No extra policy landing/candidate commit needed for caller authorization. Catalog expectation changes remain necessary.

Proven post-release verifier defect: prior Release Evidence run37280565726 artifact11331594936 fails live dependency provenance because release-evidence.yml Builds checkout uses 4eb33928 while manifest/publisher uses360a2b9c4e96809365a7de785be9a68152d5ac28. Align verifier execution checkout with actual release execution pin and retain the verification gate.

Scope refinement from actual gate failure: Parties4cd914030bb595f8149bbdf50721cec845f1f3c5 has redundant root PackageVersion security overrides rejected by unchanged no-inline-version policy. Its selected Builds332b2c144defba546fe0a09626a4a72f2340e22f already pins SignalR.Common, Options and DI.Abstractions10.0.13. Remove only redundant overrides in Parties Directory.Packages.props, preserve patched resolved versions, validate against exact catalog; parent commits/pushes owning Parties change and then advances root gitlink. Do not weaken semantic policy.

Local verification: serialized Debug build0 warnings/errors;184 focused Shell tests passed;47 packer tests passed;22 release-contract tests passed;3 FC-NIP browserless checks passed. Narrow Parties Contracts.Tests fresh Debug restore succeeded with NuGet audit enabled.

Review patches verified:10 DI cases,3 Shell package policy/Quality checks,4 Contracts.UI boundary tests including clean consumer,2 SourceTools diagnostic methods; all19 pass. Four Debug builds0 warnings/errors. Parent release-prepublish/packer60 tests pass. Parties correction rebased onto concurrent upstream work and pushed5efaae7f0120439bc8d942bbc227dabdeefa56d8; root bootstrap4c2e2b114c3c051bddbcdd2e5d0e4ad04c27673a validates51edges/7selectors. Concurrent Cursor commits e3356a8eecbaef5accc6fec30cdd1bc0a64ddee5 and8fecfe8fd6adf8700937bba6c111bfbb434d6074 preserved.

Candidate15577f42be42539b5e917cb7ae6716c2c11ac534 pushed with exact-message and committed-range commitlint success. Exact base8fecfe8fd6adf8700937bba6c111bfbb434d6074 to candidate graph diff passes with0 dependency changes/affected modules. CI39167093574 and Quality39167093330 queued. Parent full CiGovernance class excluding GovernanceBuild passed86/86. Public verification script validated all8 existing4.6.0 packages (listed registration, matching nuspec and downloadable content), ready to verify newly published version. No4.7 publication is claimed yet.

Live Windows negative fixture exposed platform-dependent directory sizes; validator now rejects non-regular screenshot paths before size. Original mismatch assertions retained and fixture proves tampering/removal/directory scenarios. Red test demonstrated against original validator;24/24 pass fixed. Commit1b2b813ddc0377334883c13f7e367a7f198ad4be pushed after exact commitlint/range validation. Current CI39168092556 and Quality39168092281 run this source; primary restore passed and Release build running.

CI39168092556 exposed CP0006 for new abstract ISurfaceAnnouncementCoordinator.IsCurrent vs published4.6.0. Preserved concurrent Cursor commit cdc182825502c13616416b484c490bd87fd650a0 restoring exact published six-member interface and retaining identity on concrete coordinator. Read-only review decompiled actual4.6 package and found sole broken interface call in unused test decorator member. Removed that member in17bd7d891176f23e49b243bfdd7d495f5e301b53; Debug build0warnings/errors and3 focused decorator/dispatcher/identity tests pass. Exact-message and range commitlint successful, fetched and checked0behind/1ahead then pushed. Current CI39168987881 and Quality39168987691. Full8-package compatibility/consumer check pending.

All8 local package/API compatibility checks and metadata validations pass against4.6.0. Both isolated package-only consumers build0warnings/errors. Evidence /tmp/fc-compat-4.6.wJTmmo/{pack.log,pack-plan.json,package-metadata.log,consumers.log,workspace-state.txt}. These local packages include pre-existing Testing WIP and are not the canonical release candidate; CI exact committed source remains authoritative.

Exact-source CI39168987881 succeeds on17bd7d891176f23e49b243bfdd7d495f5e301b53, with successful dependency governance and AD-13 handoff artifact12151862504. Live main still exact candidate; no competing active release. Dispatched release.yml run39169591314 at2026-10-18T21:50:22Z. Quality39168987691 remains running; Epic9 live proof succeeds.

Quality follow-up: story validator lost Commit Scope Dispositions scaffold in BMAD6.12.4 refresh. Restored exact prior978175a8 seven-line section in four .agents/.claude build/build-auto template mirrors;204tests pass with2existing skips. Cancelled Release39169591314 before any production approval/publication.

Clean canonical17bd worktree exposed next Gate2c stale successor tuple. Updated only current SUCCESSOR constants, Quality dispatch/final validator literals, exact-target governance assertions and active operator-guide examples to already-selected EventStorefcf78d6cafb6c0c9f52dc350a9a09aff54ded288/package3.125.1/Builds5c04dab754e269f52306b9582cacd7cf1dc1540f. All ACTIVE fields, sealed v3 bytes6dc9aaa586cf35531de112bd68dd4d724a81d7ad76a11930684e8e9fe6c98892 and open migration approval retained; no v4 record created.292runtime/AppHost Python tests, focused exact-target governance test and serialized Debug build pass.

Remaining canonical default tests found only two stale Counter snapshots (sole added role=grid) and SourceTools packaged-analyzer Fluent constant5.0.0→5.0.1. Narrow repairs pass their3focused cases including actual packaged Debug/Release consumers.5526default tests covered and3contract pacts pass. Full Docs gate passes eight snippets, Release solution build, API summaries, DocFX site and fingerprints.

Windows job117797982912 completed with251passed/7failed: C3 wrapping assumes platform monospace metrics, and six old visual baselines. Width130px→6ch preserves actual2fragment/shadow/contrast assertions and passes Chromium/Firefox/WebKit3/3.50MiB artifact cap retained: failed21attempt diagnostics account for50.59MiB, remaining results12.60MiB. Independent all-six-image review refuses baseline refresh until specimen CSS regression is repaired: blanket anchor/summary inline-block rule overrides density control inline-flex/minima and native disclosure marker. Correct that rule, preserve24px target floor, add cross-browser semantic/geometry regression, then obtain genuine hosted captures. All six current hosted attempts are stable but are not accepted baselines because of those defects. No comparison tolerance or accessibility assertion waived.

Further repairs: retry/degraded browser expectation still asserted aria-live=polite on visual pending summary despite central announcement ownership. Real red reproduction then fixed semantic assertions verify visual summary/prompt off, exactly one polite command status owner, terminal confirmation and redaction. Chromium fullspec2/2 and focused3engines3/3 pass. Fourteen-file batch also fixes actual specimen CSS override (24px standalone links, density controls retain32/40/48px flex centering, native summary marker), with21real browser cases across3engines passing after demonstrated red checks.

Linux combined1245-case job hit30min after~520cases. Proven progressing compound audits exhausted deadlines under4worker contention; split lifecycle/connection transition histories and shell/rail journeys while retaining every assertion and receipt name. Original budgets remain; real Chromium34, Firefox34 and WebKit34 journeys plus capability probes covered by separate truthful invocations. Temporary host outage caused connection errors; fresh owned-host recovery passed unchanged, reports never merged into synthetic authority. Focused packet checks validate actual attachments while explicitly reporting unselected states missing. Shared host stopped after probes.

Linux workflow now exact3-engine matrix, fail-fastfalse, full specs perengine,2workers,30min retained, selected-engine complete evidence binding, engine+attempt artifact names. Guard rejects old workflow and passes new,55helpers/negativefixtures+typecheck+actionlint pass. Two incremental independent reviewers find no additional actionable gaps. Concurrent Cursor committed our reviewed two grid snapshots6ea3352645cee299858811cfc5df633c075191fd; preserved. Concurrent Release39169956435 packed8nupkg+8snupkg4.7.0 but failed those2Shell snapshots, no publication/sealed readiness; all4.7NuGet URLs/GitHubtag/release absent. New stale release39172029555 cancelled after main advanced.

All14owned pending files committed/pushed cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4 after exact full-message/range commitlint, scoped diffcheck, fresh fetch and0behind/1ahead. No user Testing WIP staged. CI39172411432 and Quality39172411080 are current. Raster baselines remain unchanged: fresh hosted captures required for corrected specimen layout before accepting image refresh.18temporary Linux captures at /tmp/frontcomposer-local-corrected-captures are capture-only and not committed comparison evidence.

CI39172411432 green oncf5fe8da includingdependencygovernance. Quality39172411080 passes restored204storyvalidator and otherGate2b checks, exactsuccessor preparation passes and live providerverification running. Reused concurrent valid Release39173030800 forcf5fe8da rather than dispatchduplicate; verify-source and plan passed. Productionprepare approval issued with existing authorized reviewer via pending-deployments API, deployment7161830480, no protection config changes. Main remains exactcf5fe8da. Keep remote main stable until publication; accept current-source hosted baseline refresh afterward as test-only changes, package implementations unchanged.

CF release39173030800 prepare-candidate succeeds: sealed4.7.0 packages8nupkg+8snupkg,5958valid tests0failure, consumer metadata/boundary checksvalid. Downloadedartifact12153711181 andverifiedall29prepared-candidateSHA256filehashes, exactCFsource/version. Readinessfallback-approved, publish_authorizedtrue, existingjpiquotattestationfallbackunexpired2026-11-04. Productionpublisherapprovedasuserauthorized, deployment7161948290; repositoryprotectionsunchanged. ActualGitHubrelease/NuGetpublicationnotyetclaimed.

CF Quality39172411080 completed: Chromium443pass6visualfail1existingskip;Firefox422pass6visualfail22conditional skips;WebKit412pass16fail(6visual+10AE04samezero-sizegridcell),22conditional skips;Windows287pass6visualfail;Epic9liveproofgreen. BuildproviderqualificationpassedthenAppHosthealthreadinessstatus0failed, tailgatesnotreached. DedicatedagentsdiagnosingAppHostandWebKittargetinventory. Hosted24captureprovenancematchCFandrun,each3attemptsbyteidentical,expectedmatcholdtrackedbytes. Baselinevisualreviewinprogress; noimagesrefreshedyet.

Immutable GitHub Release v4.7.0 actually published at 2026-10-18T22:57:16Z, release id416759469, all29 assets present, lightweight tag resolves exact cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4. PublicNuGet checker at22:57:35 still reports indexing pending for all8 versions; no NuGet success claimed yet. All24 independent visual reviews approved; copied matching hosted PNGs and appended platform-specific before/after rationale. Visual governance passes all24, exact candidate commitlint passes; images uncommitted pending release verification and any real projectionCSS fix.

Release39173030800 completes SUCCESS, including immutable exact-source publication gate and AD15 handoff. Independent Release Evidence39174645465 SUCCESS; artifact12154181217 downloaded to /tmp/frontcomposer-release-4.7.0-evidence. Ledger confirms manifest_verified=true, published_bytes_verified=true, repository_signatures_verified=true, partial_publish_incident=false, compliant-candidate. Eight published package contents verified against sealed candidate (repository signatures excluded from byte-content comparison by contract), all8NuGet.org repository signatures valid. Independent fresh public checker confirms all8version indexes, listed registrations, exact nuspecid/version and downloadable packageHEAD200, report /tmp/frontcomposer-nuget-4.7.0-verification.json.

Incremental Quality fixes reviewed and committed:8efb38ff reviewed24hosted baselines+rationale;369443cd pointerinventoryfalse-positive repair (overflow-text Range boundaries, nativehit authorityretained), independent review no gaps,3new+63related+10originalWebKit journeys pass;23fdfaf7 AppHost requestdeadline/redacted diagnostics repair,92real/mocked Python tests pass plusactionlint/py_compile/diffcheck. Root found+repaired diagnostic malformedURL regression; reviewer found+repaired slowheader andcontinuationsecretredaction gaps. None changes packagecode. Exact messages and3commit rangecommitlint pass; fetched0behind3ahead beforepush. Fresh Quality still required to prove hostedAppHost cause and complete allgates.

Push succeeded: remote main23fdfaf7257a1bcb089908b5636c613508dd0a7a. New CI39174995071, Quality39174994696, CodeQL39174995095, Commitlint39174995053. Commitlint alreadygreen. Narrow committed-source successor/artifact/cleanup identity plus contract-pipeline and exactbrowsermatrix governance3/3 pass in cleanunit-only checkout (no canonical runtimeprovenance claim), build0warnings/errors. CI build/unitjob succeeds; dependencygovernance inprogress. All original user Testing WIP and prior user spec remain untouched.

Fresh Quality39174994696: Chromium450pass1existing skip;Firefox429pass22conditional skips;Windows294pass includingordinary24-platform comparisons andartifactvalidation;Epic9passed. WebKit417pass12AE04failures,22conditional skips: old10metadata targets now62x16 band, additional2filterhidden targets62x14.5. All6WebKitvisual comparisons passed. New nativepointerdiagnosis underway againstactualUbuntu24; previouslocal10passes were not proof of hostedresolution. AppHost againfailedbeforeheaders onall4publicHTTP/HTTPS loopback endpoints; perrequest5sdeadline nowworks. All10resourcewaitshealthy,tokenacquired,provider19/19interactionsvalid,health0,commands/query/SignalRunreached;cleanupalltrue. Actualdiagnosticsartifact12153878620retained at /tmp/frontcomposer-23fdfaf-contract-diagnostics with6redaction-scannedJSONfiles. Safeper-eventstartup/state/directtarget diagnostics beingaddedtoidentifyactualapplicationcause; noauthority/healthcheckbypass. NuGet4.7.0releaseandindependentverificationremaincomplete.
