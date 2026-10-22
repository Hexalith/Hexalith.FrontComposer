# Epic 15 Context: Maintainers Evolve FrontComposer Without Contract Drift

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable framework maintainers to customize, inspect, migrate, document, test, and upgrade FrontComposer while preserving public contracts and exact runtime identity. Reconcile current documentation and planning drift, maintain evidence-backed compatibility baselines, and reduce generated evidence in Git without changing accepted delivery history or approval requirements.

## Stories

- Story 15.1: Restore Current Artifact-Integrity Validation
- Story 15.2: Reconcile Component and Page-Pattern Documentation
- Story 15.3: Align Migration and Classification Guidance
- Story 15.4: Correct the Fluent UI V5 Contingency Identity
- Story 15.5: Reconcile the Published Compatibility Baseline
- Story 15.6: External Browser Artifacts And Bounded Accessibility Receipts
- Story 15.7: Retire Bulky Browser Evidence

## Requirements & Constraints

- Current epic, story, and action statuses must agree, and story-owned changes must reconcile with complete, nonempty File Lists. Contradictions fail deterministically with support-safe diagnostics independent of machine paths or unrelated working-tree changes. Completed Epics 9–11 remain accepted regression history; new residual work does not reopen their parent stories.
- Component, index, status, toolbar, tab, diagnostic, migration, and skill-corpus documentation must describe the implemented public contract. Check semantic content and identifier resolution, including supported APIs, state behavior, accessibility, and customization seams. Published documentation changes pass documentation validation.
- Migration guidance must distinguish executable version edges from explicitly manual-only package/API changes. Unclassified or falsely executable edges fail parity checks. Migration is dry-run by default; apply is atomic and refuses generated, submodule, symlinked, out-of-root, bin, obj, and .git targets. Inspection preserves deterministic text/JSON ordering, severity/fail behavior, relative paths, and redaction.
- Compatibility baselines advance only to evidence-backed published packages. Tags alone are insufficient. Intentional lag needs a dated Release Owner decision with reason, expiry, and blocking evidence; breaking changes require intentional baselines, documentation, and migration/deprecation handling.
- Reuse existing validators, inventories, parity checks, PublicAPI, ApiCompat, snapshots, and migration lanes. Extend missing semantic coverage without creating parallel registries, scanners, or evidence bundles. Record the exact focused validation command and result.
- Generated qualification output belongs in external artifact storage, with one bounded summary/receipt per attempt and one selected-result reference in Git. Attempt metadata totals at most 10 MiB; command excerpts are at most 2,048 UTF-8 bytes. Automatic admission warns and omits blobs over 10 MiB, generated artifact types regardless of size, and generated-evidence selections over 50 MiB per commit. Splitting or renaming output cannot bypass these limits.
- Redact before storage. Diagnostics normally retain 30 days; enduring accepted proof requires durable immutable retrieval, and active failures/holds retain required bytes until disposition. Preserve source-owned visual baselines, original receipts, historical identities, attempt outcomes, and independent approval gates.

## Technical Decisions

- Governed public contracts include schema canonicalization and fingerprints, generated-output locations, HFC diagnostic allocations, CLI JSON, analyzer policy, and Contracts boundaries. Canonicalization changes invalidate drift baselines. Preserve the UI-neutral Contracts kernel, rendering-contract separation, and Recommended analyzers with warnings treated as errors and narrowly owned exceptions.
- Preserve named formats `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1`. Diagnostic identities, executable migration edges, and published manual-only guidance have distinct authorities whose contents must agree.
- Hexalith.McpCli owns the target CLI/MCP presentation. Existing FrontComposer CLI/MCP packages are obsolete migration sources limited to safety and continuity work. Retirement requires an approved generic replacement or explicit withdrawal with owner-approved parity evidence.
- Fluent versions remain catalog-owned. Documentation must resolve the exact selected identity and report its actual dated approval posture; a package pin does not grant approval. The recorded Components + Icons 5.0.1 GA decision is not approved, and Fluent acceptance remains separate work.
- External evidence manifests bind immutable artifact identity, exact bytes, source/producer identity, attempt disposition, and retention. Consumers needing content must materialize and verify hashes/sizes before running existing validators. Hashes or excerpts alone cannot satisfy content-dependent checks. Preserve browser/run/test/retry binding, PNG integrity, and final-successful-attempt selection.
- Evidence retirement requires an exact-path manifest, consumer migration, verified retrieval, dated preservation amendments, and passing checks after removal. Ordinary unit tests use bounded offline fixtures; historical exact-byte checks explicitly materialize their inputs. No blanket evidence deletion, history rewrite, or cross-repository cleanup is authorized.

## UX & Interaction Patterns

- Document canonical projection and command states, distinguishing transport acknowledgement from projection confirmation. Status has visible text and accessible meaning beyond color or hover. Preserve WCAG 2.2 AA, focus, keyboard, routing, validation, and announcement contracts.
- FcPageToolbar is the public toolbar contract; internal Fluent composition remains implementation-owned. Customization resolves full-view overrides before projection templates and generated defaults. Mismatch/render-fault guidance is bounded and Development-only, never exposing payloads, tenant/user values, tokens, or stack traces. Testing helpers avoid application-specific selectors and hidden timing assumptions.
- Moving browser output preserves all accessibility acceptance semantics, required manual assistive-technology/device proof, and opt-in UX review dispositions; storage changes confer no approval.

## Cross-Story Dependencies

- Component documentation and migration classification can complete independently; overall documentation parity requires both.
- Fluent contingency documentation consumes the catalog identity and Story 13.9's dated decision without closing Fluent or Product approval gates.
- Compatibility reconciliation consumes published-package/release-ledger evidence and any Release Owner lag decision; exact EventStore runtime approval and publication governance remain in Epics 16 and 17.
- Story 15.7 depends on 15.6's external output/receipts and verified consumer migration. Coordinate planning integrity with 15.1 and shared admission rollout with Hexalith.AI.Tools. FrontComposer owns its browser evidence; EventStore retains ownership of its separate packets.
