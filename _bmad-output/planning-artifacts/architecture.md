---
title: Hexalith.FrontComposer Architecture Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-11
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

## Key Invariants

- `SourceTools` references only the `Contracts` kernel and stays netstandard2.0-clean.
- No Blazor/Fluent/runtime/testing implementation types are added to `Contracts`; rendering contracts live in Contracts.UI, runtime options/registries/actions in Shell, and `InMemoryStorageService` in Testing.
- `ProjectionQuery` owns query criteria. `QueryRequest.Create` composes it with transport/cache metadata while HFC0001/CS0618 preserves the v1.12 flattened source and flat JSON compatibility surface until `2.0.0`.
- No Roslyn `ISymbol` escapes the SourceTools parse stage.
- Generated output path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is public contract.
- Schema canonicalization pins encoder, sentinel, source-gen context, and ordinal comparison.
- Fluent UI v5 is the UI component system; raw interactive HTML controls are forbidden outside documented carve-outs.
- Shell state follows Fluxor single-writer discipline and scoped-lifetime discipline.
- MCP security fails closed and requires both tenant tool and resource visibility gates.
- EventStore command acceptance is not treated as projection-confirmed success.
- UX/layout policy lives in architecture section 4 and is treated as planning input.

## Architecture Review Remediation

Epic 11 traces to `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`. The review found no Critical findings, but it identified High and Medium issues in runtime blind spots and architecture boundaries:

- token lifecycle and circuit-safe EventStore auth;
- projection realtime resilience;
- MCP cross-request lifecycle and operability;
- open-redirect and storage-key validation hardening;
- dead scoped CSS and visual-conformance guards;
- Testing harness failure modes;
- command/projection route-contract unification;
- Contracts kernel split and package/query compatibility evidence (Stories 11.11-11.14);
- Shell layering and duplication consolidation;
- one-type-per-file, LoggerMessage, and enforcement-policy alignment.

The 2026-07-05 Story 11.8 sign-off approved the kernel split. Stories 11.11-11.13 implemented the Contracts.UI assembly, ownership relocation, and composed-query compatibility surface. Story 11.14 adds the explicit release inventory, `v1.12.0` package-validation baseline, published migration guidance, and `2.0.0` Release Owner decision; it is documentation/inventory evidence, not future assembly implementation.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e11-contracts-kernel-split.md`
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
