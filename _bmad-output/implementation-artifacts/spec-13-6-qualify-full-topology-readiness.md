---
title: 'Story 13.6: Verify Full-Topology Readiness with Disposable Infrastructure'
type: 'feature'
created: '2026-10-27'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5688c3eba98567133285b0f51843d9daa8f24f0b'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
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

- [ ] Root `.gitmodules` and `references/Hexalith.Platform`: enroll exactly the approved canonical source revision using already available local objects; do not initialize nested submodules or change any existing checkout/pin. Keep the canonical source clean. Record only the enrollment metadata in the index if Git requires it; preserve all other staged/user changes and do not commit/push.
- [ ] `deps.local.props`, AppHost `.csproj`, source/dependency governance: propagate canonical Platform/existing Builds roots, link the two canonical helper files and reference existing Builds.Tooling source. Enroll the exact source identity and required source-closure/semantic checks without broad trust or catalog/workflow changes.
- [ ] AppHost `Program.cs`, `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`, `eng/verify-isolated-readiness.py`: reuse canonical private Sentry/mTLS control plane and native sidecars, with exact owned identities, loopback endpoints, credentials/certificate directories outside checkouts, private component scopes and verified cleanup/recovery. Preserve default gateway mode and maintenance-before-writers ordering. Investigate the monitor/identity-lifetime issue without touching unrelated resources.
- [ ] `HexalithTenantsApi.cs` (new), `Hexalith.FrontComposer.AppHost.csproj`, `Program.cs` in the AppHost directory: add metadata and the existing Debug/source build-forcing edge for the selected API host. Compose ten applications/nine ordinary sidecars through the ordinary graph, with correct references/waits and explicit owned SDK endpoints. Keep the domain processor for EventStore dispatch and preserve gateway-only mode.
- [ ] AppHost `DaprComponents/accesscontrol.tenants-api.yaml` plus BFF policy files (new), `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`: add the approved deny-by-default GET-only caller policies, private registry/resources/placement/scheduler and zero state/pubsub access for new sidecars; retain strict config validation.
- [ ] `eng/verify-isolated-readiness.py`: add `--topology full`; preserve gateway maintenance/readback, then verify all full-graph resources through Aspire readiness/direct endpoints with ordinary workers. Run existing rejection/Epic 9 proof unchanged. Refuse inherited isolation escapes.
- [ ] `tests/e2e/specs/full-topology-readiness.spec.ts` (new): live-only authenticated combined UI, Tenants and Admin navigation; assert accessible headings/focus and absence of stale/unauthorized content. Runner supplies Aspire endpoints/private credentials; redact secrets.
- [ ] `tests/e2e/scripts/verify-isolated-readiness.test.py`, `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs`, live navigation spec: extend ten/nine membership, caller/route/verb/bearer rejection and real data assertions; preserve mode parity, maintenance ordering, unhealthy/missing app refusal, shared-path refusal and cancellation cleanup.
- [ ] `eng/verify-isolated-readiness.py`, AppHost `Program.cs` if needed, live navigation spec: diagnose the independent Admin query HTTP/envelope/membership result. Use documented delegated-human bootstrap only if needed on fresh owned state and prove actual status/membership; retain failures until resolved.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, colocated primary `spec-13-6-verify-responsive-and-assistive-accessibility.md`: append candidate, commands/counts and remaining gates. Preserve historical/frozen records; parent/sprint remain in-progress.

**Acceptance Criteria:**

- Given fresh infrastructure, when full rehearsal succeeds, then all ten applications, nine sidecars, actual SPIFFE-backed intended caller admission, authenticated navigation and live checks pass, with persisted activation bound to selected EventStore revision.
- Given full mode is absent, when startup runs, then existing composition/verification regressions pass.
- Given failure/cancellation, when rehearsal ends, then cleanup affects only proven owned resources and incomplete checks cannot qualify.

## Implementation Notes

The parent manages the already-rendered BMAD workflow; the implementation handoff must not rerender or recursively dispatch it. Execute implementation sequentially. Load the approved amendment below as active scope; the later historical "Unapplied amendment proposal" records the pre-approval state only.


Approved and continued on 2026-10-28. Baseline is `5688c3eba98567133285b0f51843d9daa8f24f0b`; the intervening DocFX release-test fix is preserved. Story key is `13-6-i-ux-e-verify-responsive-and-assistive-accessibility`; it and Epic 13 are already in-progress, so sprint synchronization is a no-op.

## Spec Change Log

## Review Triage Log

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

- `8f7c4906a91387c2caf8d46dddc4d3e6dd0af905` | shared | Separate external AppHost SDK update; preserve and exclude from this implementation's authorship.
- `137304d34c9b73fdf3ff0be5beafbe5b34279fc9` | shared | Separate external CI governance alignment; preserve unchanged.
- `ccf69d6842b0d420d9e8a512819067f2c6c3a5ec` | shared | Separate external shared catalog alignment; preserve its commit and pre-existing working checkout mismatch.
- `1ccb4afc47ec8f1bb5379fd98452242546bbd06c` | shared | Separate external dependency-governance correction and tests; preserve unchanged.


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

- `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c` | shared | External mixed publication captured story files and unrelated dependency advances; no commit/push/dependency update was performed by this implementation. Review must include the prior story hardening already captured in external commits.


### Parent handoff verification — 2026-10-28

The parent independently inspected the actual-ACL result and Dapr denial logs: direct control 200, approved BFF GET requests 403 without receiver hits, and missing SPIFFE identity. This is transport diagnosis, not authenticated user acceptance. The canonical Platform enrollment proposal remains unapplied. Existing Builds Runtime APIs were independently confirmed from exact Git objects, and no Builds/catalog update is proposed. Story validation/diff checks pass and Aspire process inventory is empty. The original baseline and all incomplete acceptance/review status remain unchanged.

After the recorded focused checks, parent cleanup added required XML summaries and Allman formatting to the new HexalithTenantsApi metadata type only. Its project path and SuppressBuild behavior are unchanged; earlier compiled/runtime input manifests retain their exact source attribution. No independent Build review or full acceptance is claimed for this blocked candidate.


### Platform enrollment approved and resumed — 2026-10-28

The user approved the exact prepared Platform enrollment. The frozen exception/tasks above now record that human amendment; earlier unapplied proposals remain historical. Original baseline `5688c3eba98567133285b0f51843d9daa8f24f0b` is preserved. Resume HEAD is `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c`; it selects Builds `3d7f49a5430294da5bcd437cbdace85c62c42027`, while the pre-existing external clean source checkout is `81ab049208678bd6616828824729c0ddac02f7d1`. Preserve both selections/checkout state rather than updating existing dependencies. Package and SDK pins remain unchanged. The API metadata XML/Allman correction from the parent is already present and must be retained.

Continue sequential implementation from the spec without rerendering BMAD. Use a stable detached verification checkout of the actual captured candidate root plus story-owned patch and the newly approved Platform coordinate; avoid reusing older source qualifications as current proof. Only root-declared dependencies may be materialized from existing local objects. Further story-only fixes may be copied back after main preimage checks; external publications/work remain preserved. Earlier 55 host/186 Node/54 Python results and all runtime failures remain tied to their recorded inputs. Parent/Story 13.6 remain in-progress; no independent Build review or completion transition has yet run.

The only newly authorized dependency change is the Platform enrollment above, including its necessary metadata/source-governance integration. Existing runtime/SDK/package versions and existing dependency pointers remain outside scope. No further permission is needed to carry out the approved integration, its required reversible verification setup or the already approved private Admin/bootstrap path.
