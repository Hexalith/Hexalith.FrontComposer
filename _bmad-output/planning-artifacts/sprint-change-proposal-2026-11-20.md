# FrontComposer audit correction

20 November 2026 · Requested by the repository operator · Moderate scope

## Issue and evidence

The operator requested implementation of the nine recommendations from the [technical audit](https://chatgpt.com/space/page_662d2b7e5ad081919604b295f6261890). The audited source is FrontComposer `c1aea173` with its ten direct repositories at their audited main revisions. The Counter sample exposed two defects that fixtures had missed. A completed MCP command stayed acknowledged on later reads, and the CLI reported zero generated files while Roslyn had emitted 31. A controlled interop timeout also stopped subsequent browser writes. A combined Debug build raised MSB3277 for the OIDC assembly.

## Impact and approach

Apply a direct correction to existing behavior in Epics 2, 3, 5, 7, 11, 12, 13, 14, 15 and 16. The PRD already promises inspection, asynchronous completion and tenant-scoped reads. The UX specifies meaningful command feedback. The changes implement those commitments within the existing generator, Shell, MCP and host boundaries. Rollback would discard working functionality. Reducing MVP scope would leave supported workflows unreliable. Historical story completions remain a record of their original acceptance runs.

## Changes and acceptance

| Area and owning role | Current behavior | Required behavior and proof |
|---|---|---|
| Stories 5.2 and 11.3 · MCP developer | The dispatch-time snapshot loses later callbacks. The active-order queue retains completed handles. | Deliver a delayed terminal callback across requests exactly once, preserve reader admission and identity checks, and unlink completed active entries under normal retention. Exercise delayed delivery, duplicate delivery and bounded sequential completions. |
| Stories 3.4 and 11.15 · Shell developer | A cancelled interop write stops the storage reader. | Continue after one per-call failure, surface that failure at a flush boundary, and settle pending flushes during disposal. Verify a subsequent successful write. |
| Story 7.1 · Tooling developer | A flat fixture passes while the installed CLI misses compiler-generated files in Counter. | Support both compiler and flat layouts, exclude foreign generators and verify a known command and projection with the packaged CLI. |
| Epics 12 and 16 · Builds and host developers | The combined net11 host resolves OIDC 10 alongside modules built against 11. | Scope compatible package selection, check the assembled host and its router, and exercise a sign-in and token relay with a disposable provider where available. |
| Epics 2 and 13 · Shell developer | A schema-invalid 304 cache entry survives refresh. | Discard only the affected scoped entry and retry once without a validator. Verify recovery, recurring failure and tenant isolation. |
| Epic 3 · Form developer | Process-wide cache reuses dynamic property defaults across forms. | Create fresh defaults per form and preserve agreement between properties from the same instance. Verify dynamic and constant values. |
| Epic 15 · CiCd owner | Quality and security jobs consume mutable shared workflow references. | Pin reviewed upstream commits at the owning source and validate the affected workflow configuration. |
| Epics 7 and 12 · Documentation owner | The README repeats obsolete hashes, architecture identifies an old runtime, and the tutorial omits runnable commands. | Use current source-of-truth links and tested commands from a direct-only checkout through the Counter UI and CLI. |
| Story 3.4 · Browser test owner | A timing-sensitive test can miss acknowledgement before confirmation. | Capture the transition before dispatch or control test timing, and still assert the terminal UI. |

## Handoff and sequencing

The product owner role mapped the findings onto the existing backlog. The developer roles take the independent fixes and their regressions in parallel. Allow 2–3 working days for independent CLI, storage and onboarding work, 2–3 for MCP handoff and retention together, and 2–4 for host qualification with the Builds and identity owners. The cache, form and CI changes can proceed while the host is being qualified. Updating the README command depends on the CLI fix. Record actual verification and any remaining owner dependencies before closing the correction. Version publication, dependency seal activation and changes to Epic 16 or 17 release gates require their own evidence and owner decisions.

## Checklist disposition

Trigger and evidence, epic impact, PRD/architecture/UX comparison, alternatives, acceptance criteria and implementation routing were reviewed as a batch. The operator's instruction “apply recommendations” authorizes local implementation. No new numbered epic or story is needed. Verification and integration outcomes belong in the implementation record below.

## Implementation record

The MCP dispatcher now hands its callback to the acknowledged entry, and active entries have removable ordering nodes. Two independent HTTP polls on the Counter sample returned terminal `Confirmed`. All 537 MCP tests passed, including handle reuse and request-scope disposal.

A failed interop call is delivered at a flush boundary and the browser storage worker continues draining. The original two-write probe reached JS twice and completed before the caller deadline. All 16 canonical storage tests passed. Projection cache recovery retries a single unreadable entry without a validator. Constructor defaults are scoped to one resolved form and preserve relationships between fields initialized together.

The installed CLI finds both Roslyn and legacy flat layouts, selects a single project target, and excludes files from unrelated generators. All 158 CLI tests passed. On Counter the installed tool reported **43 generated files** (31 C# and 12 Razor-generated C#), five forms and one grid. Repeated inspection emitted identical JSON. The documented tutorial was also exercised from a fresh local clone with the recorded direct pins and an added projection.

A fresh Release solution build passed without warnings on both the recorded dependency pins and the audited working revisions. The combined Debug AppHost also built with zero warnings. The owner-side Builds catalog now keeps OIDC 10 for the applicable net10 consumers and selects 11 for the combined net11 host. The router suite passed 17/17. A real Keycloak callback, protected Tenants page, signed bearer relay to a validation receiver, and sign-out succeeded. The authenticated Parties admin page exposed a separate missing authoritative projection store. The current invocation-only UI cannot subscribe to the authoritative projection under its Dapr policy. The repository operator approved a dependent correction that checks current tenant ownership through the caller-bound Tenants API. Its host and test project inputs are governed by the dependency seal.

The owner-side CiCd generator prepares eleven immutable references in eight workflows, and its output passed Actionlint. Reproduction instructions are in `docs/proposals/frontcomposer-workflow-pins-20261120/` in CiCd. The Debug router action is present in the product checkout and the CLI regression runs through the existing package test. The owner revisions are under review. Hosted workflow activation remains with CiCd. [Builds PR #195](https://github.com/Hexalith/Hexalith.Builds/pull/195) · [CiCd PR #4](https://github.com/Hexalith/Hexalith.CiCd/pull/4)

Chromium recorded the intermediate acknowledgement and final confirmation on three consecutive runs. The ordinary Development profile also passed the lifecycle and two UI smoke checks, including the accessibility scan.

The complete docs validator passed, including compilation of its snippets and DocFX. Debug publishing of the combined UI exposed a separate SDK static-asset compression error (`MSB4018` in `ApplyCompressionNegotiation` for Blazor JavaScript). It reproduced with fresh UI intermediates while reusing built dependency projects. Interactive assets served in the supported Test host. Published deployment remains unqualified.

The final Shell regression lane passed 4,035/4,035. Earlier runs exposed a duplicate feature in the new form-test harness, a transient loopback-port collision and drift in the sealed inventory of public test identifiers containing underscores. The final harness uses the production provider registrations without duplicate features. New test names follow PascalCase, and retiring the obsolete cache test reduced the exception inventory from 3,587 to 3,586. The inventory guard also passed against a temporary index containing the new test files.

On the final checkout the seven project-level default Release test runs passed **6,794/6,794**, using the repository's default trait exclusions and the container's child-process reaper. TypeScript type checking passed. Chromium passed **7/7** across the smoke, lifecycle and MCP resource specs. The repository tracked-file check passed on 6,488 files. Release activation, hosted CI rollout and a production publication remain separate from these local results.
