---
title: Hexalith.FrontComposer Architecture Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-19
sourceOfRecord:
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/architecture-quality-review-2026-07-04.md
---

# Hexalith.FrontComposer Architecture Planning Source

This document makes the architecture discoverable to implementation-readiness workflows. The detailed brownfield architecture source remains `_bmad-output/project-docs/architecture.md`; this file is the planning artifact that readiness checks should load.

## Architecture Summary

FrontComposer is a source-generation-driven Blazor application framework. A dual-TFM, UI-clean contracts kernel defines the netstandard-safe attribute, communication, registration, MCP, schema, and diagnostic contracts; packable net10-only `Contracts.UI` owns Blazor/Fluent rendering contracts. A Roslyn incremental generator reads annotated domain projections and commands; runtime consumers compose the generated artifacts through the Blazor Shell, MCP server, CLI, and Testing package. Schema fingerprints bind the producer and consumers so drift is detected instead of failing silently.

## Layers

- **Layer 0 - Contracts kernel:** `Hexalith.FrontComposer.Contracts` targets `net10.0;netstandard2.0`; both faces are free of Blazor, Fluent, runtime implementations, and test fakes. It owns attributes, communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts, diagnostics IDs, and UI-neutral seams.
- **Layer 0A - Contracts.UI:** packable net10-only Blazor/Fluent rendering contract assembly. It owns `Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members, and projection slot/template/view rendering contracts under their existing public namespaces.
- **Layer 1 - SourceTools producer:** Roslyn incremental generator. Parse emits pure equatable IR; transform and emit produce generated UI, Fluxor state, registration, and manifests while referencing only the `Contracts` kernel.
- **Layer 2 - Consumers:** Shell directly references Contracts + Contracts.UI and owns runtime options, registries, and Fluxor actions; Testing references Contracts + Shell and owns test fakes; MCP and Schema remain kernel-only; CLI has no project references.
- **External dependencies:** root-declared `references/Hexalith.*` submodules only. Nested submodules are not initialized.

### Shell sublayers

- **Components** owns Blazor render composition and may consume Routing derivations, State snapshots/actions, and application Services.
- **Routing** owns pure route and label derivation. It must not depend on Components, State, Services, or Infrastructure.
- **State** owns Fluxor slices/effects, state-service contracts, mutation coordinators, and the polling scheduler interfaces/lane models consumed by generated views. State may consume Routing and Services, but never Components.
- **Infrastructure** owns external adapters and concrete background orchestration. `PendingCommandPollingDriver`, `ProjectionFallbackPollingDriver`, and `ProjectionFallbackRefreshScheduler` are scoped Infrastructure workers; their State contracts and mutation coordinators remain in State.
- **Infrastructure.Telemetry** is cross-cutting and may be imported by any Shell sublayer. The only retained non-telemetry State-to-Infrastructure exception is `State/DataGridNavigation/LoadPageEffects.cs` consuming the exact legacy `Infrastructure.EventStore.ProjectionSchemaMismatchException` seam (via the `Infrastructure.EventStore` namespace import). Its `IProjectionPageLoader` dependency is a same-layer `State.DataGridNavigation` type, not a cross-layer seam.

The Shell source architecture guard enforces namespace/folder agreement, the State-to-Components prohibition, Routing purity, concrete worker placement, and the explicit State-to-Infrastructure exception list. The dependency direction is render composition/background adapters toward pure derivation and state contracts; no render-layer dependency may flow back into State or Routing.

## Key Invariants

- `SourceTools` references only the `Contracts` kernel and stays netstandard2.0-clean.
- No Blazor/Fluent/runtime/testing implementation types are added to `Contracts`; rendering contracts live in Contracts.UI, runtime options/registries/actions in Shell, and `InMemoryStorageService` in Testing.
- `ProjectionQuery` owns query criteria. `QueryRequest.Create` composes it with transport/cache metadata while HFC0001/CS0618 preserves the v1.12 flattened source and flat JSON compatibility surface throughout 2.x, with removal targeted for `3.0.0`.
- No Roslyn `ISymbol` escapes the SourceTools parse stage.
- Generated output path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is public contract.
- Schema canonicalization pins encoder, sentinel, source-gen context, and ordinal comparison.
- Fluent UI v5 is the UI component system; raw interactive HTML controls are forbidden outside documented carve-outs.
- Shell state follows Fluxor single-writer discipline and scoped-lifetime discipline.
- MCP security fails closed and requires both tenant tool and resource visibility gates.
- EventStore command acceptance is not treated as projection-confirmed success.
- Shared-catalog compatibility is determined from semantic catalog contents and affected-module restore/build behavior at the actual selected gitlinks; hard-coded historical SHAs are not compatibility allowlists.
- **Approved GOV-1 amendment:** `hexalith.dependency-graph.v1` is bounded to exact root gitlinks (depth 1) and the direct gitlinks
  contained in each exact root-selected commit (depth 2). Those identities are release provenance and
  are sealed in the release dependency graph; deeper historical edges require a separately approved schema.
- UX/layout policy is defined by the UX, IA, and route invariants below and projected into the
  canonical `ux-design.md` planning source.

## UX, IA, And Route Invariants

- A bounded context is presented to operators as one **Module** with one primary shell entry and one
  required default **Module Tab**. Primary module-tab routes use `/{module}/{tab}`.
- Projection flyouts are secondary navigation. They may expose projection links but must not replace
  the module workspace or its default tab.
- Generated command pages use `/commands/{BoundedContext}/{CommandTypeName}`. Palette entries and
  projection empty-state CTAs must resolve through the same route family.
- UI uses the centrally pinned FrontComposer/Fluent UI Blazor v5 package and Fluent 2 tokens. User
  journeys and visual states conform to WCAG 2.2 AA, including keyboard, focus, names, roles,
  live-region, reduced-motion, and forced-colors behavior.
- Command transport acceptance is distinct from projection/status confirmation. Lifecycle UI exposes
  `IdempotentConfirmed`, `NeedsReview`, `Warning`, and `Degraded` as well as the core states.
- FC-CNC allows one in-flight local command. A second local submit is not queued or batched; it is
  blocked with localized, accessible feedback that the attempted submit did not run, while the
  original command remains visible and unchanged.
- Default timing contracts are confirming-to-Degraded at `10_000` ms, status polling every `1_000` ms
  for at most `120_000` ms, and exactly one transient Epic 4 retry after `250` ms.

## Shared Catalog Compatibility And Dependency Provenance

> **Approved 2026-07-19:** Administrator ratified the depth-1/2 boundary, canonical contracts, and
> numeric ceilings below as Architect and Release Owner. This amendment supersedes the former unbounded
> complete-reachable interpretation of v1.

Dependency governance uses a **bounded committed-object graph** and separates two concerns that must
not be conflated:

- **V1 boundary:** enumerate every gitlink at the explicit FrontComposer root commit as depth 1 and
  every gitlink in each exact root-selected repository commit as depth 2. Edges below depth 2 are out
  of scope for v1. The 2026-07-19 census (8 root + 32 direct nested = 40) is evidence, not a fixed count.
- **Compatibility:** validate every Builds selector inside that v1 boundary, cache catalog bytes by
  distinct selected commit, load `Props/Directory.Packages.props` from that exact commit, and evaluate
  the selecting owner's explicit semantic package/import/marker profile. A compatible catalog at a new
  commit passes.
- **Provenance:** record the exact repository identity, owner/path edge, 40-hex commit, depth, and Builds
  catalog SHA-256 fingerprint in deterministic review and release evidence.

Pointer-change CI compares the base and candidate dependency graphs and runs the affected module's
supported standalone restore/build gate. Repository resolution is closed-world from the root
`.gitmodules`; graph collection reads explicit committed Git objects, records edges before object-read
or catalog-validation deduplication, and never recursively initializes nested submodules, moves their working-tree HEADs, clones a
candidate URL, or executes candidate-supplied commands.

The graph engine is offline/object-only. CI acquires exact base and candidate objects from the explicit
root repository and FrontComposer-owned approved remote policy into isolated temporary bare stores,
including base-only objects needed to prove removals. A versioned `eng/dependency-graph-policy.json`
owns trusted identity/path mappings, semantic owner profiles, supported module argv/evidence-only
dispositions, and v1 limits. Base/candidate `.gitmodules` are untrusted graph data. For PRs the active
policy is the exact base commit's policy; for pushes it is the non-zero before commit's policy. Both
graphs use that immutable revision and evidence records its commit and raw SHA-256. A candidate policy
change cannot authorize itself: it activates only when it is the base policy of a later change. The
one-time bootstrap requires an unchanged graph, frozen publication, and approval of the exact policy
digest, enforced by the Release Owner-controlled
`HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256` repository variable. Base-policy existence permanently
disables bootstrap after the initial landing. A zero/unavailable before revision may produce
diagnostic/full-affected evidence, but the gate fails and is never release-eligible. The policy is
release-definition and fallback-invalidation material.

The policy has no implicit semantic or build defaults. Every Builds-selector owner maps to exactly one
named semantic profile; every governed target identity maps either to the exact standalone .NET
restore/build argv and solution or to an explicit evidence-only disposition. The seed closes those
registries over FrontComposer plus the eight root-declared repositories; only AI.Tools is evidence-only
because its selected seed commit contains no solution/build surface. Missing identities, profiles,
commands, or dispositions fail closed. The focused spine contains the exact seed matrices.

Every build row runs its exact static `dotnet restore`/`dotnet build` argv in Release/NuGet mode from
an isolated checkout of the candidate owner commit. For edge-bound consumers, CI safely materializes
the bounded regular-file contract tree from the exact selected candidate Builds commit into
`references/Hexalith.Builds`, verifies the catalog graph hash, and never initializes the nested
repository. This supports both catalog-only consumers and current `Hexalith.Build.props` imports.
Materialization rejects unsafe modes/paths and is capped at 16,384 files, 16 MiB per blob, and 256 MiB
total. The Builds repository uses its self-owned tree; a missing or ambiguous binding fails closed.

Affected-module mapping classifies depth-1 changes first. Added/changed root edges build the candidate
target; removed root edges build FrontComposer. Those root changes subsume descendant depth-2 churn, so
removing/replacing a module cannot schedule its nonexistent old owner. Remaining nested changes build
the candidate owner, collapsing to FrontComposer if no candidate owner survives.

V1 edges sort ordinally by `(depth, owner_repository, owner_commit, path, repository, commit)` and use
strict normalized identities, lowercase 40-hex commits, and relative POSIX paths. Builds edges bind the
SHA-256 of raw catalog blob bytes plus a nullable contract marker. The closed envelope contains exactly
`{schema, root, edge_count, edges, graph_digest}` with `edge_count == len(edges)`; root and edge shapes
are closed by edge kind. Parsers reject duplicate JSON member names, missing/unknown members, boolean
integers, and invalid depths before digest verification. The digest material is `{schema, root,
edge_count, edges}` with ASCII-only values, `ensure_ascii=true`, `allow_nan=false`, sorted object keys,
compact comma/colon separators, UTF-8 encoding, and no BOM/trailing newline. The existing outer manifest
seal binds the complete graph object. This is project canonicalization v1, not RFC 8785. Offline
verification checks the closed schema, structure, order, and digest; live verification reconstructs the
graph from the sealed root commit.

V1 requires Git SHA-1 object format. Blob sizes are checked before reads and tree records are streamed
under inclusive byte/edge ceilings; another object format or an exceeded ceiling fails closed. Python
owns the canonical catalog semantic policy and machine result. C# Governance consumes that result and
retains repository-wide MSBuild ownership checks instead of independently encoding catalog semantics.

Missing objects after bounded acquisition fail closed. All depth-1/2 edges, including self/back-references, are recorded; the
fixed boundary guarantees termination. Blob parsing/hashing may cache by Builds repository/commit, but
semantic evaluation remains per selector or owner contract profile and diagnostics retain every owner.

Pull-request evidence uses `github.event.pull_request.base.sha` as the explicit base input and
`github.sha` as the exact merge revision built by primary CI. It records the computed merge-base and
requires that value to equal the event base; otherwise it fails closed. It diffs that event base against
the same merge revision used for collection and affected builds. Push evidence compares
`github.event.before` with `github.sha`; an unavailable/zero base takes the full-affected fail-closed
path. Unchanged graphs build no module.

GOV-1 introduces the top-level `manifest_schema: hexalith.release-evidence.v2`, closed
`dependency_graph`, closed `dependency_policy`, and closed `workflow_provenance` members atomically.
The existing outer seal covers every top-level member except `seal`. V2 fallback approval hashes the
existing definition/package-set inputs together with the dependency graph digest, active policy
SHA-256, and canonical combined CI/release workflow-definition digest. Older manifests are audit-only and always non-publishable; they cannot satisfy fallback and are
never upgraded or resealed in place. Historical ledger bytes remain unchanged.

Hexalith.Builds will eventually expose a semantic catalog-contract version through BUILD-CAT-1.
During migration, consumers validate semantic contents directly and record the computed fingerprint;
an exact fingerprint allowlist is not a substitute compatibility contract. Making the upstream marker
mandatory requires a later separately approved change after supported gitlinks migrate.

Decision record: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`.
Focused spine: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.

## FR-24 Release Evidence Architecture

Release authorization is an exact-artifact pipeline:

```text
Pack once
  → validate inventory, tests, and package consumers
  → generate SBOM and symbol evidence
  → sign and timestamp the exact .nupkg files
  → verify signatures and timestamps
  → checksum packages, symbols, and evidence
  → seal and verify the release manifest
  → classify-release --require-publishable
  → publish those same authorized bytes
  → verify published NuGet and GitHub assets
```

Pre-publication authorization and post-publication verification are separate phases. Only the former
may authorize publication. Rebuilding, repacking, or signing reconstructed packages after publication
does not prove what NuGet received.

The sealed manifest identifies every immutable release candidate by normalized path and SHA-256 hash
and binds the complete defined v1 graph: root gitlinks plus direct gitlinks from exact root-selected
commits. Each dependency edge is normalized and commit-addressed; Builds edges additionally carry the
semantic catalog contract version when available and the catalog-content SHA-256 fingerprint.
Publication consumes those exact paths without rebuilding or replacing an artifact. A blocked
classification, invalid manifest, missing evidence, or `publish_authorized=false` terminates the
release before NuGet, GitHub Release, tag/changelog, or other external publication side effects.

The active immutable base/before policy independently authorizes exact CI, Release, and post-release
evaluator closures. It pre-authorizes local caller blob hashes and literal-40-hex reusable/action
coordinates; runtime sources must project exactly one authorized closure. Candidate workflow/policy
changes cannot authorize themselves and activate through the same delayed two-change rule.

The release caller must pass `github.event.workflow_run.head_sha` as an explicit required 40-hex input,
and the reusable workflow must check out and propagate exactly that revision. The reusable workflow is
referenced by an active-policy-authorized immutable Hexalith.Builds commit, whose identity is sealed with the caller
workflow hash and the CI-selected policy coordinates. The triggering CI run ID is also passed; its
single versioned `dependency-release-handoff.json` artifact is fetched through the read-only GitHub
Actions run/artifact APIs only after repository, workflow, event, branch, conclusion, run ID, and head
SHA match the triggering CI run; its recorded candidate must equal the event `head_sha`. Actual
caller/reusable workflow refs and SHAs are checked against sealed coordinates. The exact policy blob is
then reloaded from its recorded FrontComposer commit and its raw hash/schema revalidated. A deterministic
static source closure includes every conditional or unconditional `uses:` plus recursively resolved local
composite metadata, independent of the runtime path; every transitive non-local `uses:` is a literal
40-hex commit. Action metadata blob hashes are sealed; mutable/dynamic refs, Docker actions, cycles,
unsupported YAML forms, and bounded depth/source/blob limits fail closed. Builds local actions
come from an exact `job.workflow_sha` checkout, never `@main`. Primary CI's reusable workflow and
transitive actions follow the same closure and are carried in the versioned handoff. The current mutable
CI/release `@main` calls and reusable release contract without an exact release-commit input are
non-conforming; the REL-4 publication freeze remains mandatory until that upstream seam and its
FrontComposer tests exist.

Hexalith.Builds issue 17 / BUILD-REL-1 must deliver the exact reusable CI/release inputs, outputs,
runtime-identity checks, static closure, CI handoff, exact-candidate, and always-emitted verification
handoff contracts. Its owner-accepted immutable revision is pending. GOV-1 may implement local graph and
policy work, but Tasks 4/5, story completion, release eligibility, and any unfreeze remain blocked until
that revision and its workflow/action blob hashes are recorded in the active policy.

The handoff's CI evaluator digest is canonical SHA-256 over its exact caller/reusable/action sources.
Manifest CI provenance must copy those sources exactly, project the authenticated run identity, and bind
the raw handoff JSON SHA-256. Offline verification recomputes the CI-only digest and the combined
CI/release workflow-definition digest and rejects any unequal projection.

Release emits an `if: always()` versioned verification handoff for every attempt, authenticated by the
Release run ID/attempt. It carries the original authenticated CI run ID/attempt/raw handoff hash,
exact active-policy projection, candidate, version/tag/release identity, sealed manifest identity,
exact assets, and authorized Release evaluator. The post-release verifier re-downloads both handoffs,
requires their candidate/policy projection to agree even on pre-manifest failure, and derives
its live root from that handoff and manifest, never the second `workflow_run` hop's head/default-branch
SHA, and its own static closure must match the active policy. It cannot green-no-op a failed or partial
attempt; a default-branch-advance race is a required fixture.

After publication, that independent verifier downloads NuGet and GitHub assets, verifies package
signatures, and compares their hashes with the sealed manifest. A mismatch, missing asset, or partial
publication fails the release and creates an incident record; post-publication evidence cannot
authorize a release retroactively. Durable evidence attached during initial GitHub Release creation is
the public evidence chain. Short-retention workflow artifacts are supplemental.

Ownership boundaries:

- **Hexalith.Builds** owns the reusable workflow contract, signing-secret declaration/forwarding,
  minimum permissions, and the upstream semantic catalog-contract version/canonicalization contract.
- **FrontComposer** owns artifact creation, inventory/consumer/test validation, signing, evidence
  generation, dependency-graph collection/verification, readiness classification, publication of
  authorized bytes, and downloaded-artifact verification.
- **Release Owner** owns signing identity, timestamp authority, secret provisioning/rotation, the
  release freeze, exceptions, and partial-publication incident response.

This delivery architecture does not alter FrontComposer runtime, public product behavior, or UX.

## Epic 11 Release Readiness Remediation Program

Epic 11 traces to `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`. The review found no Critical findings, but it identified High and Medium issues in runtime blind spots and architecture boundaries. Planning is organized into four workstreams:

- **Runtime reliability and security:** Stories 11.0–11.5 are done; 11.18a is in review.
- **Adopter testing and route integrity:** Stories 11.6–11.7 are done and consume Epic 10 evidence
  where referenced.
- **Contracts and package boundary:** Story 11.8 and Stories 11.11–11.14 are done; they are retained as
  decision/delivery history, not queue candidates.
- **Maintainability and enforcement:** Stories 11.9, 11.15–11.16, and 11.17a are done;
  11.17b–d, 11.18b–c, and 11.19a–d are in review. Stories 11.20–11.23 are sequential,
  separately approval-gated backlog phases materialized by the approved Story 11.19d analyzer
  decision; Story 11.23 is a v1.0 publication gate.

Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents. Logging ownership follows
security/fail-closed (11.18a), then command-lifecycle/projection/polling hot paths (11.18c), then
residual Warning/Error/Critical sites (11.18b). The 2026-07-05 Story 11.8 sign-off approved the kernel
split. Stories 11.11–11.13 implemented the Contracts.UI assembly, ownership relocation, and
composed-query compatibility surface. Story 11.14 completed release inventory, package-validation,
migration, and Release Owner documentation evidence.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e11-contracts-kernel-split.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md`
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`
