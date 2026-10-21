# Epic 15 Context: Maintainers Evolve FrontComposer Without Contract Drift

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable framework maintainers to customize, inspect, migrate, document, test, and upgrade FrontComposer while preserving public contracts and exact dependency identity. Repair current planning and documentation drift with focused checks, retain accepted delivery history, and base compatibility claims on verified package evidence and explicit owner decisions.

## Stories

- Story 15.1: Restore Current Artifact-Integrity Validation
- Story 15.2: Reconcile Component and Page-Pattern Documentation
- Story 15.3: Align Migration and Classification Guidance
- Story 15.4: Correct the Fluent UI V5 Contingency Identity
- Story 15.5: Reconcile the Published Compatibility Baseline

## Requirements & Constraints

- Current story, epic, and action statuses must agree; story-owned changes must reconcile with complete, nonempty File Lists. Contradictions fail deterministically with support-safe diagnostics independent of machine paths or unrelated working-tree changes.
- Completed Epics 9–11 and closed tooling, route, Contracts-boundary, generated-code, and analyzer decisions remain accepted regression history. Repair current residual work without reopening its historical parent stories.
- Component, index, status, toolbar, tab, diagnostic, migration, and skill-corpus documentation must describe the implemented public contract. Validate semantic contents and identifier resolution, including public APIs, state behavior, accessibility, and supported customization seams. Internal Fluent composition is not automatically public API.
- Migration guidance must distinguish executable version edges from explicitly manual-only package/API changes. Inspection must preserve deterministic text/JSON ordering, severity/fail behavior, relative paths, and redaction. Migration remains dry-run by default; apply is atomic and rejects generated, submodule, symlinked, out-of-root, bin, obj, and .git targets.
- Adopter assertions and evidence are deterministic and redacted by default. Never expose payloads, tenant/user values, tokens, stack traces, or application-specific selectors in support evidence or diagnostic guidance.
- Compatibility baselines advance only to evidence-backed published packages, never from tags alone. An intentional lag needs a dated Release Owner decision with reason, expiry, and blocking evidence. Breaking changes require intentional API/schema baselines, documentation, and migration/deprecation handling.
- Reuse existing validators, inventories, parity checks, snapshots, PublicAPI, ApiCompat, and migration lanes. Extend missing semantic coverage without adding parallel registries, scanners, schemas, or evidence bundles. Record the exact focused command and result; changed published documentation must pass documentation validation.

## Technical Decisions

- Public contracts include schema canonicalization and fingerprint identity, generated-output locations, HFC diagnostic allocations, CLI JSON schemas, analyzer policy, and Contracts boundaries. Canonicalization changes invalidate drift baselines; preserve deterministic encoder, sentinel, and comparison behavior.
- Keep the Contracts kernel UI-neutral and netstandard2.0-clean. Rendering contracts belong in Contracts.UI; runtime implementations and test fakes stay outside the kernel. SourceTools depends only on the kernel. Preserve built-in Recommended analyzers with warnings treated as errors and narrowly owned exceptions.
- Preserve the named compatibility formats `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1`. The diagnostic registry governs diagnostic identities; executable migration edges and published guide classifications must agree without merging their distinct purposes. HFCM9002 production emission remains unapproved and synthetic/manual only.
- Hexalith.McpCli owns the target CLI/MCP presentation. Existing FrontComposer CLI/MCP packages are obsolete migration sources, limited to safety and continuity work. Retire operations only after approved generic replacements or explicit withdrawal with owner-approved parity evidence.
- Fluent versions remain catalog-owned. Resolve the exact selected identity when updating guidance and distinguish package selection from approval. The dated 2026-10-21 Product Owner/Architect decision records Components + Icons 5.0.1 GA as not approved; the Fluent acceptance gate remains open. Documentation must reflect that decision and subsequent dated decisions rather than infer acceptance from a dependency pin.

## UX & Interaction Patterns

- Document projection and command states consistently, distinguishing transport acknowledgement from projection/status confirmation. Status meaning needs visible text and accessible semantics beyond color or hover.
- Preserve WCAG 2.2 AA behavior and document supported focus, keyboard, routing, validation, announcement, and customization contracts. FcPageToolbar is the public toolbar contract; tab and toolbar guidance must describe supported behavior without exposing implementation-owned Fluent details.
- Customization resolves full-view overrides before projection templates and generated defaults. Mismatch/render-fault guidance is bounded and Development-only; operator data must not become a channel for raw debug details. Adopter helpers express behavior without hidden timing assumptions or application-specific selectors.

## Cross-Story Dependencies

- Component documentation and migration classification can complete independently; the overall documentation-parity residual requires both.
- Fluent contingency documentation consumes the exact catalog identity and Story 13.9's dated approval posture. Correcting documentation does not close the separate Fluent approval or UX evidence gates.
- Published compatibility reconciliation depends on release-ledger/package evidence and any Release Owner lag decision. Exact EventStore runtime capture/approval and publication-governance closure remain separate responsibilities in Epics 16 and 17.
