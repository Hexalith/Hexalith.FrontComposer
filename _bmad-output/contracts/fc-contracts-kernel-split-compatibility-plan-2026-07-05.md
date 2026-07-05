---
title: FC Contracts Kernel Split Compatibility Plan
date: 2026-07-05
status: approved
owner: Architect + PM
story: 11.8
sourceAction: E11-AI-2
blocks:
  - 11.11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface
  - 11.12-relocate-runtime-and-testing-owned-types-out-of-contracts
  - 11.13-decompose-queryrequest-through-hfc0001-migration-path
  - 11.14-update-architecture-context-ux-and-package-compat-docs
---

# FC Contracts Kernel Split Compatibility Plan

## Decision

Approve the Contracts kernel split for the pre-v1.0 window.

Target package shape:

- `Hexalith.FrontComposer.Contracts` becomes the netstandard2.0-clean kernel for attributes,
  communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts,
  diagnostics IDs, and stable wire/DTO types.
- `Hexalith.FrontComposer.Contracts.UI` is the approved net10-only Blazor/Fluent rendering contract
  assembly. Story 11.11 owns the final project/package name if implementation evidence requires a
  narrow adjustment, but the rendering surface must not remain in the kernel by default.
- `Hexalith.FrontComposer.SourceTools` remains netstandard2.0 and references only
  `Hexalith.FrontComposer.Contracts`.

This amends the earlier multi-TFM decision where `Contracts` carried both `net10.0` and
`netstandard2.0` faces and guarded net10/Fluent-only code with `#if NET10_0_OR_GREATER`.

## Approved Moves

- Move `Typography` / `FcTypoToken` and other Fluent text-token mappings to `Contracts.UI`.
- Move `RenderFragment`-based projection/view/template/slot contexts to `Contracts.UI` or to an
  implementation-equivalent net10-only assembly approved by Story 11.11 evidence.
- Move `KeyboardEventArgs`-dependent shortcut contracts to the net10/UI side.
- Move runtime and testing implementation types out of the kernel in Story 11.12:
  `InMemoryStorageService` to Testing, `InlinePopoverRegistry` implementation to Shell, shell options
  to Shell-owned options, and Fluxor action records to Shell where applicable.
- Split `QueryRequest` in Story 11.13 through the HFC0001 migration/deprecation path so UI query
  criteria and transport/caching envelope concerns no longer share one 19-parameter record.

## Affected Packages

- `Hexalith.FrontComposer.Contracts`: public API removals/moves are expected before v1.0 and must be
  represented in package validation baselines.
- `Hexalith.FrontComposer.Contracts.UI`: new package/assembly target for Blazor/Fluent rendering
  contracts if Story 11.11 implements the approved shape.
- `Hexalith.FrontComposer.Shell`: should reference `Contracts.UI` for UI rendering contracts after
  the move.
- `Hexalith.FrontComposer.Testing`: may reference `Contracts.UI` only for UI test host helpers; runtime
  test fakes moved from Contracts belong here when they are test-owned.
- `Hexalith.FrontComposer.SourceTools`: remains kernel-only. Generated code may emit references to
  UI contract namespaces, but the analyzer project must not add net10/Blazor/Fluent references.
- `Hexalith.FrontComposer.Mcp`, `Hexalith.FrontComposer.Schema`, and `Hexalith.FrontComposer.Cli`:
  expected to stay on kernel contracts unless a later story records contrary evidence.

## Compatibility Posture

- Release posture: pre-v1.0 breaking package-boundary change is approved only with explicit
  compatibility evidence before v1.0 publication.
- Source compatibility: prefer type-forwarding, obsolete shims, or HFC0001 diagnostics where feasible;
  if a move cannot stay source-compatible, document the old namespace/type and the new package/type.
- Binary compatibility: public API baselines and package validation must be intentionally updated under
  Story 11.11-11.14 evidence. No silent baseline churn.
- Schema/wire compatibility: MCP descriptors, schema fingerprints, CLI JSON, generated-output path, and
  EventStore wire DTOs must not change as a side effect of the package split unless explicitly versioned.
- Package inventory: update the release package inventory, docs, and package-compat guidance before the
  v1.0 release candidate.

## Ordering

The decision gate is complete, but implementation remains deliberately last:

```text
11.0 -> 11.1 -> 11.2 -> 11.4 -> 11.3 -> 11.5 -> 11.6 -> 11.7
-> 11.9/11.15/11.16 -> 11.17/11.18/11.19 -> 11.8/11.11-11.14 last
```

Story 11.8 is done. Stories 11.11-11.14 remain backlog implementation/evidence work.

## Done Criteria For The Implementation Set

- `Contracts` can be consumed by netstandard2.0 analyzer/build hosts without inheriting Blazor or the
  pinned Fluent UI RC.
- `SourceTools` still references only `Contracts`.
- Shell/UI consumers compile against the moved rendering contracts.
- Public API baselines, package validation, docs, release inventory, and migration/deprecation guidance
  are updated intentionally.
- Package-consumer validation covers at least one representative Hexalith adopter path before v1.0 RC.
