---
title: FrontComposer 2.0 Release Version Decision
date: 2026-07-11
status: approved
owner: Release Owner
approvedBy: Administrator
story: 11.14
amends: fc-contracts-kernel-split-compatibility-plan-2026-07-05
---

# FrontComposer 2.0 Release Version Decision

## Decision

Approve `2.0.0` as the release target for the Contracts kernel split, the Contracts.UI assembly,
the Story 11.12 runtime/testing ownership moves, and the Story 11.13 composed-query migration.

The latest stable baseline is `v1.12.0`. Moving released public types between assemblies without
type forwarding is binary-breaking, and the Story 11.12 namespace/assembly moves require source
changes. These changes must not be presented as an ordinary backward-compatible minor release.

## Compatibility Posture

- `Hexalith.FrontComposer.Contracts.UI` is a new net10-only package. Existing rendering and shortcut
  namespaces remain unchanged, but adopters must add the package reference and rebuild.
- Story 11.12 moves require adopters to update namespaces and package references to Shell or Testing.
- The flattened v1.12 `QueryRequest` constructor, criteria properties, deconstruction, and flat JSON
  representation remain available through HFC0001/CS0618 shims throughout 2.x, with removal targeted
  for `3.0.0`.
- `IQueryService`, Testing callback signatures, EventStore wire behavior, MCP descriptors/output,
  schema fingerprints, CLI JSON, generated routes/output paths, and Pact wire shape do not change as
  incidental consequences of the package-boundary work.

## Release Evidence

The implementation/release commit range must contain a valid Conventional Commit breaking-change
signal (`!` or a `BREAKING CHANGE:` footer) so semantic-release generates a major release and truthful
release notes. `CHANGELOG.md` remains semantic-release-owned and is not hand-authored by Story 11.14.

Package validation uses `v1.12.0` as the latest stable baseline for existing packages. The first
Contracts.UI release has no prior baseline; its project-specific exception must be removed after
`Hexalith.FrontComposer.Contracts.UI` `2.0.0` is published, then validation advances to that baseline.

## Approval Record

Administrator, acting as Release Owner, approved the `2.0.0` posture in the Story 11.14 dev-story
workflow on 2026-07-11.

On 2026-07-12, Administrator approved the Story 11.14 review resolution that retains the flattened
`QueryRequest` compatibility shims throughout the complete 2.x release line and advances their
removal target to `3.0.0`.
