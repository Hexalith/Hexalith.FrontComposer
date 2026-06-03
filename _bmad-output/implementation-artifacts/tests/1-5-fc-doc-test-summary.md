# Test Automation Summary — Story 1.5 (FC-DOC component-documentation contract)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation
**Framework detected:** xUnit v3 (`3.2.2`) + Shouldly (`4.3.0`) — the project's standard test stack.
**Feature under test:** the FC-DOC component-documentation contract
(`_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md`) and the published pages it
governs under `docs/reference/components/`.

> **Output-path note.** The skill's single `default_output_file`
> (`…/implementation-artifacts/tests/test-summary.md`) already holds **Story 1.0's** summary. To avoid
> destroying that record, this story's summary is written to a story-scoped file in the same directory.

## Context — why these are governance/E2E tests, not API tests

Story 1.5 ships **no `src/` code** — it is a documentation-contract ready-gate. There is no HTTP/API
surface to exercise, so **API tests are not applicable**. The runtime enforcement is the PowerShell
Gate 2d (`eng/validate-docs.ps1`), which only runs in CI/`pwsh`. The story itself records the gap:

> "Enforcement is Gate 2d (`validate-docs.ps1`) itself — **there is no bUnit 'pin test' for this
> ready-gate the way 1.2–1.4 had**; the docs validator *is* the gate."

The generic `DocsSiteValidationTests` already pins front-matter, `uid`/`slug` uniqueness, and the four
top-level Diataxis entries across `reference/**` (so the new component pages inherit those). What was
**uncovered in the .NET suite** is the **FC-DOC-specific** contract. This run closes that gap by
generating a pin test that keeps the contract enforceable without shelling out to `pwsh`.

## Generated Tests

### E2E / Governance Tests

- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcDocComponentDocumentationContractTests.cs`
      — pins the FC-DOC contract over the published component pages (`[Trait("Category","Governance")]`,
      colocated with the existing `DocsSiteValidationTests`).

**15 test cases** (5 `[Fact]` + 5 `[Theory]` × 2 authored component pages):

| Assertion (gap covered) | Maps to |
|---|---|
| Anchor page (`FrontComposerShell`) + ≥1 more conforming page exist | AC1 / AC2 proof |
| Every component page contains all 7 required FC-DOC sections (Overview, Usage, Parameters/slots, Layout (FC-LYT), Accessibility (FC-A11Y), Localization (FC-L10N), Related) | AC1 — required-section template |
| Every component page declares `genre: reference` + `audience: adopter` | AC1 — Gate-2d front-matter |
| Every ` ```csharp ` fence is marked `compile` or `no-compile reason="…"` | AC1 — snippet rule |
| Every component page is free of unsafe text (absolute private paths, control sequences, tenant/secret literals) | AC1 — unsafe-text rule |
| Anchor page links **all** published `HFC1050`–`HFC1055` diagnostic pages (and they exist) | AC3 — a11y cross-link |
| Every component page links **≥1** published `HFC105*` diagnostic page | AC3 — cross-link convention |
| `reference/components/index.md` lists every authored component page | AC1 — index wiring |
| `toc.yml` nests `Components` under `Reference` and keeps exactly 4 top-level Diataxis entries | AC1 — TOC invariant |
| FC-DOC component status map covers the read-only-MVP set (layout, navigation, DataGrid, settings) as an authored page **or** a tracked gap with a named owner | AC2 — coverage record |

## Coverage

- **API endpoints:** 0/0 — not applicable (docs-only story, no API surface).
- **FC-DOC contract clauses:** the required-section set, front-matter genre/audience, csharp-fence
  marking, unsafe-text, HFC105* a11y cross-links, index/TOC wiring, and component-status-map coverage
  are now pinned in the .NET suite (previously only enforced by the `pwsh`-only Gate 2d validator).
- **Published component pages exercised:** `front-composer-shell.md`, `navigation.md` (the two authored
  pages; the two tracked-gap areas are asserted *as* tracked-gaps-with-owners).

## Results

- **New tests:** `Passed: 15, Failed: 0`
  (`dotnet test … --filter FullyQualifiedName~FcDocComponentDocumentationContractTests`).
- **Bite-verified:** mutation-tested — stripping a `no-compile reason` marker and removing a required
  section each produced exactly the expected failure (2 failed); reverted byte-identical.
- **Build:** test project builds clean — `0 Warning(s), 0 Error(s)` (TWAE).
- **No regression:** full SourceTools.Tests lane = `Passed: 941, Failed: 3` — the **3** failures are
  the pre-existing documented baseline (`CommandFormEmitterTests.Emit_DoesNotLogModelInstance`,
  `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`,
  `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates`), unrelated to this change.
- **No `src/` change, no docs change, no baseline change** — the docs were already FC-DOC-conforming;
  the discovered gap was the *absence of automated coverage*, which this test closes.

## Discovered gaps — auto-applied

The FC-DOC contract clauses were checked against existing coverage. The generic `DocsSiteValidationTests`
covers front-matter/uid/slug/top-level-TOC; everything **specific to FC-DOC** was uncovered and is now
filled in one new test class:

1. **Required-section template** — nothing asserted the 7-section set per component page. **Added.**
2. **`csharp`-fence marker rule** — only the `pwsh` validator enforced `compile`/`no-compile reason`. **Added.**
3. **Unsafe-text rule** — only the `pwsh` validator enforced it. **Added** (paths / control chars / secret literals).
4. **HFC105* accessibility cross-links** — no test pinned the AC3 cross-link convention. **Added** (anchor links all six; every page links ≥1).
5. **Components nested-under-Reference TOC placement** — the generic test pins the *top-level* count; nothing pinned that `Components` is nested under `Reference` (not a 5th entry). **Added.**
6. **Component status-map coverage (AC2)** — nothing pinned that every read-only-MVP area is an authored page or a tracked-gap-with-owner. **Added** (parses the contract's status table).

No doc defects were discovered — the pages were already conforming, so "auto-apply" filled the
*coverage* gap (the missing tests), not doc fixes.

## Next Steps

- Run in CI alongside `DocsSiteValidationTests` (both `[Trait("Category","Governance")]`) via the
  solution lane: `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` with `DiffEngine_Disabled=true`.
- When the **DataGrid surface** (FC-TBL / Story 2.8) and **Settings** (Story 1.6) pages are authored,
  the `[Theory]` cases auto-extend to them (they enumerate `docs/reference/components/*.md`), and the
  status-map test will then expect authored pages rather than tracked gaps.
