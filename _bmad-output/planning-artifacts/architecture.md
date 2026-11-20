---
title: Hexalith.FrontComposer Architecture Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-11-20
sourceOfRecord:
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/architecture-quality-review-2026-07-04.md
---

# Hexalith.FrontComposer Architecture Planning Source

**Tooling ownership.** The September McpCli course correction transferred the target agent-facing presentation to `Hexalith.McpCli`. The subsequent AI.Tools plan retains `frontcomposer inspect` and `migrate` as local compiler tooling. The HTTP MCP library serves existing consumers until its coupled retirement gate is satisfied. See the [compatibility inventory and removal criteria](../../README.md#legacy-compatibility-surfaces).


This document makes the architecture discoverable to implementation-readiness workflows. The detailed brownfield architecture source remains `_bmad-output/project-docs/architecture.md`; this file is the planning artifact that readiness checks should load.

## Architecture Summary

FrontComposer is a source-generation-driven Blazor application framework. A dual-TFM, UI-clean contracts kernel defines the netstandard-safe attribute, communication, registration, MCP, schema, and diagnostic contracts; packable net11-only `Contracts.UI` owns Blazor/Fluent rendering contracts. A Roslyn incremental generator reads annotated domain projections and commands; runtime consumers compose the generated artifacts through the Blazor Shell, MCP server, CLI, and Testing package. Schema fingerprints bind the producer and consumers so drift is detected instead of failing silently.

## Layers

- **Layer 0 - Contracts kernel:** `Hexalith.FrontComposer.Contracts` targets `net11.0;netstandard2.0`; both faces are free of Blazor, Fluent, runtime implementations, and test fakes. It owns attributes, communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts, diagnostics IDs, and UI-neutral seams.
- **Layer 0A - Contracts.UI:** packable net11-only Blazor/Fluent rendering contract assembly. It owns `Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members, and projection slot/template/view rendering contracts under their existing public namespaces.
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
- Tenant scope is part of EventStore request identity. Command, query, subscription, count, and storage
  paths must preserve the same tenant boundary and fail closed when tenant identity is absent or
  mismatched; TEN-SCOPE-1 owns the FR-30 end-to-end proof.
- Shared-catalog compatibility is determined from semantic catalog contents and affected-module restore/build behavior at the actual selected gitlinks; hard-coded historical SHAs are not compatibility allowlists.
- **Approved GOV-1 amendment:** `hexalith.dependency-graph.v1` is bounded to exact root gitlinks (depth 1) and the direct gitlinks
  contained in each exact root-selected commit (depth 2). Those identities are release provenance and
  are sealed in the release dependency graph; deeper historical edges require a separately approved schema.
- UX/layout policy is defined by the UX, IA, and route invariants below and projected into the
  canonical `ux-design.md` planning source.

## FC-NIP Composition Invariants

- `IPendingCommandOutcomeResolver` is the single owner of terminal pending-command application and eligible fresh-row publication. Generated callbacks and infrastructure adapters emit observations; they do not mutate terminal pending state directly.
- Command target metadata is immutable and explicit. A generated descriptor comes only from an explicit command-to-projection declaration; dynamic values resolve through typed `ICommandTargetIdentityProvider<TCommand>`, while an explicitly declared `SameAsSource` mode may copy one pre-dispatch generated source snapshot. There is no ambient-row fallback.
- Exactly one target snapshot is validated before asynchronous dispatch. It records `ProjectionTypeName`, canonical `ViewKey`, exact `EntityKey`, `ChangeKind`, applicable `PriorStatus` / `ExpectedStatus`, and framework-stamped `CapturedAt`. Accepted `MessageId` is associated afterward, and terminal `ObservedAt` never overwrites capture time.
- Terminal materiality is independent of target intent and is closed to `Material`, `NoOp`, or `Unknown`. `NoOp`, `Unknown`, delete, rejected, and needs-review outcomes suppress the indicator; material idempotent confirmation retains the existing eligible ten-second TTL disposition.
- Projection nudges, visible-row diffs, EventStore `AggregateId`, and untyped result payloads are not universal row identity.
- Indicator state is observable. Every effective add/dismiss/expiry/clear/scope mutation invalidates subscribed generated consumers; subscriptions are scoped and disposed.
- Tenant/user scope is enforced before state is read or rendered, not only on the next producer add.
- Active indicator identity is `(ViewKey, EntityKey)` and uses atomic first-wins semantics across duplicate and distinct message IDs; later attempts do not replace provenance or extend expiry.

Story 9.3 approved the successor explicit target-identity contract at
`_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md`. The base record was
created on 2026-07-04, and its decision was approved and the record updated on 2026-07-05; those dates
are distinct chronology, not references to two contracts. That one base decision remains historical
authority. Stories 9.4-9.8 implement and prove these
invariants without changing EventStore lifecycle/status ownership or the Shell dependency direction.

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
>
> **Release integration update 2026-09-09:** The finalized GOV-1 architecture spine is the
> authoritative mechanism record. AD-1 through AD-19 retain their stable IDs; this section is a
> projection and cannot amend their closed schemas, trust boundaries, or implementation gate.

Dependency governance uses a **bounded committed-object graph** and separates two concerns that must
not be conflated:

- **V1 boundary:** enumerate every gitlink at the explicit FrontComposer root commit as depth 1 and
  every gitlink in each exact root-selected repository commit as depth 2. Edges below depth 2 are out
  of scope for v1. The 2026-07-19 census (8 root + 32 direct nested = 40) is evidence, not a fixed count.
- **Compatibility:** validate every Builds selector inside that v1 boundary, cache catalog bytes by
  distinct selected commit, load `Props/Directory.Packages.props` from that exact commit, and evaluate
  the selecting owner's explicit semantic package/import/marker profile. FrontComposer requires each
  governed `Hexalith*Version` property to be present exactly once in canonical self-default form with a
  literal NuGet version, but does not repeat its point value. External-package decisions remain explicit,
  and an internal family advance must survive the exact affected-module Release/NuGet restore and build.
  A structurally valid, build-compatible catalog at a new commit passes.
- **Provenance:** record the exact repository identity, owner/path edge, 40-hex commit, depth, and Builds
  catalog SHA-256 fingerprint in deterministic review and release evidence.

Pointer-change CI compares the base and candidate dependency graphs and runs the affected module's
supported standalone restore/build gate. Repository resolution is closed-world from the active
policy's explicit identity allowlist, including approved depth-2 targets absent from the root
`.gitmodules`; graph collection reads explicit committed Git objects, records edges before object-read
or catalog-validation deduplication, and never recursively initializes nested submodules, moves their working-tree HEADs, clones a
candidate URL, or executes candidate-supplied commands.

The graph engine is offline/object-only. CI acquires exact base and candidate objects from the explicit
root repository and FrontComposer-owned approved remote policy into isolated temporary bare stores,
including base-only objects needed to prove removals. A versioned `eng/dependency-graph-policy.json`
owns trusted identity/path mappings, semantic owner profiles, supported module argv/evidence-only
dispositions, evaluator authorizations, Release Owner logins, attestation capability, and v1 limits.
Base/candidate `.gitmodules` are untrusted graph data. For PRs the active
policy is the exact base commit's policy; for pushes it is the non-zero before commit's policy. Both
graphs use that immutable revision and evidence records its commit and raw SHA-256. A candidate policy
change cannot authorize itself: it activates only when it is the base policy of a later change. The
one-time v1 bootstrap was consumed when the first policy landed on 2026-07-19; base-policy absence is
permanently fail-closed and no bootstrap mode is reachable. A zero/unavailable before revision may produce
diagnostic/full-affected evidence, but the gate fails and is never release-eligible. The policy is
release-definition and fallback-invalidation material. The authenticated CI handoff,
release-verification handoff v3, and manifest v4 record its canonical coordinates and raw-byte SHA-256.

The policy has no implicit semantic or build defaults. Every Builds-selector owner maps to exactly one
named semantic profile; every governed target identity maps either to the exact standalone .NET
restore/build argv and solution or to an explicit evidence-only disposition. Missing identities,
profiles, commands, or dispositions fail closed. `eng/dependency-graph-policy.json` is the sole
executable authority for profile IDs and values, target dispositions/argv, resource limits, and
evaluator authorizations. Architecture owns their closed schemas, coverage invariants, activation rule,
and trust boundary but does not duplicate volatile policy rows. Evidence seals the policy repository,
path, schema version, 40-hex revision, and canonical raw-byte SHA-256.

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

The manifest lineage is `hexalith.release-evidence.v1` (legacy) → `v2` → `v3` → `v4`.
Preparation emits only `hexalith.release-evidence.v4`; v2/v3 acceptance is confined to explicit audit
and verification commands, and no legacy manifest is resealed, upgraded, or made fallback-eligible.
V4 seals the dependency graph, policy, selected quality run, exact attestation-or-fallback projection,
and CI/Release workflow provenance defined by AD-17. The AD-9 fallback request and canonical
`hexalith.attestation-fallback-authorization.v3` are bound to one authenticated Release run/attempt,
candidate, CI handoff, active policy, and fallback digest. A retry or any dependency, policy, workflow,
package-set, expiry, or run-coordinate drift requires new authorization. Historical ledger bytes remain
unchanged.

Hexalith.Builds will eventually expose a semantic catalog-contract version through BUILD-CAT-1.
During migration, consumers validate semantic contents directly and record the computed fingerprint;
an exact fingerprint allowlist is not a substitute compatibility contract. Making the upstream marker
mandatory requires a later separately approved change after supported gitlinks migrate.

Decision record: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`.
Focused spine: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.

**2026-11-19 credential design.** The selected target keeps the protected OIDC job in FrontComposer and pins the Builds actions it executes. The builder and publisher stay privilege-separated. [GOV-1 amendment](architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md#frontcomposer-trusted-publishing-amendment--2026-11-19)

## FR-24 Release Evidence Architecture

Release authorization is an exact-artifact, privilege-separated pipeline:

```text
Secretless candidate builder
  → check out and execute only the exact authenticated candidate
  → pack once; validate inventory, tests, consumers, checksums, SBOM, and symbols
  → upload one run-bound publication-candidate artifact
Protected candidate-free publisher
  → authenticate the raw archive, descriptor, policy, evaluator, plan, and every declared byte
  → mint and verify provenance attestation or validate the run-bound approved fallback
  → prepare, seal, offline/live verify, and classify manifest v4
  → publish only the authorized bytes
Independent post-release verifier
  → verify GitHub assets, NuGet.org repository signatures, normalized package content,
    attempt disposition, and durable evidence
```

Pre-publication authorization and post-publication verification are separate phases. Only the former
may authorize publication. Rebuilding or repacking reconstructed packages after publication does not
prove what NuGet received.

The sealed manifest identifies every immutable release candidate by normalized path and SHA-256 hash
and binds the complete defined v1 graph: root gitlinks plus direct gitlinks from exact root-selected
commits. Each dependency edge is normalized and commit-addressed; Builds edges additionally carry the
semantic catalog contract version when available and the catalog-content SHA-256 fingerprint.
Publication consumes those exact paths without rebuilding or replacing an artifact. A blocked
classification, invalid manifest, missing evidence, or `publish_authorized=false` terminates the
release before NuGet, GitHub Release, tag/changelog, or other external publication side effects.

Push CI on `main` emits the sealed `hexalith.dependency-release-handoff.v1` artifact (spine AD-13):
the push-CI run/attempt, exact candidate, active base policy, candidate dependency graph, and the
active-policy-authorized CI evaluator closure of the 40-hex-pinned `domain-ci.yml`. It is the sole
release-candidate authority. The `hexalith.dependency-release-source.v1` proof CI also emits is a
diagnostic only and never authorizes preparation.

Release is operator-controlled through `workflow_dispatch`. Its unprotected gate requires the dispatch
ref to be exactly `refs/heads/main`, requires the dispatched value to be a lowercase 40-hex commit,
re-reads the live main ref, and selects exactly one completed successful push CI run for that same SHA.
It also requires one completed successful push run of `quality.yml` for that SHA, then fetches only
the run/attempt-named `dependency-release-handoff` through read-only Actions APIs and verifies it
offline and live. Missing, failed,
truncated, paginated, duplicated, or malformed responses fail before the protected job. No tag,
ambient checkout, default-branch value, or later workflow-run head may replace the dispatched candidate.

The selected Hexalith.Builds reusable implements `split-publication-v1` as two fixed jobs defined
directly in one immutable `domain-release.yml`: `build-publication-candidate` and
`publish-publication-candidate`. The builder has no environment, publication credential, OIDC or
attestation authority, or write scope. It may execute the exact candidate only to restore, build, pack,
plan, validate, and produce the closed `hexalith.publication-candidate.v1` artifact. Builder-side
manifest, readiness, or classification output is diagnostic denial evidence only and cannot authorize.

The publisher is the only product-publication actor. It runs under the protected `production`
environment, executes only the active-policy-authorized candidate-free owner closure, downloads the
raw publication-candidate ZIP through the authenticated Actions API, enforces AD-7 before extraction,
and treats every candidate byte as non-executable data. It independently obtains and verifies the
required GitHub provenance attestation, or validates AD-9's approved-unsupported fallback; prepares and
seals manifest v4; completes offline/live verification and classification; and requires
`publish_authorized=true` before its first NuGet or GitHub Release mutation. Attestation minting over
authenticated package digests is evidence registration, not package/Release publication, and cannot
authorize by itself. Author signing, production PFX custody, and RFC 3161 author timestamps are not
requirements; candidates are author-unsigned and NuGet.org repository-signs uploaded packages.

The manifest and exact `builds-execution-sha` bind the selected reusable in the AD-16 lineage rooted at
`a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`. The Builds catalog gitlink is a separate dependency-graph
identity and need not equal any CI, Release or post-release execution pin. A new owner-accepted Builds
revision implementing the split must enter that lineage and the active policy through delayed activation
before FrontComposer selects it.

**Identity authorities and the active caller (reconciled 2026-11-16, Story 17.1).** The
[identity register](prd-addendum-2026-09-08.md#2-dependency-governance-mechanism-moved-from-nfr-13--7--d-11) keeps five identities apart:

1. **Lineage anchor:** the literal `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` above. It is a
   predecessor only.
2. **Execution pins.** The Release pin is held by the `builds-execution-sha` input in
   `.github/workflows/release.yml` and by the `prepare-domain-release@` pin in
   `.github/workflows/release-publish.yml`. `eng/release_contract.py builds` requires those two to equal
   `--approved`, which is the caller's own `env.BUILDS_EXECUTION_SHA`, and names the drifting file and
   field. `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate` compares
   matching 40-hex values across the full set, which adds `env.BUILDS_EXECUTION_SHA`,
   `HEXALITH_BUILDS_EXECUTION_SHA` and the `.hexalith/builds-execution` checkout `ref:`. Its regexes can
   miss an additional non-SHA or `-suffix` member, so it does not enforce every observed value.
   The `release` rows of `evaluator_authorizations` in
   `eng/dependency-graph-policy.json` authorize this non-conformant caller. The CI pin is held by the
   `domain-ci.yml@` pin in `.github/workflows/ci.yml`, authorized by the policy's `ci` rows. The
   post-release pin is held by the `.hexalith/builds-execution` checkout `ref:` in
   `.github/workflows/release-evidence.yml`, authorized by the policy's `post_release` rows.
   Those rows remain non-conformant (NC-7, recorded 2026-09-08). Dated policy snapshot 2026-11-16: all
   seven `post_release` rows name a Builds `domain-release.yml` reusable, while AD-12 requires
   `reusable: null` and caller-reachable sources only. They include the row matching the current
   `release-evidence.yml` blob (sha256 `5cc696fc168000e7d3995a2db3c415761c69e53be21497dffbb5989197a3f524`),
   whose reusable is unreachable because `release-evidence.yml` never calls it. NC-6 also remains open:
   the `release-evidence.yml` verify step reloads no policy and performs no `post_release`
   authorization, so no check consults these rows yet. NC-33 also remains open: verification/ledger
   decisions can execute ambient or candidate helper bytes rather than only the authorized closure.
   Dated snapshot
   2026-11-16: Release and post-release `360a2b9c4e96809365a7de785be9a68152d5ac28`,
   CI `7d2019adf4591a2c320a9288f76fc5f0f6905f65`.
3. **Builds catalog gitlink:** read with `git ls-tree <commit> references/Hexalith.Builds`. Dated
   snapshot 2026-11-16: `65fd22f37b9fad64b595841923cdbe050563ab99`.
4. **Future split revision:** EXT-BUILDS-1. It does not yet exist.
5. **Candidate identities:** each is bound to the evidence that names it. None is selected for G-8.

The active caller is not the AD-19 topology above. `release.yml` delegates to the repository-owned
`release-publish.yml`. Its protected `production` job checks out the dispatched candidate and runs
the Builds `prepare-domain-release` composite action and Semantic Release while it holds publication
credentials. This legacy caller-owned publisher is **not G-8 conformant** (nonconformance register
NC-34). AD-13's evaluator-trust rule assumes a Builds `domain-release.yml` reusable, and this caller
does not use one.

Every authenticated Release run uploads `hexalith.release-verification-handoff.v3` under `if: always()`.
It carries the selected quality run, original CI handoff, candidate, policy, publication-candidate
coordinates, release state, final manifest v4, attestation/fallback, assets, denial reason, and Release
evaluator. The total AD-15 classifier distinguishes gate-frozen, no-releasable, rejected, compliant,
deferred, missing-artifact, partial-publish, and other non-compliant attempts. The
`publication_started` marker is set immediately before the first product-publication mutation;
attestation registration and protected-job start do not set it. External bytes or state inconsistent
with that marker are a partial-publication incident.

After publication, that independent verifier downloads NuGet and GitHub assets. GitHub package bytes
must exactly match the sealed checksums. Because NuGet.org repository-signs an unsigned submission by
adding the root `.signature.p7s` entry, its raw archive hash is expected to differ; the verifier requires
a valid repository signature for every package and byte-equivalence for every other normalized ZIP
member. A mismatch, missing asset, or partial publication fails closed and creates an incident
observation; post-publication evidence cannot authorize retroactively.
`frontcomposer.release-ledger-record.v2` observations are append-only and attempt-keyed: a later green
verification cannot replace or weaken an incident. Successful product Releases carry the mandatory
AD-19 assets. If publication started but no complete immutable product Release exists, the separately
authorized candidate-free incident-recovery stage preserves authenticated and quarantined evidence in
an immutable reserved-namespace prerelease. Run artifacts are short-retention replay material, never
durable authorization.

**Evidence storage (owner decision 2026-10-23).** The evidence policy in PRD §7 applies here and supersedes the 2026-10-21 qualification evidence storage amendment and Stories 15.6/15.7.

Ownership boundaries:

- **Hexalith.Builds** owns the reusable workflow contract, minimum permissions, and the upstream
  semantic catalog-contract version/canonicalization contract.
- **FrontComposer** owns artifact creation, inventory/consumer/test validation, evidence
  generation, dependency-graph collection/verification, readiness classification, publication of
  authorized bytes, and downloaded-artifact verification.
- **Release Owner** owns production environment approval, NuGet publishing credentials, exceptions,
  and partial-publication incident response.

### GOV-1 Adoption And Split Implementation Gate

The current FrontComposer caller still selects a legacy publication-capable path, so the architecture
target is not a production-state claim. Production releases must remain halted, but the halt is
currently not in effect (NC-35, NC-36). `HEXALITH_RELEASE_PUBLISH_ENABLED` must be literal `false` as
a deny-only emergency stop; it can never authorize. No bounded risk exception is currently approved.

**Observed nonconformance (authenticated API read 2026-11-16T15:55:29Z).** The variable reads `true`
(updated 2026-10-05T06:13:40Z); v4.5.0 (2026-09-19) and v4.6.0 through v6.0.0 were published
after the 2026-09-09 halt adoption without an approved exception. Register rows NC-35 and NC-36 route
this to the Release Owner. Incident handling belongs to Story 17.2 and ledger rows to Story 17.14. `true` never authorizes, and neither does a green run
or environment approval. This record changes no variable, workflow or release posture.

The **GOV-1 split implementation gate** is the canonical packet and approval-projection protocol in the
spine's Deferred section. It requires
`_bmad-output/implementation-artifacts/gov-1-split-publication-conformance.json`, authenticated live
checks and five reviewer reports, a closed nonconformance register, an unchanged protected-main
evidence PR with Release Owner approval, and a distinct protected-main approval-projection PR. A stale
spine hash, mixed candidate/Builds/policy/evaluator evidence, unavailable source evidence, any open row,
direct push, or squash/rebase fails the gate.

Unresolved owner decisions remain explicit:

- Product Owner and Release Owner acceptance of AD-19, the production-release halt, and the current
  no-exception posture (PRD D-16/G-8).
- Release Owner or repository-administrator action to set the emergency-stop variable to literal
  `false` and capture an authenticated API observation.
- Hexalith.Builds owner and Release Owner acceptance of the immutable split-reusable revision entering
  the AD-16 lineage.
- Product Owner and Release Owner approval of the incident acknowledgement/containment target and the
  `docs/release-incident-response.md` runbook before integration.
- Release Owner confirmation of the no-bypass protected-main ruleset required for conformance and
  append-only ledger approvals.

This delivery architecture does not alter FrontComposer runtime, public product behavior, or UX.

## Epic 11 Release Readiness Remediation Program

Epic 11 traces to `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`. Stories
11.0–11.9 and 11.11–11.32 are completed delivery history. The rejected 2026-09-10 epic acceptance and
approved 2026-09-12 course correction added Stories 11.25–11.32 as a bounded remediation extension,
and sprint status records the bounded epic as done:

- **Historical identity capture and immediate gates:** Story 11.25 delivered its technical capture;
  Stories 11.26–11.28 repaired analyzer, route-e2e, and FC-NIP documentation gates.
- **Runtime and evidence hardening:** Stories 11.29–11.31 corrected fallback/view registration,
  Testing/MCP boundaries, and shared correlation pseudonymization.
- **Artifact integrity:** Story 11.32 reconciled queue/story evidence and made contradictions fail
  closed.
- **Residual ownership:** Story 11.25 did not grant EventStore migration approval or close G-3.
  E11R-AI-1 is carried by proposed EVT-ID-1/EVT-APP-1. The current Story 11.32 validator red state is
  proposed PLAN-INT-2 work and does not rewrite completed Epic 11 history.

Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents. Logging ownership follows
security/fail-closed (11.18a), then command-lifecycle/projection/polling hot paths (11.18c), then
residual Warning/Error/Critical sites (11.18b). Only their materialized children carry implementable
queue state; Story 11.32 enforces that invariant.

### EventStore Active-Identity Invariant

Story 11.24 (its committed identity record was removed with the evidence on 2026-10-23, PRD §7)
is immutable historical authorization for `bb94d93e… / 3.91.1 / a8a50859…`. The live 2026-09-08
provider/AppHost capture at `059f6a89… / 3.103.0 / 35c3d1e5…` is immutable prior compatibility
evidence. Neither record is projected onto a different Builds revision.

Story 11.25 created the v2 technical record for EventStore source `059f6a89…`, package `3.103.0`,
and Builds `a32cb422…`; it is completed history and explicitly did not claim migration approval. The
in-review v3 candidate targeted package `3.106.0` with earlier EventStore/Builds gitlinks, empty
approval receipts, and `migrationApprovalClaimed=false`. At the 2026-09-22 reconciliation point,
FrontComposer HEAD `b61e51e1…` instead selected EventStore `db1e9d73…`, catalog package `3.106.0`,
and Builds `2fba3497…`. None of the older records is projected onto that later tuple.

EVT-ID-1 therefore records, in its story or spec, the exact FrontComposer commit SHA, EventStore
gitlink, Builds gitlink, and catalog package current at its execution, with the link to a passing
Quality workflow run at that tuple covering Gate 2c provider verification and `epic9-live-acceptance`
(PRD §7). EVT-APP-1 is a separate approval task. It may claim migration approval only after the
EventStore maintainer, FrontComposer maintainer, and Release Owner sign the exact EVT-ID-1 tuple, or after EVT-XFER-1/OI-18
first transfers the EventStore approval role through a dated Product and Architecture decision. A
semantic-compatibility exception and rollback remain unapproved.

No committed identity record or identity validator exists. The release review compares the story's
recorded tuple with the candidate gitlinks and catalog contents; a missing record, tuple mismatch,
failing provider run, or absent required signature blocks the release (G-3). Historical SHAs are not
embedded as unexplained current compatibility constants.

**Approved scheduling rule (2026-11-15).** Reserve one qualification/decision window of at most 24 hours, starting immediately before the separate seal push. Capture within two hours of the required same-attempt green results and schedule the three named migration decisions within that window. A repository-wide hold requires coordination with all active sessions and the workspace automation owner; a session-only hold does not satisfy or remove that prerequisite. Hold unrelated commits and dependency/catalog/policy changes only within the coordinated authority; external automation or coordination requires its owner's explicit authorization. An urgent fix ends the attempt. The Release Owner, with Architect involvement, records a bounded disposition within four working hours of drift, missing authority, failed qualification, timeout or a latest-main lineage conflict: schedule one fresh attempt, reject with corrective work, or request an explicit planning amendment. End the hold at decision or deadline; never extend it silently. G-3 stays open without exact evidence and the required decisions.

**Candidate lineage.** Approval covers only the exact tested commit C. Record/receipt descendants are distinct commits. A latest-main mismatch requires evidence or an explicitly approved planning resolution; no prior receipt transfers automatically. The record commit is not the tested tuple. No documentation exemption, semantic-compatibility exception or release-selector change is authorized.

**Current incident clarification (2026-11-15).** Repository-wide coordination was not achieved; it remains an open Release Owner action with Architect involvement, not a prerequisite silently removed by the session-only implementation. No working-hours calendar was supplied. Use conservative UTC disposition due time `2026-11-15T21:49:35Z`, no later than four elapsed hours after first drift `2026-11-15T17:49:35Z`; this defines no business calendar or named owner receipt and does not extend the outer deadline `2026-11-16T17:27:10Z`. Failed qualification means failed successor required steps; the expected seal-transition refusal under the previous policy is not itself a failed successor qualification. Other approved triggers, roles and deadlines remain unchanged.

### Approved Residual Architecture Handoff

The approved 2026-09-22 course correction keeps these boundaries explicit for downstream story
creation:

| Concern | Proposed work | Architecture boundary |
| --- | --- | --- |
| Exact runtime identity | EVT-ID-1, EVT-APP-1, conditional EVT-XFER-1 | Evidence and approval are separate; tuple drift creates new work. |
| Tenant scope | TEN-SCOPE-1 | Production EventStore seams prove FR-30 end to end. |
| MCP production security | MCP-SEC-1, MCP-SEC-2, MCP-APP-1 | Implementation and negative evidence precede independent sign-off. |
| Adopter proof | ADOPT-KIT-1, EXT-ADOPTER-1 | FrontComposer owns the kit; the adopter owns external execution evidence. |
| Split publication | EXT-BUILDS-1, GOV-B through GOV-J, GOV-ACCEPT-1/2 | Upstream acceptance, implementation, evidence, and owner approval never collapse into one status. |
| Release evidence | REL-LEDGER-1/2, REL-BASE-1, REL-A3-APP-1 | Append-only release truth and decisions remain Release-owned. |

Aliases are proposed handoff identifiers, not final story numbers. Their accepted boundaries are in
`sprint-change-proposal-2026-09-22.md`; create-epics-and-stories assigns numeric IDs.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e11-contracts-kernel-split.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-11.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-22.md`
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`

## Approved EventStore Active-Identity resolution — 2026-11-15

The [three-capacity planning receipt](sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15) adopts all six amendment provisions at `2026-11-15T21:58:49Z`, with jpiquot separately exercising Product Owner, Architect and Release Owner authority. It supersedes earlier pending identity/amendment assessments prospectively; prior records and the Active-Identity history remain intact. Original EventStore authority is available; 16.2 is not invoked and no role transfers.

Keep the tuple actually judged, its record/receipt commit and the current release candidate separate. The accepted C FrontComposer `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` differs from record/main R `695d23149c56a6104e81d23a54116b4fb914df95`, even with identical EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` and catalog default `5.1.0`. C's received migration decisions remain nonoperative, with no later activation, backdating or transfer. R's independent green Quality supplies no C-equivalence, exact R capture or owner decision.

G-3 closes only for a proposed current candidate with its own exact four-part capture, passing same-attempt required live lanes and overall Quality, and three valid named receipts within an authorized window. Descendant record commits establish provenance without an identity exemption. Production's exact-current-main selector, successful push CI/Quality requirements and current-main recheck before protected credentials remain intact. Source execution still proves no package-mode runtime compatibility; existing property/framework/launch/restored-assets, browser-retry and behavioral limits remain explicit.

jpiquot additionally acts as the explicitly named workspace automation owner and attests prospective session/automation coordination from `2026-11-15T21:58:49Z` to no later than unchanged `2026-11-16T17:27:10Z`, ending earlier on decision, drift or urgent fix. No historical global hold, supplied individual roster or independently observed acknowledgment is inferred. As Release Owner with Architect involvement, jpiquot owns current-candidate readiness or a terminal blocker/next-action disposition by that deadline. No automatic recapture loop, new attempt or publication is authorized; any fresh attempt needs its own explicit named schedule of at most 24 hours, evidence and decisions. **G-3 remains open.**

### Review clarification of coordination, lineage and cadence — 2026-11-15

The [review clarifications](sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#review-clarifications--2026-11-15) preserve the approved rule and earlier records. Accept jpiquot's resolved automation-owner identity and coordination attestation from `2026-11-15T21:58:49Z`; provision 4's active-session roster/acknowledgments remain undocumented and required. Hold-ending “decision” means final migration disposition or named terminal blocker disposition, with drift, urgent fix or original deadline `2026-11-16T17:27:10Z` also ending it; the planning approval at hold start does not end it.

The structural record-descendant/current-main cycle remains blocked: committing another capture/receipt advances main, so another capture alone is insufficient. jpiquot as Release Owner with Architect must bound its resolution under unchanged exact-current-main/evidence/receipt and release guards, or record a terminal blocker and next action by that outer deadline. The supplied parent GitHub main and C/R run queries completed by `2026-11-15T21:59:14Z`; separate parent review metadata at `2026-11-15T22:13:43Z` reports R's last required success `2026-11-15T20:57:29Z` and capture deadline **`2026-11-15T22:57:29Z`**. This establishes time-only eligibility at that observation, without capture/receipt transfer, deadline extension, recapture, retry or new-attempt authorization. No new remote check is made. **16.3 remains in-progress; G-3 remains open**, with 16.1 done and 16.2 not invoked/backlog.

## Approved EventStore candidate and record lineage — 2026-11-16

The [three-capacity planning receipt](sprint-change-proposal-2026-11-16.md#planning-approval-receipt--2026-11-16), observed at `2026-11-16T12:23:13Z`, adopts Option A and §§3.1–3.5 of the complete proposal prospectively. It explicitly selects same-repository off-main record storage for a future exact candidate, resolving the storage interpretation without a semantic or documentation exemption. The ended, never-executed M procedure remains history; no old candidate, hold or deadline is revived.

Compare the live main/dispatch/checkout SHA to tested T, then compare T's exact gitlinks and catalog selections with the owning capture and role receipts. The approved record-ref mechanism separates T from Q/S/F storage commits; it grants no commit-equivalence or documentation exception. Human Release Owner review verifies immutable record reachability, actual authority, cadence, evidence and applicability; automated current-main/CI/Quality/handoff/credential controls remain unchanged. Missing human review leaves G-3 open. Adopt the ordering and retention in the approved proposal §§3.2–3.5.

Approval binds the exact FrontComposer commit, EventStore source gitlink, Builds gitlink and catalog-owned EventStore package identity with evaluated per-context selection scope. Other committed inputs remain fixed by T and subject to their independent checks. Capture in the owning 16.1 spec, separately stated role receipts in the owning 16.3 spec, and final human review remain on the separately authorized same-repository record ref; resolve immutable Q/S/F links after commit without embedding a self-reference. Main stays T through consumption, and records stay durably reachable afterward until a separately authorized preservation disposition verifies another durable same-repository reference.

Complete schedule, all-writer/automation coverage and dated acknowledgments, storage/Git authority and capture/decision capacity before starting qualification. Retain the at-most-24-hour window, two-hour durable capture, durable decisions/final review by window end, four-working-hour disposition and hold/stop rules in the approved proposal. A later source/policy/workflow change, including G-8 implementation, creates a different candidate; finish required release-blocking integration before selecting releasable T. No runtime, API, component, schema or topology diagram change is required. New automated G-3 enforcement would be separate work; this adoption creates none.

**Correct-course planning action closed; Story 16.3 in-progress; G-3 open.** Existing evidence/decisions and source/package/runtime limits remain unchanged. Original EventStore authority is available; 16.2 stays done as not invoked with conditional reopening. G-8's halt and all publication gates remain independent; no qualification, Git action or release is authorized by this adoption.
