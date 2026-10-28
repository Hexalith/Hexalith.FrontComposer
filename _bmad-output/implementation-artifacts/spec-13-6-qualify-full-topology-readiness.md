---
title: 'Story 13.6: Verify Full-Topology Readiness with Disposable Infrastructure'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5688c3eba98567133285b0f51843d9daa8f24f0b'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/13-6-canonical-platform-provenance-amendment.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 has successful CI/Quality evidence, but its readiness rehearsal starts only EventStore and Counter. The full module graph remains unqualified; default components target shared Redis.

**Approach:** Extend the disposable rehearsal through the ordinary application composition, then verify readiness, authenticated navigation and existing live proof.

## Boundaries & Constraints

**Always:** Keep gateway-only mode as default; require explicit full selection. Run maintenance with the isolated gateway only, verify backup/activation/readback, stop it, then start the full graph with ordinary workers. Reuse EventStore APIs, private realms, lifecycle locking, ownership checks and bounded cleanup. Use Debug/source references. Read Hexalith state guidance before persistence work.

**Never:** Touch shared state, persistent realms or unrelated resources; change package versions, existing dependency Gitlinks, workflow pins, existing JWT/OIDC validation, domain authorization or approved baselines; duplicate platform orchestration; commit/push/publish. The API/BFF caller-policy additions and single canonical Platform root enrollment explicitly approved below are permitted. Qualify only observed disposable topology. Preserve seven completed Owner-reported manual Pass records and waiver. WebKit system paint and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independent.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected behavior | Failure handling |
|---|---|---|---|
| Existing invocation | No topology selection | Existing gateway rehearsal | Preserve existing checks |
| Full selection | Fresh owned infrastructure; activation verified | All ten application resources and nine ordinary sidecars participate; authenticated navigation and existing live proof pass | Missing/unhealthy resource fails |
| Maintenance | Full rehearsal requested | Only maintenance gateway starts before activation | Refuse full writers before readback |
| Isolation | Every component/sidecar/discovery path | Private resources and scoped app IDs; API/BFF sidecars have no state/pubsub access | Shared fallback or ambiguous ownership fails before mutation |
| Authenticated reads | Valid human bearer through either BFF/API; approved GET routes | Original user identity and domain authorization remain enforced; authorized non-stale data/empty result | Missing/invalid bearer, unapproved caller or verb/path fails closed |
| Cancellation/failure | Startup, navigation or proof fails | Owned cleanup; retained sanitized diagnostics | Keep recovery configuration when absence cannot be confirmed |

### Human-approved API/BFF amendment — 2026-10-28

The user approved the prepared amendment in chat. Compose the existing selected `Hexalith.Tenants.Api` host, invocation-only sidecars for it and both UI BFFs, and narrowly scoped Dapr caller policies. Retain the original nine applications: full composition becomes ten applications and nine ordinary sidecars. API receiver policy is deny by default and admits only the existing six GET read routes from `tenants-ui` and `frontcomposer-ui`; BFF receivers expose no invocation operations. Preserve existing OIDC clients, user-bearer forwarding, JWT validation and domain authorization. This explicitly amends the earlier authentication-change prohibition for these caller-policy additions only.

Diagnose Admin's separate bearer-forwarding EventStore query path. If authoritative administrator membership is missing, use only the existing documented delegated-human bootstrap on verified fresh owned state, bind it to the actual private token subject, and verify terminal status plus fresh, non-degraded, version-consistent membership. No fabricated principal, app-ID authorization grant or shared/production bootstrap is authorized. Preserve all other boundaries and the original baseline revision.

### Human-approved Platform/mTLS enrollment — 2026-10-28

The user approved the prepared canonical Platform enrollment. Add only the root-declared `references/Hexalith.Platform` dependency from `https://github.com/Hexalith/Hexalith.Platform.git` at `b742b1b7a496ac72d6961ca6db5213d3df42d234`, reuse its canonical mTLS helper/subscriber without copying or modifying that orchestration, and consume the existing selected Builds.Tooling Runtime source API. This explicitly permits the new `.gitmodules` entry/Gitlink; it does not permit changing any existing Builds/SDK/package pin, initializing nested submodules, relaxing caller policies or publishing.

Map the canonical Platform and existing Builds roots, add the AppHost compile/project references and necessary source/dependency governance enrollment, and reuse private Sentry plus mTLS placement/scheduler/native-sidecar infrastructure. Preserve ten applications/nine ordinary sidecars and default gateway rehearsal parity; mTLS control-plane containers are infrastructure. Require actual SPIFFE-backed admission for approved callers, deny the existing negative cases, retain JWT/OIDC/domain enforcement and continue the approved real Admin/bootstrap/navigation/live proof. Extend exact ownership and private certificate/configuration cleanup, preserving recovery files when safe cleanup cannot be confirmed. Diagnose the identity-lifetime failure using only owned resources; keep every earlier failure at its original attribution.

### Human-approved bounded provenance amendment — 2026-10-28

The user explicitly approved the prepared [canonical Platform provenance amendment](13-6-canonical-platform-provenance-amendment.md) in chat. Implement its closed consumer source projection for Platform `b742b1b7a496ac72d6961ca6db5213d3df42d234`, the two unchanged canonical helper Git-blob hashes and actual root Builds `81ab049208678bd6616828824729c0ddac02f7d1`. Preserve complete raw v1 depth-one/depth-two inventory, all 17 Platform mappings and their deterministic digest. Keep the seven currently untrusted identities globally untrusted, with no nested/source acquisition, initialization, execution or broader trust. Unknown identities outside the exact sealed context and additional Compile/ProjectReference consumption must fail closed. Update the adopted GOV-1 contract/spine and memory log consistently with collection, consumer validation, exact-object acquisition and AD-12 delayed activation; no candidate may authorize its own changed policy.

This approval also permits only `profiles.frontcomposer-catalog-v1.selected_catalog_required_packages.Microsoft.Extensions.Localization` and `.System.Collections.Immutable` to change from `10.0.13` to `10.0.14`, matching the already-selected Builds catalog. These are policy expectations, not actual package/catalog/SDK/Gitlink changes. Preserve every other approved baseline and all existing runtime/authentication behavior. This explicitly amends the earlier approved-baseline prohibition for this exact governance contract and these two expectation fields only. No commit, push or publication is authorized. Preserve the successful disposable runtime proof at its exact recorded inputs; repeat runtime checks only if consumed code changes. Complete governance verification and the parent-managed independent Build review without asking again for this approval.

</frozen-after-approval>

## Code Map

- Canonical Platform at the approved SHA: root `DaprSelfHostedMtls.cs` / `DaprSelfHostedMtlsSubscriber.cs` plus the selected Sentry configuration; read ordinary source only, never skills in references.
- Existing Builds `src/libraries/Hexalith.Builds.Tooling/Runtime/Composition{Workspace,PortAllocator,ToolchainPins}.cs` supplies the required namespace; reference that existing source project. No Builds update is needed.
- Root `.gitmodules`, `deps.local.props`, AppHost project and `eng/dependency-graph-policy.json` / source mapping governance own the new root source dependency; preserve their current package/catalog/workflow selections.

- AppHost directory: `src/Hexalith.FrontComposer.AppHost/`. Retain eventstore, eventstore-admin, eventstore-admin-ui, tenants, parties, sample, tenants-ui, frontcomposer-ui and counter-web; add the existing selected tenants-api host.
- Its `LocalReadiness{Configuration,Topology,HealthCheck}.cs` own private configuration, infrastructure and health checks.
- `eng/verify-isolated-readiness.py` owns maintenance, persisted readback, bearer/Epic 9 helpers and cleanup.
- `tests/e2e/specs/route-contract.spec.ts` supplies reusable navigation/focus patterns.
- Selected Tenants `src/Hexalith.Tenants.AppHost/Program.cs` and its `DaprComponents/accesscontrol.tenants-{api,ui}.yaml`: reuse API/BFF composition, authority/audience, invocation and six GET routes. Read ordinary source under references; never load skills there.
- Selected Tenants `src/Hexalith.Tenants.Api/Program.cs`, UI `TenantsUiServiceCollectionExtensions.cs` and `docs/production-auth-readiness.md`: validated bearer relay and documented delegated-human bootstrap.
- Selected EventStore `src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs`: independent Admin query path through EventStore `api/v1/queries`, not through tenants-api.

## Tasks & Acceptance

**Execution:**

- [x] Root `.gitmodules` and `references/Hexalith.Platform`: enroll exactly the approved canonical source revision using already available local objects; do not initialize nested submodules or change any existing checkout/pin. Keep the canonical source clean. Record only the enrollment metadata in the index if Git requires it; preserve all other staged/user changes and do not commit/push.
- [x] `deps.local.props`, AppHost `.csproj`, source/dependency governance: propagate canonical Platform/existing Builds roots, link the two canonical helper files and reference existing Builds.Tooling source. Enroll the exact source identity and required source-closure/semantic checks without broad trust or catalog/workflow changes.
- [x] `eng/dependency_graph.py`, `eng/dependency-graph-policy.json`, `tests/eng/test_dependency_graph.py`, GOV-1 spine/memory log and owning contract: implement the approved sealed consumer projection and two exact expectation alignments; verify actual committed-object candidate diagnostics, raw inventory/digests, closed consumption, unknown-identity refusal, no unconsumed acquisition/execution, and delayed policy activation. Preserve current runtime inputs and all unrelated external work.
- [x] AppHost `Program.cs`, `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`, `eng/verify-isolated-readiness.py`: reuse canonical private Sentry/mTLS control plane and native sidecars, with exact owned identities, loopback endpoints, credentials/certificate directories outside checkouts, private component scopes and verified cleanup/recovery. Preserve default gateway mode and maintenance-before-writers ordering. Investigate the monitor/identity-lifetime issue without touching unrelated resources.
- [x] `HexalithTenantsApi.cs` (new), `Hexalith.FrontComposer.AppHost.csproj`, `Program.cs` in the AppHost directory: add metadata and the existing Debug/source build-forcing edge for the selected API host. Compose ten applications/nine ordinary sidecars through the ordinary graph, with correct references/waits and explicit owned SDK endpoints. Keep the domain processor for EventStore dispatch and preserve gateway-only mode.
- [x] AppHost `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.tenants-api.yaml` plus BFF policy files (new), `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`: add the approved deny-by-default GET-only caller policies, private registry/resources/placement/scheduler and zero state/pubsub access for new sidecars; retain strict config validation.
- [x] `eng/verify-isolated-readiness.py`: add `--topology full`; preserve gateway maintenance/readback, then verify all full-graph resources through Aspire readiness/direct endpoints with ordinary workers. Run existing rejection/Epic 9 proof unchanged. Refuse inherited isolation escapes.
- [x] `tests/e2e/specs/full-topology-readiness.spec.ts` (new): live-only authenticated combined UI, Tenants and Admin navigation; assert accessible headings/focus and absence of stale/unauthorized content. Runner supplies Aspire endpoints/private credentials; redact secrets.
- [x] `tests/e2e/scripts/verify-isolated-readiness.test.py`, `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs`, live navigation spec: extend ten/nine membership, caller/route/verb/bearer rejection and real data assertions; preserve mode parity, maintenance ordering, unhealthy/missing app refusal, shared-path refusal and cancellation cleanup.
- [x] `eng/verify-isolated-readiness.py`, AppHost `Program.cs` if needed, live navigation spec: diagnose the independent Admin query HTTP/envelope/membership result. Use documented delegated-human bootstrap only if needed on fresh owned state and prove actual status/membership; retain failures until resolved.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, colocated primary `spec-13-6-verify-responsive-and-assistive-accessibility.md`: append candidate, commands/counts and remaining gates. Preserve historical/frozen records; parent/sprint remain in-progress.

**Acceptance Criteria:**

- Given fresh infrastructure, when full rehearsal succeeds, then all ten applications, nine sidecars, actual SPIFFE-backed intended caller admission, authenticated navigation and live checks pass, with persisted activation bound to selected EventStore revision.
- Given full mode is absent, when startup runs, then existing composition/verification regressions pass.
- Given failure/cancellation, when rehearsal ends, then cleanup affects only proven owned resources and incomplete checks cannot qualify.

## Implementation Notes

The parent manages the already-rendered BMAD workflow; the implementation handoff must not rerender or recursively dispatch it. Execute implementation sequentially. Load the approved amendment below as active scope; the later historical "Unapplied amendment proposal" records the pre-approval state only.


Approved and continued on 2026-10-28. Baseline is `5688c3eba98567133285b0f51843d9daa8f24f0b`; the intervening DocFX release-test fix is preserved. Story key is `13-6-i-ux-e-verify-responsive-and-assistive-accessibility`; it and Epic 13 are already in-progress, so sprint synchronization is a no-op.

## Spec Change Log

## Review Triage Log

Independent review of the complete original-baseline diff (`d1f9960d7a279efb286c1844dc63175d0b7649ec68dd482a5749d24c1b7c1975`) finished in all three layers before triage. Each finding was classified separately before grouping. Verification-gap findings V1–V3 are accepted from that layer's filed evidence without repeating its verification. Original baseline and frozen intent remain unchanged.

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| B1 — inherited source roots in affected builds | false | The cited external Compile links and Tooling reference are exclusively in AppHost. The actual active-policy commands select Release/package mode; `.slnx` excludes AppHost in Release, no included project references it, and the executed `_LoadRestoreGraphEntryPoints` target confirms AppHost is absent from restore entrypoints (`/tmp/fc-review-13-6-restore-entrypoints.log`). Direct AppHost property evaluation does not demonstrate the claimed affected-build compilation. Reject. |
| B2 — SDK/restore configuration omitted from consumer seal | medium | `consumer_configuration` seals MSBuild suffixes but omits tracked `global.json` and NuGet configuration, so a later committed consumer could change SDK/restore behavior without changing this seal. Include these effective tracked inputs and test committed mutations; patch. |
| B3 — root NuGet filename missing from runner identity | medium | Actual tracked file is lowercase `nuget.config`; both root input lists spell only `NuGet.Config`. On this POSIX host a dirty edit to the actual file therefore escapes the working-patch identity. Correct the filename coverage and exercise add/edit/remove drift; patch. |
| B4 — inherited startup/restore/import overrides accepted | medium | The existing refusal function accepts `DOTNET_STARTUP_HOOKS`, `RestoreConfigFile`, `RestoreSources` and `ImportDirectoryBuildProps`; inherited environment is forwarded to the real build. Those demonstrated input channels can redirect execution or restore/import inputs before the owned rehearsal. Extend existing refusal keys and tests; patch. |
| B5 — full rehearsal accepts hosts without procfs | medium | `run(full)` requires POSIX only, whereas its mandatory native process ownership/absence checks require Linux `/proc`. A POSIX host without procfs can provision and activate before cleanup refuses. Add the full-mode prerequisite before artifact/configuration creation and test this existing failure state; patch. |
| B6 — post-helper failure leaves unrecorded mTLS directories | low | The canonical helper cleans its own failures, but validation/record I/O after it returns can leave fresh private paths because Program's disposal guard begins later. This is a real rare setup-failure leak before resources start; fixing all such paths needs additional ownership-sensitive exception/cleanup branches. Reject under the low-impact/uncommon-use complexity rule. |
| B7 — post-dispatch bootstrap failures omit worker diagnostics | medium | Dispatch errors capture bounded diagnostics, but rejected/unknown status, timeout and failed projected membership raise outside that capture. Cleanup then removes the worker opportunity while retained IDs correctly prevent redispatch. Extend capture over the existing retained attempt and test those failure phases; patch. |
| B8 — incomplete live ACL path matrix | low | Live checks cover two approved callers, one allowed route, blocked verb/path, outsider and closed BFFs, while static checks inventory all six unchanged canonical GET patterns. Additional descendant/encoded-path live cases are absent, but no bypass is demonstrated. Their incremental diagnostic value is negligible for ordinary qualification and they require new route cases/branches; reject under the low-impact complexity rule. |
| B9 — additional signed-invalid JWT/domain-negative live cases absent | low | New live negatives use absent/malformed bearers; signed wrong-audience/issuer/expiry and unauthorized-user cases are absent. Existing selected API still enables issuer/audience/key/lifetime validation and forwards the original bearer; upstream tests cover the signed-token cases (read only, not claimed as executed here). Adding new signing/realm/authorization fixture machinery for unchanged validation is beyond direct correction for this uncommon verification gap; reject under the low-impact complexity rule. |
| B10 — readiness failure/cancellation regression cases absent | medium | The separate optimistic-reconciliation test change covers readiness success, but not pending readiness failure/cancellation and async disposal. The caller's catch/finally paths exist; the missing regression coverage belongs to the independently published `5af87f9e` feature. Preserve those four external files equal to HEAD and defer. |
| E1 — inherited roots bypass canonical affected-build materialization | false | Same claimed execution path as B1, independently classified: both links live only in AppHost, which is excluded from the active Release build and its actual restore entrypoints. No included project references AppHost. The direct property-override example does not reach the claimed consumer build. Reject. |
| E2 — post-helper mTLS exception orphan | low | Same real post-return private-directory leak as B6, independently classified: no AppHost resource has started and helper-internal failures already clean themselves. Exceptional metadata/validation failure is uncommon; comprehensive safe cleanup adds branches. Reject under the low-impact/uncommon-use complexity rule. |
| E3 — HTTP error body is unbounded | low | Successful responses read at most 1 MiB + 1; `HTTPError` calls `read()` without a limit. A large owned endpoint error can exhaust the runner's memory. Apply the same existing bound and guaranteed close on this branch, with an oversized-error regression; direct correction survives and routes to patch. |
| V1 — no positive materialization integration regression | medium | Pre-verified gap: removing only `materialize_consumer_sources` from `run_affected_builds` leaves all 128 engine tests passing; existing positive affected-build tests have no projection and the projection integration case only refuses before materialization. Add a positive case whose command observes the exact two canonical files before execution; patch as filed. |
| V2 — Parties adapter omits subscription readiness | medium | Pre-verified gap: selected Parties adapter does not supply `WaitForSubscriptionAsync`, while its registration is asynchronous and current tests use synchronous registration. A fast accepted command can miss confirmation and remain Saving. This adapter/contract mismatch belongs to the separate `5af87f9e` feature and unchanged selected Parties source, not this topology implementation; defer to those owners. |
| V3 — Parties fallback stops permanently on connection | medium | Pre-verified gap: shared reconciler invokes fallback once for its lifetime, but selected Parties driver returns on connection and existing tests assert that old contract. A later disconnect can leave an accepted command without fallback reconciliation. This separate adapter/contract mismatch predates this story's implementation; defer independently of V2. |

After individual classification: patch entries are B2, B3, B4, B5, B7, E3 and V1 (seven distinct root causes); defer entries are B10, V2 and V3 (three distinct root causes). B1/E1 are refuted; B6/E2/B8/B9 are rejected low findings. No surviving intent gap or bad-spec entry requires a loopback. Patches add no public surface and address demonstrated input/failure states or the filed integration regression. `review_loop_iteration` remains 0.

## Verification

- Existing Builds source API/canonical Platform compatibility build; exact new root dependency/source-governance checks; owned mTLS positive and negative transport admission before full authenticated acceptance. No plaintext fallback or fabricated caller identity.

- `python3 tests/e2e/scripts/verify-isolated-readiness.test.py`; `npm --prefix tests/e2e run test:epic-9-evidence`; `npm --prefix tests/e2e run typecheck`.
- Build the AppHost and Shell.Tests individually in Debug; run the built Shell test assembly with `DiffEngine_Disabled=true` and `-class '*FrontComposerUiAppHostTests'`.
- `python3 eng/verify-isolated-readiness.py --topology full`: resource/navigation/live checks, persisted readback and cleanup pass. Report exact dependency blockers; do not bypass them through submodule edits.
- `python3 eng/validate-story-artifacts.py`; `git diff --check`. Keep raw logs external; record commands/counts, full revision and existing CI links.


## Current execution and scope blocker — 2026-10-28

Status remains **in-progress**. Candidate is root `5688c3eba98567133285b0f51843d9daa8f24f0b` plus this uncommitted implementation. Selected source revisions are EventStore `b62a5f4b376fbd5994ef811f80d652e7c6e53f54`, Tenants `ec2d21a93cc8591eabdba17b2004dfcc6482b6e3`, and Parties `6ba02654ca390b3333183962355bd5c5857b8224`; no dependency selection changed.

The actual full rehearsal reached all **nine applications Running/Healthy**, all **six sidecar inventories**, healthy direct API endpoints and revision-bound persisted activation. Its three authenticated browser cases then **failed 3/3**. Both signed-in shells displayed the query gateway's unauthenticated/unauthorized surface; Admin displayed an upstream Tenants-unavailable alert alongside an empty heading. Headings and sign-in did not establish authorized data. The unchanged rejection/Epic 9 helpers were not reached in that full attempt. The [owning result](13-6-accessibility-result.md#full-topology-readiness-candidate-and-scope-blocker--2026-10-28) records commands, counts, failed attempts and cleanup.

Implementation interpretation: preserve the existing synthetic Counter in default gateway maintenance; only explicit full rehearsal suppresses it during maintenance. Counter is not an ordinary backend writer. Full browser navigation uses the existing fixed OIDC callback ports under exclusive AppHost ownership and fails occupied ports; default gateway mode and existing helpers retain CLI isolation. Logical Dapr app IDs retain their workload audiences and are scoped by the private registry, placement/scheduler and explicit component scopes.

Disposable full configuration keeps the already-disabled Parties Memories indexing feature disabled and clears only `Parties:MemoriesSearch:{Endpoint,ApiToken,TenantId,CaseId}`. Source-enabled indexing and inherited Memories settings are refused. Verified pristine backup/readback is required before that override; historical state is refused before activation. Local mapping/inventory probes remain active and Degraded still fails. This qualifies no remote Memories capability and changes no ordinary development setting.

The selected Tenants UI client requires Dapr invocation of `tenants-api`. Without its BFF sidecar it falls back to `Tenants:BaseAddress`; this AppHost supplies the owned domain processor, which is the wrong receiver. There is no observed localhost:3500 call on that selected path. A final fail-closed prerequisite now refuses missing API/BFF composition, missing/unmatched SDK endpoints or unowned/default sidecar listeners before reading credentials or starting authenticated navigation. This guard has focused test evidence; the preceding real 3/3 browser failures retain their attribution.

### Unapplied amendment proposal

Approval is needed to extend the frozen nine-application scope and its prohibition on authentication changes. **No missing API host, BFF sidecar or caller policy has been added.**

| Exact files / resources | Proposed bounded amendment |
| --- | --- |
| `src/Hexalith.FrontComposer.AppHost/HexalithTenantsApi.cs` (new); owning `.csproj` | Add source metadata for the already-selected `references/Hexalith.Tenants/src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj`, with `SuppressBuild=true`, and the same Debug/source build-forcing ProjectReference properties as existing child hosts. No package/gitlink update. |
| AppHost `Program.cs` | Add application `tenants-api` and its invocation-only sidecar; retain all nine existing applications. Add invocation-only sidecars for `tenants-ui` and `frontcomposer-ui`, references/waits on the API, and explicit own-sidecar SDK endpoints. Keep the domain processor `tenants` for EventStore dispatch. Application count becomes **10**; ordinary sidecar count becomes **9** (6 existing + API + 2 BFFs), excluding optional fixtures. |
| AppHost `DaprComponents/accesscontrol.tenants-api.yaml` and BFF policies (new); `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`; runner | Compose private discovery/resources/placement/scheduler for all three new sidecars with zero state/pubsub access. Add an explicit deny-by-default API receiver policy admitting only the existing six GET read routes from `tenants-ui` and `frontcomposer-ui`; BFF receivers expose no invocation operations. These are actual caller-policy additions requiring the amendment, not merely a resource count correction. |
| API/security wiring in `Program.cs` | Reuse the selected API host's documented EventStore authority/audience configuration and `InboundBearerForwardingHandler`; relay the authenticated human bearer through the API to EventStore. Preserve both existing OIDC clients, JWT validation and domain authorization. Do not fabricate principals or grant authorization by app ID. Use the owned gateway readiness endpoint. |
| Runner, `FrontComposerUiAppHostTests.cs`, `verify-isolated-readiness.test.py`, live navigation spec | Extend membership/sidecar checks to 10/9, retain maintenance ordering/default gateway parity, and prove the actual API/BFF paths, listener ownership and authorized data/empty surfaces before unchanged rejection/Epic 9 proof. Keep all current failure assertions and approved baselines. |
| Admin diagnosis and optional real bootstrap | Diagnose separately: Admin's existing `DaprTenantQueryService` forwards the caller bearer to EventStore `api/v1/queries`, which dispatches to Tenants. Adding `tenants-api` does not establish that this path is repaired. Capture bounded HTTP/query-envelope outcomes and authoritative membership. If bootstrap is needed, use the existing documented delegated-human bootstrap for the exact private token subject on verified fresh state; retain its actual MessageId/CorrelationId and verify terminal status plus fresh, non-degraded, version-consistent membership before handoff. No bootstrap was dispatched in this continuation. |

Source anchors for the proposal are the selected Tenants AppHost's `tenants-api`/BFF composition and GET-only receiver policy, `Hexalith.Tenants.Api/Program.cs`, `TenantsUiServiceCollectionExtensions.cs`, `TenantBootstrapHostedService.cs`, and `docs/production-auth-readiness.md`; Admin remains on `Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs`. The API/BFF/auth-policy amendment and any resulting production fix remain unimplemented pending explicit approval.

Final focused evidence: Python **48/48**, helper evidence **186 Node + 48 Python**, host selection **51/51**, TypeScript exit **0**; AppHost Debug/source and individual Shell Debug builds have zero warnings/errors. The first real SIGTERM fixture failure is retained; a deterministic Popen-startup cancellation gap was reproduced, corrected and separately verified. Historical reduced-topology and hosted results are not relabelled as current full qualification. Frozen intent, incomplete acceptance tasks, seven completed Owner-reported manual Pass records/waiver and parent/sprint in-progress status are preserved.


### Parent verification — 2026-10-28

The parent inspected the current implementation diff and final command logs. The current nine-resource composition, safety coverage and append-only owning records are implemented; the runner/live-navigation tasks and full acceptance remain incomplete. The parent independently confirmed unchanged historical result/primary text and primary frozen intent, successful story-artifact/diff checks and an empty Aspire process inventory. No independent Build review or completion transition is claimed: Step 3 cannot advance while authenticated navigation and final rejection/Epic 9 acceptance remain unsatisfied. The proposed API/BFF/caller-policy amendment remains unapplied.


### Approved continuation and preserved external changes — 2026-10-28

The user approved the prepared API/BFF/caller-policy amendment and continued Admin diagnosis. The current frozen block and implementation tasks now record that explicit renegotiation; earlier nine-application results and unapplied-proposal text remain historical. No additional approval is needed for the concrete amendment or its necessary reversible implementation choices.

Original `baseline_commit` remains `5688c3eba98567133285b0f51843d9daa8f24f0b`. Resume HEAD is `1ccb4afc47ec8f1bb5379fd98452242546bbd06c`. Four separate external commits update the AppHost SDK/catalog/governance; preserve them and their attribution. Current AppHost SDK is 13.6.4. Root HEAD selects Builds `4250edc94af021945fe072085f02b5122c9b21c3`, while its pre-existing clean source checkout is `f914752cf8223b683799d4b25b4b0f742863297b`; preserve that checkout mismatch rather than updating dependencies. Record actual tested inputs; do not promote old build/runtime/hosted results to this successor.

The working tree already contains the approved prior implementation and its records; this is authorized resumed work, not an unknown dirty-tree blocker. Parent and Story 13.6 remain in-progress, so sprint synchronization is still a no-op. No independent Build review has run yet; complete implementation/acceptance before advancing to the parent-managed review step.

## Commit Scope Dispositions

- `8f7c4906a91387c2caf8d46dddc4d3e6dd0af905` | `shared` | Separate external AppHost SDK update; preserve and exclude from this implementation's authorship.
- `137304d34c9b73fdf3ff0be5beafbe5b34279fc9` | `shared` | Separate external CI governance alignment; preserve unchanged.
- `ccf69d6842b0d420d9e8a512819067f2c6c3a5ec` | `shared` | Separate external shared catalog alignment; preserve its commit and pre-existing working checkout mismatch.
- `1ccb4afc47ec8f1bb5379fd98452242546bbd06c` | `shared` | Separate external dependency-governance correction and tests; preserve unchanged.


### External publication/input reconciliation — 2026-10-28

During the approved continuation, a separate workspace `/pushall` operation published `5f665c4d38afb2255a7dde4620330e09824a40a1`, capturing the prior candidate, approved spec amendment and dependency advances. The working tree was clean at discovery. This assistant did not invoke that operation, create its commit or authorize further publication; preserve it unchanged. Original baseline remains `5688c3eba98567133285b0f51843d9daa8f24f0b`. Review must retain all story-owned changes in the mixed external commit, without attributing unrelated dependency work to this implementation.

The earlier Builds checkout mismatch is superseded. Observed clean selected sources now match root gitlinks: Builds `9f85d8e66e5c16675cee8c224f1fd88430d96007`, EventStore `903174461632e7bcabd6111092aa059f16e1b1c7`, Tenants `750bdc2e8de77595a55bf595c335094cce0d580c`, Parties `3e1dd7cac6ee4db1db48d1bfb478e1b5848f7b8e`. AppHost SDK remains 13.6.4; .NET SDK is 10.0.402. Record actual tested inputs and keep earlier 9/6 health, failed navigation, focused tests and hosted evidence at their original attribution. No dependency checkout/update is needed or authorized by this reconciliation.


### Stable local verification checkout — 2026-10-28

The second amended rehearsal passed maintenance activation/readback but refused full startup because EventStore changed from `903174461632e7bcabd6111092aa059f16e1b1c7` to `36c78f3435d2d492a73f1b0707cf865c8735d740` during the run. Other external source checkouts also advanced; the assistant made none of these dependency changes. Owned cleanup completed. Preserve this failed identity-change attempt.

Use a detached local verification checkout of one captured root commit plus the current story-owned working patch. Materialize only root-declared submodules at that root's exact pinned locally available Git objects, using detached worktrees without network or nested initialization. Do not change main's checkouts, pointers, dependency selections or unrelated work. Perform implementation verification in the stable checkout; record exact root/patch/source attribution and keep the all-input mutation guard. Further story-owned fixes may be copied back only after comparing main's captured file bytes, preserving/merging intervening changes. This reversible isolation is within the approved implementation scope and does not authorize commit, push or publication.

Main at coordination was `5f665c4d38afb2255a7dde4620330e09824a40a1`; its observed source checkouts (not a qualification claim) were Hexalith.Builds `abc0e1be8069d1c7a567515a13bb015adc407536`, Hexalith.EventStore `36c78f3435d2d492a73f1b0707cf865c8735d740`, Hexalith.Tenants `7ddb864cda5e942925b056391617eae4895c1253`, Hexalith.Parties `9239c531f3b0cbb4a998c419e537463b597ff14b`, Hexalith.Memories `72c36c7113c750c8b809f9d72b945a2a8c2d4555`, Hexalith.Commons `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a`. Record the verification checkout's actual selected revisions separately. No further parent record edits will occur during its live proof.


### Approved API/BFF implementation continuation and remaining blockers — 2026-10-28

The current story-owned patch composes the selected API and invocation-only BFF sidecars (10 applications / 9 ordinary sidecars), private six-GET/closed-receiver policies, strict private discovery/component validation, source build edges, API health, endpoint ownership, revision/patch guards and runtime probes. Original user bearer forwarding, OIDC/JWT/domain enforcement, default gateway selection and existing live helper scripts are preserved. No dependency or pin change was made.

Stable detached proof used root `5f665c4d38afb2255a7dde4620330e09824a40a1` plus captured story patch, exact root-pinned locally available source worktrees and no nested initialization/network. Inputs and copy-back hashes are in `/tmp/fc-story13-6-stable-vishqiou/{handoff-inputs,safe-copyback}.json`; each attempt records `source-inputs.json`. Focused evidence is clean AppHost/Shell Debug builds, **55/55** host checks, **186 Node + 54 Python** helpers, and TypeScript exit **0**.

Retain all successor failures: `mtnpkwyd` reached ten Aspire application health waits but failed an incorrect API health-format assertion (corrected to its observed Healthy JSON dependency contract); `e6v7hici` passed maintenance/readback then refused a changed EventStore identity; stable `om2gr6ci` and `g5o2zxr5` lost uniquely owned identity containers before activation, then completed exact owned cleanup. DCP monitor shutdown is temporally recorded; watch was explicitly disabled and no per-run no-watch flag is exposed. Bounded kernel/current-cgroup evidence does not prove OOM or a causal actor. No further full rehearsal was started after the one fresh stable retry.

The completed stateless actual-ACL diagnostic (`/tmp/fc-actual-acl-_daygjl7/transport-result.json`) is transport evidence only: direct receiver 200; both approved GET callers and disallowed caller/verb/path probes 403 with no receiver hits; Dapr records missing SPIFFE identity/global deny on plaintext. Four owned processes exited 0 and private files were removed. Strict policy is retained; intended caller admission remains blocked.

Canonical capability research identifies the selected Platform `b742b1b7a496ac72d6961ca6db5213d3df42d234` helper/subscriber for private Sentry, mTLS placement/scheduler, issuer credentials and native sidecars. FrontComposer declares no root Platform dependency. Exact source inspection confirms existing Builds `9f85d8e66e5c16675cee8c224f1fd88430d96007` already supplies all Tooling.Runtime types/members used by the canonical helper; the earlier incomplete-search absence claim is corrected. No Builds update is established or proposed, and Platform's historical Builds pin must not downgrade Aspire 13.6.4. The remaining enrollment amendment is the canonical Platform source surface, followed by integration compatibility verification using current pins. No source was copied, dependency initialized/updated or policy relaxed. Proposed ordinary counts remain ten/nine; Sentry is additional infrastructure and replaces the plaintext control-plane pair. Exact files/resources and evidence are appended to the [owning result](13-6-accessibility-result.md).

Full sidecar/API boundary/navigation acceptance, real independent Admin HTTP/envelope/membership evidence and unchanged final rejection/Epic 9 proof remain incomplete. No authoritative membership absence or bootstrap delivery was established; no bootstrap was dispatched. Keep incomplete task boxes unchecked and status in-progress. Preserve all earlier historical/frozen/manual/waiver records and independent gates. The parent-managed independent Build review/completion transition has not run.

Final safe realm-loss/status logging refinements were verified by focused/helper checks after the stable runtime attempts; their input manifests retain the actual earlier hashes. Exact static API comparison is `/tmp/fc-amendment-platform-api-comparison.json`; no Builds pin change is required by that comparison or authorized here.


Exact unapplied enrollment proposal: add root `.gitmodules` / `references/Hexalith.Platform` at `b742b1b7a496ac72d6961ca6db5213d3df42d234`; map/propagate canonical Platform and existing Builds roots in `deps.local.props`; link canonical `DaprSelfHostedMtls.cs` / `DaprSelfHostedMtlsSubscriber.cs` in the AppHost and reference the existing Builds.Tooling source project. Reuse private Sentry/mTLS control-plane orchestration, extend ownership/configuration/native-sidecar/certificate cleanup checks and root dependency/source-closure governance, then rerun all incomplete acceptance. **No Builds/package/SDK pin update** is proposed. The owning result supplies the exact file/resource/check table. Separate external root publication `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c` captured candidate files/dependency advances; preserve it and keep stable proof attributed to root `5f665c4d...` plus captured patch. Original baseline remains `5688c3eba98567133285b0f51843d9daa8f24f0b`.

- `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c` | `shared` | External mixed publication captured story files and unrelated dependency advances; no commit/push/dependency update was performed by this implementation. Review must include the prior story hardening already captured in external commits.


### Parent handoff verification — 2026-10-28

The parent independently inspected the actual-ACL result and Dapr denial logs: direct control 200, approved BFF GET requests 403 without receiver hits, and missing SPIFFE identity. This is transport diagnosis, not authenticated user acceptance. The canonical Platform enrollment proposal remains unapplied. Existing Builds Runtime APIs were independently confirmed from exact Git objects, and no Builds/catalog update is proposed. Story validation/diff checks pass and Aspire process inventory is empty. The original baseline and all incomplete acceptance/review status remain unchanged.

After the recorded focused checks, parent cleanup added required XML summaries and Allman formatting to the new HexalithTenantsApi metadata type only. Its project path and SuppressBuild behavior are unchanged; earlier compiled/runtime input manifests retain their exact source attribution. No independent Build review or full acceptance is claimed for this blocked candidate.


### Platform enrollment approved and resumed — 2026-10-28

The user approved the exact prepared Platform enrollment. The frozen exception/tasks above now record that human amendment; earlier unapplied proposals remain historical. Original baseline `5688c3eba98567133285b0f51843d9daa8f24f0b` is preserved. Resume HEAD is `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c`; it selects Builds `3d7f49a5430294da5bcd437cbdace85c62c42027`, while the pre-existing external clean source checkout is `81ab049208678bd6616828824729c0ddac02f7d1`. Preserve both selections/checkout state rather than updating existing dependencies. Package and SDK pins remain unchanged. The API metadata XML/Allman correction from the parent is already present and must be retained.

Continue sequential implementation from the spec without rerendering BMAD. Use a stable detached verification checkout of the actual captured candidate root plus story-owned patch and the newly approved Platform coordinate; avoid reusing older source qualifications as current proof. Only root-declared dependencies may be materialized from existing local objects. Further story-only fixes may be copied back after main preimage checks; external publications/work remain preserved. Earlier 55 host/186 Node/54 Python results and all runtime failures remain tied to their recorded inputs. Parent/Story 13.6 remain in-progress; no independent Build review or completion transition has yet run.

The only newly authorized dependency change is the Platform enrollment above, including its necessary metadata/source-governance integration. Existing runtime/SDK/package versions and existing dependency pointers remain outside scope. No further permission is needed to carry out the approved integration, its required reversible verification setup or the already approved private Admin/bootstrap path.


## Canonical mTLS full topology implementation and remaining provenance gate — 2026-10-28

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


### Bounded provenance amendment approved and resumed — 2026-10-28

The user approved the prepared combined governance amendment. The frozen addition above is the active scope; earlier unapplied proposals and failed strict graph checks retain their historical attribution. Original baseline remains `5688c3eba98567133285b0f51843d9daa8f24f0b`; current main is `c49412ed567ac47e10a1cceec100d2c41505cf45` plus the preserved story working patch. No external commit or dependency selection is to be reset. No further approval is needed for the exact contract/policy amendment, its necessary tests and records, or independent Build review.

The full disposable proof at `/tmp/story13-6-isolated-readiness-c2tg0uhi` remains successful development evidence: 10 applications/9 native sidecars, 3/3 authenticated navigation, real completed bootstrap/fresh membership, 11 negative boundaries, four bearer-rejection routes, unchanged Epic 9 proof and owned cleanup. Source/runtime code is already implemented and copied back. Complete the approved governance work sequentially, retaining full raw provenance and global denial outside its sealed context. Keep the parent Story 13.6/sprint in-progress and preserve manual/waiver and independent acceptance decisions. Do not rerender BMAD or recursively dispatch the workflow. The parent still owes the complete uniform diff/task/matrix audit and independent Build review after this implementation handoff; review iteration remains zero.


## Approved provenance implementation verified — 2026-10-28

The user-approved bounded amendment is implemented in the current working candidate. Earlier proposal-only statements above retain their historical attribution. The closed `consumer_source_projections` policy and engine bind Platform `b742b1b7a496ac72d6961ca6db5213d3df42d234`, its `.gitmodules`/all 17 raw mappings/two unchanged canonical C# hashes, the exact consumer Compile links and root selectors/configuration, and existing root Builds `81ab049208678bd6616828824729c0ddac02f7d1` Tooling source. The projection owner must remain evidence-only; only the two canonical source blobs can be materialized into the isolated consumer. Raw v1 graph/envelope shapes remain unchanged. Seven inventory-only identities remain globally untrusted, without resolution, acquisition, nested initialization, materialization or execution; unknown identities elsewhere and extra/indirect consumption refuse before candidate execution.

Collection, acquisition, affected-build validation and live handoff/release consumers enforce the same gate. Supplied depth-two rows and catalog evidence are recomputed against committed owner objects before accepting a projection report; a structurally valid, rehashed forged envelope cannot authenticate changed inventory. The committed configuration seal includes project/props/targets/proj/slnx inputs plus automatic `.user` imports and `.rsp` response files. Negative fixtures add a project `.csproj.user`, root/nested `Directory.Build.rsp` and `MSBuild.rsp` without changing the consumer project, and verify Git-only refusal before execution. The SDK's actual automatic import is confirmed at `/home/administrator/.dotnet/sdk/10.0.402/Microsoft.Common.CurrentVersion.targets:30`.

The only package-expectation changes are FrontComposer `Microsoft.Extensions.Localization` and `System.Collections.Immutable` **10.0.13→10.0.14**, matching selected 81ab. Global trust, every other policy value, actual package/catalog/SDK/Gitlinks and consumed runtime code are preserved. The adopted GOV-1 [spine](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md), its [memory log](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/.memlog.md) and [owning contract](../contracts/shared-catalog-dependency-governance-2026-07-19.md) record the approved closed mechanism and AD-2/AD-6/AD-10/AD-12 limits without claiming a new Architect sign-off or closing independent Product/Release gates.

Sequential checks pass: dependency engine **126/126**, dependency handoff **11/11**, release-evidence consumers **15/15**. This includes exact owner/hash/inventory/configuration drift, forged raw/catalog/affected proof, evidence-only scheduling, two-file materialization, globally unknown identities, acquisition boundaries, exact 14/wrong/old/missing semantic rows and delayed-policy tests. Logs are `/tmp/fc-mtls-final-gad9h52g/governance-full-tests-auto-inputs.log`, `governance-handoff-consumers-auto-inputs.log` and `governance-release-consumers-auto-inputs.log`; earlier failed fixture/test attempts are retained alongside them.

Real committed-object component diagnostics pass for canonical seals and raw inventory: **69 total edges / 17 Platform edges**, graph digest `9a9308069b9a82a7e8acba6a1980aeea069a499356b2fcc80b71d35fb66049a1`. Platform's unconsumed nested Builds `51a71cb1cfe860d5fc135eb3b8521fd80c413810` retains raw catalog SHA-256 `95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6`; actual helper source remains root 81ab. Missing trusted exact metadata still refuses; no inventory-only objects were acquired. Current working consumer bindings/configuration match approved seal `4c4ecad2dd317b30734b8db55cd60604ab8a481b26e429e57d16c8c2f227ff8b`, and both exact 14 expectations match the actual selected catalog. Evidence and reproducible read-only commands are `/tmp/fc-mtls-final-gad9h52g/governance-actual-object-diagnostics.json`, `governance-actual-raw-inventory.json`, `governance-consumer-configuration-diagnostic.json` and `verify-governance-actual-objects.py`.

These are **component/working-candidate diagnostics**, not active-policy or release acceptance. Strict explicit approved-policy validation of committed `c49412ed567ac47e10a1cceec100d2c41505cf45` correctly refuses its older committed consumer configuration. Committed-policy validation and active-base diff at c494 correctly retain the old policy's unknown-Agents refusal. The dirty configuration/policy cannot authorize itself: owner landing and a later change selecting that base are required by AD-12. Positive fixture acceptance proves the later-base path; no actual-root commit, synthetic release claim or policy activation was created. These expected diagnostics do not require broader approval or another runtime rehearsal.

The successful `/tmp/story13-6-isolated-readiness-c2tg0uhi` runtime remains attributed to its original source manifest. No consumed runtime code changed in this governance amendment and no runtime was repeated. All nine dependency revisions and 33/34 explicitly listed source files byte-match main; the pre-existing `HexalithTenantsSample.cs` CRLF main bytes versus LF stable/committed bytes have identical normalized content and a clean Git diff, and main bytes are preserved. Post-proof governance/ledger metadata is distinct from that manifest. Original baseline 5688, external publications/work, seven Owner-reported manual Pass records/waiver and independent gates remain preserved. Parent-managed full diff/task/matrix audit and independent Build review remain pending; status stays in-progress and implementation does not mark the parent's task boxes.


## File List

These paths contain the complete Story 13.6 implementation/approved governance work since original baseline5688, including story changes captured by mixed external publications. Shared history within a listed file retains its earlier recorded attribution; a path listing does not claim every intervening hunk or external commit. The separately classified external paths below are preserved without story authorship.

- `.editorconfig`
- `.gitmodules`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`
- `_bmad-output/implementation-artifacts/13-6-canonical-platform-provenance-amendment.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`
- `_bmad-output/implementation-artifacts/spec-13-6-qualify-full-topology-readiness.md`
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/.memlog.md`
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`
- `deps.local.props`
- `eng/FrontComposer.SourceReferences.targets`
- `eng/dependency-graph-policy.json`
- `eng/dependency_graph.py`
- `eng/verify-isolated-readiness.py`
- `references/Hexalith.Platform`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.frontcomposer-ui.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.sample.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.tenants-api.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.tenants-ui.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.yaml`
- `src/Hexalith.FrontComposer.AppHost/DaprComponents/sentry.yaml`
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- `src/Hexalith.FrontComposer.AppHost/HexalithTenantsApi.cs`
- `src/Hexalith.FrontComposer.AppHost/LocalReadinessConfiguration.cs`
- `src/Hexalith.FrontComposer.AppHost/LocalReadinessMtls.cs`
- `src/Hexalith.FrontComposer.AppHost/LocalReadinessTopology.cs`
- `src/Hexalith.FrontComposer.AppHost/Program.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs`
- `tests/e2e/scripts/verify-isolated-readiness.test.py`
- `tests/e2e/specs/full-topology-readiness.spec.ts`
- `tests/eng/test_apphost_dependency_modes.py`
- `tests/eng/test_dependency_graph.py`

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/spec-ci-scoped-catalog-qualification.md` — Unrelated external CI/catalog qualification record; preserve its published attribution.
- `references/Hexalith.Builds` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `references/Hexalith.Commons` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `references/Hexalith.EventStore` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `references/Hexalith.Memories` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `references/Hexalith.Parties` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `references/Hexalith.Tenants` — Accepted submodule drift from preserved external publications; no existing dependency update is authored by this implementation.
- `src/Hexalith.FrontComposer.Contracts.UI/Freshness/FcOptimisticReconcileRequest.cs` — Unrelated optimistic reconciliation change published in external 5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d; preserved.
- `src/Hexalith.FrontComposer.Shell/Services/Feedback/FcOptimisticReconciler.cs` — Unrelated optimistic reconciliation source published in external 5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d; preserved. The separate story census update records its existing suppression only.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — Unrelated external CI governance correction; preserved without story authorship.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/DelayedReconcileSubscription.cs` — Unrelated optimistic reconciliation fixture from external 5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d; preserved.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/FcOptimisticReconcilerTests.cs` — Unrelated optimistic reconciliation tests from external 5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d; preserved.

## Artifact metadata reconciliation — 2026-10-28

The first stricter per-story artifact invocation exposed missing File List metadata and four historical shared-disposition rows whose classification lacked the validator-required backticks. This metadata-only correction records the complete factual path list and accepted syntax, preserves all classifications/reasons/commits, and leaves task/matrix boxes and independent review to the parent. The original failing log remains `/tmp/fc-mtls-final-gad9h52g/governance-main-story-artifacts-validation.log`; the corrected check is recorded separately. No source/runtime/governance implementation behavior changed.


## Verification guard refinement after parent audit — 2026-10-28

The parent found a concrete identity gap after the successful runtime: the old runner hashed tracked diffs and explicit host/helper inputs but missed other nonignored untracked root source/configuration and ignored automatic MSBuild inputs. A real-Git fixture retained six pre-fix failures for an added Shell `.cs`/configuration and ignored `.csproj.user`/`.rsp`. The minimal guard now hashes relevant nonignored untracked root inputs, tracks automatic `.user`/`.rsp` diffs and refuses ignored automatic project/response configuration in the root and selected source repositories. Git bounds input enumeration and excludes generated/cache trees and nested reference checkouts. Refusal occurs before configuration and AppHost startup; a fresh identity recheck also runs immediately before the first owned start. Default gateway behavior is preserved.

The refined Python suite passes **73/73**, covering untracked add/edit/remove drift, ignored automatic inputs in root/selected source, startup ordering and ignored build/scratch exclusions. Evidence: `/tmp/fc-mtls-final-gad9h52g/runner-untracked-inputs-before-fix.log`, `runner-untracked-inputs-after-fix.log` and `python-tests-refined-input-guard.log`. Read-only `source_inputs` plus stability recheck accepts the actual stable candidate: `refined-runner-source-inputs.json`, `refined-runner-input-attribution.json` and reproducible `verify-refined-runner-inputs.py` under that same evidence root. No current-runner full runtime pass is claimed.

The original c2tg0uhi manifest and its full success remain tied to runner SHA-256 `12d57e83bb7cf59e7dfa37cfd8fed2193f719cc83ec27227967d35fcf34a40ad`; the refined runner is `9802261acdc6f89d8f57ba1d467d033fad4aa4f5cd7a69172351b99213f5b1bb`. All nine source revisions, native daprd and **27 listed product inputs** are unchanged; **33/34 listed files match the stable proof**, with only the runner changed. In main, **32/34** are byte-identical because the pre-existing Sample CRLF/LF exception also remains preserved and normalized-equal. The refined untracked map contains three existing source/configuration entries, each already explicitly hashed in the old manifest. Approved governance/guard/test changes alter the working-patch digest; they do not change AppHost/service code or binaries. The parent explicitly accepted focused/read-only verification for this identity/refusal-only correction and directed no new full rehearsal. Historical helper/runtime results retain their original attribution; the new guard has separate evidence. Parent audit/task/matrix and independent Build review remain pending.


## Root evidence authentication refinement after parent audit — 2026-10-28

The parent identified a concrete new-primitive boundary: rehashed supplied depth-one rows could omit the Platform selection or disagree with actual root gitlinks/mappings, and root Builds catalog evidence was not independently authenticated. Validation now recomputes supplied root direct rows from actual committed root `.gitmodules`/gitlinks **before any selected-owner/skip path**, and verifies raw root Builds catalog bytes/digest/null marker alongside the existing nested metadata checks. A legitimate authenticated older root without Platform still receives no projection authority. These changes affect governance inspection only; AppHost/service/runtime code and binaries remain unchanged.

The final engine suite passes **128/128**, including structurally valid rehashed omitted-owner, actual-root-target/mapping and root-catalog forgeries through affected-build validation with no materialization/execution, plus a legitimate older-root case. Final consumer reruns pass handoff **11/11** and release-evidence **15/15**. Logs: `/tmp/fc-mtls-final-gad9h52g/governance-full-tests-root-evidence.log`, `governance-handoff-consumers-root-evidence.log`, `governance-release-consumers-root-evidence.log`. The pre-fix attempts/fixture failures remain retained, without relabelling retries as an original clean pass. Real-object component diagnostics are refreshed in `governance-actual-object-diagnostics.json` and `governance-actual-object-diagnostics-root-evidence.log`: full raw 69/all 17 and both root 81ab/nested 51 catalog evidence remain intact; current working seals/14 expectations pass their diagnostic components. Strict committed c494/old-active-base refusals retain their previously documented expected AD-12 limitations; no actual-root commit or candidate self-authorization is created.

The refined identity/refusal guard separately retains Python **73/73** and read-only actual-candidate acceptance. c2tg0uhi remains the earlier runner's successful development runtime proof, not a new-runner full-pass claim. No full runtime was repeated. Guarded copyback, exact hashes and refreshed review patches are external evidence under `/tmp/fc-mtls-final-gad9h52g/`; parent task/matrix audit, independent Build review and independent release/manual gates remain the parent's responsibility.


## Parent task and matrix audit — 2026-10-28

The parent wrote and read the complete uniform original-baseline diff, including all untracked files: `/tmp/fc-parent-13-6-final-uniform-bq_lhwo_.diff.step03-audited.diff` (6,447 lines, SHA-256 `fa9d3573d45c49e6142ce89eada579dfa40e25c818e329cc2bff5ed89677ce17`). All 11 bounded implementation tasks and all three acceptance criteria are satisfied by the implementation and executed evidence below. This task marking does not activate a candidate policy, qualify a release or close the parent Story 13.6/Epic 13 decisions. Independent Build review is next.

| Approved matrix row | Executed passing coverage |
| --- | --- |
| Existing invocation | `test_full_command_selection_is_explicit_and_default_command_is_gateway`, `test_topology_selection_defaults_to_gateway_and_maintenance_never_starts_full_writers`; C# `DisposableReadinessRequiresExplicitFullSelection`. Included in the 73/73 runner and 56/56 host results. |
| Full selection | c2tg0uhi actual 10 healthy applications/9 native sidecars, 3/3 authenticated Chromium cases, unchanged four 401 routes and Epic 9 live proof 1/1; `test_full_topology_refuses_missing_or_unhealthy_application` and `test_full_application_requires_exact_membership_healthy_state_and_ordinary_workers` pass. |
| Maintenance | `test_full_phase_starts_only_after_persisted_activation_readback_and_maintenance_stop`, both activation-readback C# cases and c2tg0uhi native backup/activation/readback prove ordering and ordinary-worker restoration. |
| Isolation | Runner component scopes/API-BFF zero-access, discovery/path/owner/listener/issuer and inherited-override regressions pass; C# shared-component/discovery/Redis/issuer refusals pass. Actual mTLS containers/native channels and cleanup were independently inspected. Refined untracked/ignored-input regressions and actual-candidate read-only guard checks separately pass. |
| Authenticated reads | Actual approved BFF reads 2/2 and negative boundaries 11/11; native real bootstrap Completed plus fresh/non-degraded/version-consistent actual-subject membership; 3/3 live navigation. Caller/route/verb/bearer and stale/degraded/ambiguous/paged-membership unit cases pass. The selected projection page renders the asserted preview only after a terminal ready/empty/stale/degraded query result; the test also rejects stale/degraded alerts. |
| Cancellation/failure | Real SIGTERM/startup-race, owned-stop, native-orphan/drain, uncertain-container and credential-retention regressions pass in the 73-test suite; c2tg0uhi verifies owned AppHost/containers/native consumers/private files absent. Incomplete checks still fail qualification. |

Verification logs are `/tmp/fc-mtls-final-gad9h52g/python-tests-refined-input-guard.log`, `apphost-tests.log`, `governance-full-tests-root-evidence.log`, `governance-handoff-consumers-root-evidence.log` and `governance-release-consumers-root-evidence.log`. The latter three pass 128/128, 11/11 and 15/15; all six matrix rows have executed passing coverage, with no skipped live test counted. Source mode checks 4/4, warning/error-free Debug builds, TypeScript, analyzer contract and the earlier 186 Node helper checks retain their exact recorded inputs.

The successful runtime remains attributed to c2tg0uhi's original runner and source manifest. The refined guard has 73 passing regressions and separate read-only actual-input evidence; its only listed stable-input change is the runner. No new full-runtime pass is inferred. Strict c494 committed/active-base failures are expected AD-12 diagnostics; positive later-base fixtures and actual-object component checks verify the bounded governance implementation without self-authorization.

The parent independently confirmed unchanged original live helper bytes, unchanged historical result/primary prefixes, four external reconciliation files equal to HEAD, and clean canonical Platform b742. The approved source remains unchanged; existing dependency advances/external publications stay separately attributed. Seven Owner-reported manual Pass records/waiver, WebKit paint and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect gates remain preserved.


## Independent Build review and final parent verification — 2026-10-28

All three independent review layers completed against the full original-baseline diff before triage. All **16 findings** have individual evidence/verdict rows in the bounded implementation spec. Seven distinct patch entries are resolved: SDK/NuGet configuration sealing, NuGet filename identity, inherited build overrides, early full-mode procfs prerequisite, bootstrap failure diagnostics through retained status/membership, bounded HTTP error bodies, and a positive affected-build materialization integration regression. The materialization bypass mutation now fails that regression. Three distinct pre-existing optimistic-reconcile follow-ups are appended to `deferred-work.md`; the four externally published reconciliation files remain byte-identical to HEAD. Two claims were refuted against the actual Release build/restore entrypoints; four uncommon/low-value findings were rejected under the workflow's low-impact complexity rule. No intent/spec loopback was needed.

The same implementation agent made the five-file guarded copyback; the parent read its entire 401-line delta and independently checked all five after-hashes. The expanded working-consumer seal is `68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca`, adding only unchanged tracked `global.json` and `nuget.config`. Spine/FC-DEP-1/memory/GOV-1 record this closure consistently. No SDK/package/catalog/Gitlink or canonical source/owner/inventory/selector value changed.

Final parent checks all pass: engine **130/130**, handoff **11/11**, release-evidence **15/15**, helper evidence **186 Node + 78 Python**, dependency-mode suite **13/13**, TypeScript, individual AppHost and Shell.Tests Debug builds (**zero warnings/errors**), and AppHost selection **56/56** with no skips. Story-artifact and diff checks pass. Exact argv, cwd, exit codes and logs are `/tmp/fc-parent-13-6-final-verification/results.json`; corrected per-story validation is rerun after completion metadata.

Independent actual-object diagnostics confirm the expanded working seal, **69 total / 17 Platform raw edges**, exact canonical blobs, root Builds and both approved catalog expectations. Expected pre-activation refusals remain: explicit new-policy diagnosis rejects the old committed consumer configuration; committed-policy/active-base diagnoses reject the globally untrusted Agents identity. These diagnostics confer no current strict CI/release acceptance. AD-12 requires the owner landing and a later base; no dirty candidate self-authority is granted.

The original successful c2tg0uhi proof remains **10 healthy applications / 9 native sidecars, 3/3 authenticated UI cases, 11 boundary negatives, four unchanged invalid-bearer checks and one unchanged Epic 9 live proof**, with verified owned cleanup. The current runner hash is `c87d29b43156650ff24e4b9c7926c864577d86dd66be5946d243d5c324da8a4f`; the original runtime used `12d57e83bb7cf59e7dfa37cfd8fed2193f719cc83ec27227967d35fcf34a40ad`. Read-only current-guard acceptance/recheck passes with all nine source revisions/native runtime and 33/34 stable listed files unchanged; only the runner differs. Main preserves its previously documented normalized-equivalent Sample line-ending exception. No consumed AppHost/service code changed and no new full rehearsal is claimed. New input/refusal/error-diagnostic behavior has its own executed tests; original runtime evidence is preserved at its exact inputs.

The bounded build has completed parent audit and independent review. Parent Story 13.6/Epic 13/sprint remain **in-progress** because WebKit system paint and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independent. Seven Owner-reported manual Pass records/waiver and all historical failures remain preserved. No current remote CI, commit, push or publication is claimed.


## Bounded completion — 2026-10-28

This implementation spec is done after parent verification and independent review. The approved independent parent acceptance gates remain open, so the primary Story 13.6 spec and sprint row remain in-progress; the default workflow sprint transition is not applied to this bounded prerequisite. The approved prohibition on commit/push/publication also takes precedence over the workflow default local-commit step. No staging, commit, push or publication was performed.
