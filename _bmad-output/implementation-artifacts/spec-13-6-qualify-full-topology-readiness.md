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

**Never:** Touch shared state, persistent realms or unrelated resources; change package versions, gitlinks, workflow pins, existing JWT/OIDC validation, domain authorization or approved baselines; duplicate platform orchestration; commit/push/publish. The narrowly scoped API/BFF caller-policy additions explicitly approved below are permitted. Qualify only observed disposable topology. Preserve seven completed Owner-reported manual Pass records and waiver. WebKit system paint and OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions remain independent.

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

</frozen-after-approval>

## Code Map

- AppHost directory: `src/Hexalith.FrontComposer.AppHost/`. Retain eventstore, eventstore-admin, eventstore-admin-ui, tenants, parties, sample, tenants-ui, frontcomposer-ui and counter-web; add the existing selected tenants-api host.
- Its `LocalReadiness{Configuration,Topology,HealthCheck}.cs` own private configuration, infrastructure and health checks.
- `eng/verify-isolated-readiness.py` owns maintenance, persisted readback, bearer/Epic 9 helpers and cleanup.
- `tests/e2e/specs/route-contract.spec.ts` supplies reusable navigation/focus patterns.
- Selected Tenants `src/Hexalith.Tenants.AppHost/Program.cs` and its `DaprComponents/accesscontrol.tenants-{api,ui}.yaml`: reuse API/BFF composition, authority/audience, invocation and six GET routes. Read ordinary source under references; never load skills there.
- Selected Tenants `src/Hexalith.Tenants.Api/Program.cs`, UI `TenantsUiServiceCollectionExtensions.cs` and `docs/production-auth-readiness.md`: validated bearer relay and documented delegated-human bootstrap.
- Selected EventStore `src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs`: independent Admin query path through EventStore `api/v1/queries`, not through tenants-api.

## Tasks & Acceptance

**Execution:**

- [ ] `HexalithTenantsApi.cs` (new), `Hexalith.FrontComposer.AppHost.csproj`, `Program.cs` in the AppHost directory: add metadata and the existing Debug/source build-forcing edge for the selected API host. Compose ten applications/nine ordinary sidecars through the ordinary graph, with correct references/waits and explicit owned SDK endpoints. Keep the domain processor for EventStore dispatch and preserve gateway-only mode.
- [ ] AppHost `DaprComponents/accesscontrol.tenants-api.yaml` plus BFF policy files (new), `LocalReadinessConfiguration.cs`, `LocalReadinessTopology.cs`: add the approved deny-by-default GET-only caller policies, private registry/resources/placement/scheduler and zero state/pubsub access for new sidecars; retain strict config validation.
- [ ] `eng/verify-isolated-readiness.py`: add `--topology full`; preserve gateway maintenance/readback, then verify all full-graph resources through Aspire readiness/direct endpoints with ordinary workers. Run existing rejection/Epic 9 proof unchanged. Refuse inherited isolation escapes.
- [ ] `tests/e2e/specs/full-topology-readiness.spec.ts` (new): live-only authenticated combined UI, Tenants and Admin navigation; assert accessible headings/focus and absence of stale/unauthorized content. Runner supplies Aspire endpoints/private credentials; redact secrets.
- [ ] `tests/e2e/scripts/verify-isolated-readiness.test.py`, `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs`, live navigation spec: extend ten/nine membership, caller/route/verb/bearer rejection and real data assertions; preserve mode parity, maintenance ordering, unhealthy/missing app refusal, shared-path refusal and cancellation cleanup.
- [ ] `eng/verify-isolated-readiness.py`, AppHost `Program.cs` if needed, live navigation spec: diagnose the independent Admin query HTTP/envelope/membership result. Use documented delegated-human bootstrap only if needed on fresh owned state and prove actual status/membership; retain failures until resolved.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, colocated primary `spec-13-6-verify-responsive-and-assistive-accessibility.md`: append candidate, commands/counts and remaining gates. Preserve historical/frozen records; parent/sprint remain in-progress.

**Acceptance Criteria:**

- Given fresh infrastructure, when full rehearsal succeeds, then all ten applications, nine sidecars, authenticated navigation and live checks pass, with persisted activation bound to selected EventStore revision.
- Given full mode is absent, when startup runs, then existing composition/verification regressions pass.
- Given failure/cancellation, when rehearsal ends, then cleanup affects only proven owned resources and incomplete checks cannot qualify.

## Implementation Notes

The parent manages the already-rendered BMAD workflow; the implementation handoff must not rerender or recursively dispatch it. Execute implementation sequentially. Load the approved amendment below as active scope; the later historical "Unapplied amendment proposal" records the pre-approval state only.


Approved and continued on 2026-10-28. Baseline is `5688c3eba98567133285b0f51843d9daa8f24f0b`; the intervening DocFX release-test fix is preserved. Story key is `13-6-i-ux-e-verify-responsive-and-assistive-accessibility`; it and Epic 13 are already in-progress, so sprint synchronization is a no-op.

## Spec Change Log

## Review Triage Log

## Verification

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
