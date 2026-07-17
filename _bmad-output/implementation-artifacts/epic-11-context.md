# Epic 11 Context: Release Readiness Remediation Program

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the remaining release-readiness risks without reopening completed product epics. It is
governed as four workstreams: runtime reliability/security, adopter testing/route integrity,
contracts/package boundary, and maintainability/enforcement. Stories 11.17, 11.18, and 11.19 are
nonimplementable decomposition parents; only their child stories carry delivery status.

## Workstreams And Story State

### Runtime Reliability And Security

- Stories 11.0–11.5: done.
- Story 11.18a, fail-closed/security log sites: review.
- Story 11.24, owner-approved EventStore runtime adoption: blocked backlog. It has no story file and
  cannot move to `ready-for-dev` until EventStore Story 1.20 durably authorizes migration and pins the
  approved source and package identities.

### Adopter Testing And Route Integrity

- Stories 11.6–11.7: done. Story 11.6 consumes completed Story 10.5 privacy evidence.

### Contracts And Package Boundary

- Story 11.8 decision record and Stories 11.11–11.14 delivery records: done. These remain historical
  acceptance contracts, not queue candidates.

### Maintainability And Enforcement

- Stories 11.9 and 11.15–11.16: done.
- Story 11.17a: done; Stories 11.17b–d: review.
- Stories 11.18b–c: ready-for-dev.
- Stories 11.19a–d: ready-for-dev.

Parent Stories 11.17, 11.18, and 11.19 must never receive backlog, ready-for-dev, or review status.

## Requirements & Constraints

This epic is release-risk remediation, not new product scope. Each implementation story must use focused Given/When/Then acceptance criteria, preserve existing command/query/projection behavior, and add durable evidence for the defect class it closes.

Security and support-safety apply throughout. Auth, redirects, storage scopes, MCP denial paths, lifecycle tracking, logs, telemetry, snapshots, and Testing evidence must fail closed where appropriate and must not expose raw tokens, secrets, payloads, stack traces, unrestricted PII, or raw local paths.

Testing lets adopters simulate command rejection, timeout, stall-at-`Syncing`, authorization-policy states, and per-request paging/filter/sort outcomes through configurable fakes. `TestFaultEvidenceRecorder` is evidence-only and records redacted fault observations; it does not inject faults. Changed builders, assertions, fakes, and evidence paths require direct tests, default redaction of configured tenant/user identifiers (including property and dictionary keys), structural secret redaction, bounded path evidence, and intentional Testing public-API baseline updates.

Visual fixes require guard-backed evidence for dead scoped CSS, unlinked stylesheets, legacy Fluent tokens, and accessibility-sensitive behavior. Public API, schema, CLI/MCP wire shapes, diagnostics, generated output, and package inventory remain controlled contracts; changes require intentional baselines and migration or compatibility evidence. Build policy remains .NET 10, `.slnx`, centralized dependencies, nullable code, and warnings as errors, with no global warning/analyzer suppression used to hide remediation.

## Technical Decisions

The canonical generated command route is `/commands/{BoundedContext}/{CommandTypeName}`. Palette entries, empty-state CTAs, and generated command pages must converge on it. Module tabs use `/{module}/{tab}`, with projection flyouts secondary to the module workspace.

The implemented package target keeps both `Contracts` TFMs UI-clean and places Blazor/Fluent rendering contracts in packable net10-only `Contracts.UI`. `SourceTools` remains a packable netstandard2.0 analyzer referencing only the kernel. Shell owns runtime options, registries, and Fluxor actions; Testing owns `InMemoryStorageService`. Public moves require package-consumer, public-API, documentation, release-inventory, and deprecation evidence.

MCP cross-request state uses a singleton state store behind a scoped facade; it must not capture scoped admission services. EventStore token acquisition must work safely in interactive circuits with expiry and sign-out eviction. Projection realtime must recover beyond the default retry ladder, restart after closed connections, and align disposal/cache synchronization. Logging uses sanitized source-generated events with exclusive ownership: 11.18a security/fail-closed sites first, 11.18c command-lifecycle/projection-refresh/polling hot paths second, and 11.18b residual Warning/Error/Critical sites last.

EventStore dependency adoption is a separate identity gate. Story 11.24 requires the exact
Story-1.20-approved source SHA in Debug/source mode and the approved package version and hashes in
Release/package mode through an already-landed Builds pin. Provider Pact verification, isolated
package restore, source/package AppHost builds, Governance/default lanes, and a live Aspire smoke must
converge before adoption can complete. The story does not authorize adapter, topology, rollback, or
container-deployment redesign.

`ProjectionQuery` now owns canonical query criteria and is composed through `QueryRequest.Create`; HFC0001/CS0618 preserves the v1.12 flattened source/deconstruction surface and flat JSON throughout 2.x, with removal targeted for `3.0.0`. Shell boundaries place telemetry cross-cutting, connection/polling workers in infrastructure, and route/label helpers outside render components; duplicated scope, snapshot, fatal-exception, hydration, JSON, and literal-escaping behavior should be consolidated with focused equivalence tests.

## UX & Interaction Patterns

Realtime and command surfaces must expose reconnecting, fallback, degraded, pending, rejected, and confirmed states without treating HTTP acceptance or a projection nudge as confirmed success. UI remediation uses FrontComposer/Fluent UI Blazor v5, Fluent 2 tokens, accessible names and keyboard behavior, stable selectors, reduced-motion and forced-colors support, plus rendered-DOM, computed-style, bUnit, e2e, or Governance evidence appropriate to the change.

## Cross-Story Dependencies

The current implementation queue consists only of materialized child stories. Story 11.18c freezes
the hot-path semantic inventory before Story 11.18b freezes and migrates the residual Warning+ set, so
the same direct log call cannot be claimed twice. Stories 11.19a–d are independent by defect class,
except that 11.19d is a decision record and may create later implementation work only through a new,
explicitly approved story.

Story 11.0 and FC-IA-1 are resolved prerequisites for completed Story 11.7. Story 11.8 is the resolved
decision prerequisite for completed Stories 11.11–11.14. Epic 10 is done; its evidence may be consumed
by Epic 11, but Epic 11 does not claim independence from or reopen it.

Story 11.24 is independent of the analyzer sequence in Stories 11.20–11.23. Its sole activation gate
is durable EventStore Story 1.20 migration authority; a tag, current EventStore HEAD, package version
without approved hashes, or a source-only gitlink is insufficient.
