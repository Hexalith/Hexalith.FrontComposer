---
title: Hexalith.FrontComposer Architecture Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-15
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

The sealed manifest identifies every immutable release candidate by normalized path and SHA-256 hash.
Publication consumes those exact paths without rebuilding or replacing an artifact. A blocked
classification, invalid manifest, missing evidence, or `publish_authorized=false` terminates the
release before NuGet, GitHub Release, tag/changelog, or other external publication side effects.

After publication, an independent verifier downloads NuGet and GitHub assets, verifies package
signatures, and compares their hashes with the sealed manifest. A mismatch, missing asset, or partial
publication fails the release and creates an incident record; post-publication evidence cannot
authorize a release retroactively. Durable evidence attached during initial GitHub Release creation is
the public evidence chain. Short-retention workflow artifacts are supplemental.

Ownership boundaries:

- **Hexalith.Builds** owns the reusable workflow contract, signing-secret declaration/forwarding, and
  minimum permissions.
- **FrontComposer** owns artifact creation, inventory/consumer/test validation, signing, evidence
  generation, readiness classification, publication of authorized bytes, and downloaded-artifact
  verification.
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
- **Maintainability and enforcement:** Stories 11.9 and 11.15–11.16 are done, 11.17a is done,
  11.17b–d and 11.18a are in review, and 11.18b–c plus 11.19a–d are materialized future work.

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
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
