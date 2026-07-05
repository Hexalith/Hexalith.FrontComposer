---
title: Hexalith.FrontComposer Architecture Planning Source
status: canonical-planning-source
created: 2026-07-05
updated: 2026-07-05
sourceOfRecord:
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/architecture-quality-review-2026-07-04.md
---

# Hexalith.FrontComposer Architecture Planning Source

This document makes the architecture discoverable to implementation-readiness workflows. The detailed brownfield architecture source remains `_bmad-output/project-docs/architecture.md`; this file is the planning artifact that readiness checks should load.

## Architecture Summary

FrontComposer is a source-generation-driven Blazor application framework. A contracts kernel defines the netstandard-safe attribute, communication, registration, MCP, schema, and diagnostic contracts; the Story 11.8 decision approves moving Blazor/Fluent rendering contracts to a net10-only `Contracts.UI` assembly. A Roslyn incremental generator reads annotated domain projections and commands; runtime consumers compose the generated artifacts through the Blazor Shell, MCP server, CLI, and Testing package. Schema fingerprints bind the producer and consumers so drift is detected instead of failing silently.

## Layers

- **Layer 0 - Contracts kernel:** attributes, communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts, and diagnostics IDs. The approved v1.0 target keeps this layer netstandard2.0-clean and free of Blazor/Fluent/runtime/test implementation types.
- **Layer 0A - Contracts.UI:** net10-only Blazor/Fluent rendering contract assembly planned by Story 11.11. This is the home for `Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members, and projection slot/template/view rendering contracts that cannot live in the kernel.
- **Layer 1 - SourceTools producer:** Roslyn incremental generator. Parse emits pure equatable IR; transform and emit produce generated UI, Fluxor state, registration, and manifests while referencing only the `Contracts` kernel.
- **Layer 2 - Consumers:** Shell, MCP, CLI, Testing, sample/UI hosts. Shell/UI consumers may reference `Contracts.UI`; MCP/Schema/SourceTools stay kernel-only unless a later approved story records evidence.
- **External dependencies:** root-declared `references/Hexalith.*` submodules only. Nested submodules are not initialized.

## Key Invariants

- `SourceTools` references only the `Contracts` kernel and stays netstandard2.0-clean.
- No new Blazor/Fluent/runtime/testing implementation types are added to `Contracts`; Story 11.11 moves the approved rendering surface to `Contracts.UI`.
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
- Contracts kernel split (approved by Story 11.8; implementation Stories 11.11-11.14 remain deliberately last);
- Shell layering and duplication consolidation;
- one-type-per-file, LoggerMessage, and enforcement-policy alignment.

The 2026-07-05 readiness correction extracts the route-contract decision into Story 11.0 and splits oversized Epic 11 work before implementation. The later 2026-07-05 Story 11.8 sign-off approves the Contracts kernel split, amends the former multi-TFM decision, and requires package-compat/public-API/deprecation evidence in the pre-v1.0 window before Stories 11.11-11.14 are implemented.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e11-contracts-kernel-split.md`
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
