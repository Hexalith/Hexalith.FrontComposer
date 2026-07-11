# Hexalith.FrontComposer — Contribution Guide

> **Generated:** 2026-06-02 · deep scan. Derived from [CONTRIBUTING.md](CONTRIBUTING.md), [commitlint.config.mjs](commitlint.config.mjs), [.releaserc.json](.releaserc.json), and CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)).

## Commit messages — Conventional Commits (required)

Versioning is automated by semantic-release, so commit messages **must** follow [Conventional Commits](https://www.conventionalcommits.org/) (`@commitlint/config-conventional`, enforced locally by a husky `commit-msg` hook and in CI).

Format: `<type>(<optional scope>): <description>`

| Type | Effect |
|---|---|
| `feat:` | minor bump |
| `fix:` / `perf:` | patch bump |
| `feat!:` / `BREAKING CHANGE:` footer | **major** bump |
| `docs:` `refactor:` `test:` `chore:` `build:` `ci:` `style:` | no release |

Rules: imperative mood, lowercase, no trailing period; keep the subject short. **Do not use `feat` for refactors** (false minor bump + publish). In CI, both the **PR title** and **every commit** are validated by commitlint (advisory during Epic 1 via `continue-on-error`, slated to become blocking).

## Branching & PRs

- Branch names: `feat/<desc>`, `fix/<desc>`, `docs/<desc>` (ecosystem convention).
- **No direct commits to `main`** — use feature branches + PRs.
- PRs target `main`; CI runs on push/PR to `main` (concurrency-cancelled per ref).

## Code review

Senior/adversarial code review is part of this team's workflow — see [ONBOARDING.md](ONBOARDING.md) (`/bmad-code-review`, run on every story before flipping to Done; `/bmad-dev-story` is the main build command). Budget for review-found rework; verify any CRITICAL finding before acting on it.

## Must-follow engineering rules

1. **`.slnx` only** — never create/use `.sln`.
2. **Centralized package versions** — never add `Version=` to a `.csproj`; edit [Directory.Packages.props](Directory.Packages.props).
3. **`TreatWarningsAsErrors=true`** — analyzer/style warnings break the build; fix them, don't suppress without justification.
4. **Keep the package boundary downward.** Contracts targets `net10.0;netstandard2.0` and both faces are UI-clean; Contracts.UI is net10-only; SourceTools is netstandard2.0 and references only Contracts. Put runtime options/actions/registries in Shell and adopter fakes in Testing.
5. **ULIDs, not GUIDs** for `messageId`/`correlationId` (`IUlidFactory`).
6. **Don't hand-edit generated code.** Change the generator or the annotated domain types. The generated-output path (`obj/{Config}/{TFM}/generated/HexalithFrontComposer/`) is a **public contract** — validate in Debug *and* Release.
7. **Submodules:** root-declared under `references/` only; never recurse into nested submodules; **never modify submodule files without explicit approval** (changes propagate across the Hexalith ecosystem).
8. **`docs/` is a published DocFX site** referenced by product code, tests, CI, and fixtures — don't repurpose it as scratch space. (This BMAD doc set lives in `_bmad-output/project-docs/` precisely for that reason.)
9. **Schema canonicalization is load-bearing** — don't change the encoder/sentinel/comparer in `CanonicalSchemaMaterial` (silently invalidates all fingerprints/baselines). Don't relax the `SchemaBaselineProvenance` safe-identifier regex (security boundary).

## Source-generator contributions

From [CONTRIBUTING.md](CONTRIBUTING.md) and the generator design:

- Keep the IR **pure and fully equatable** — no `ISymbol` may escape the parse stage; use `EquatableArray<T>` for collections; hand-write `Equals`/`GetHashCode`. Missing a field breaks incremental caching.
- Diagnostics travel as **`DiagnosticInfo` data**, converted to Roslyn `Diagnostic`s only inside `RegisterSourceOutput`.
- The **drift pipeline must not depend on `CompilationProvider`** (only the trim/AOT advisory may, isolated in its own output).
- Density thresholds (≤1 Inline / 2–4 CompactInline / ≥5 FullPage) and baseline identity formation are **spec-locked** — change only with a story/ADR.
- New diagnostics: declare a `public const string` in `FcDiagnosticIds` (Contracts) + a `DiagnosticDescriptor` (SourceTools) with full XML docs; follow the `HFC1xxx` (build) / `HFC2xxx` (runtime) bands. See the full catalog in [api-contracts.md](./api-contracts.md) §1.4.
- `Debugger.Launch()` is contributor-local only — never in `src/**/*.cs` (source-scanned by the IDE-parity suite). Remove before review.
- Don't broaden the Roslyn package pin (`Microsoft.CodeAnalysis.CSharp` 5.6.0) outside a story that owns that compatibility work.

## Testing requirements

- All configured tests must pass before a change is done.
- xUnit **v3** + Shouldly (no raw `Assert.*`) + NSubstitute; bUnit for components; Verify for snapshots (`Verify.XunitV3`); FsCheck for properties; PactNet for the EventStore boundary.
- Three-part test names `Subject_Scenario_Expectation`.
- Update `.verified.txt` snapshots **intentionally** (CI sets `DiffEngine_Disabled=true`).
- Generator tests go through `CompilationHelper.CreateCompilation()`.
- **Public-API baselines:** update Contracts.UI and Testing `PublicAPI.Shipped.txt` files intentionally; Shell's focused FC-TBL baseline remains separately governed.
- **NFR17 tripwire:** new `IStorageService.SetAsync` call sites in `Shell/State/` require updating the tripwire whitelist + the story compliance matrix.
- **Pacts:** regenerate and commit intentional contract changes — CI fails on a stale pact diff (`tests/.../Shell.Tests/Pact`).
- Benchmarks live only in the separate `Shell.Tests.Bench` exe.

## Documentation changes

- Edits to the published site under `docs/` must pass `pwsh ./eng/validate-docs.ps1` (CI Gate 2d), which checks Diataxis structure, the diagnostic registry, the API-summary baseline, and builds DocFX with `warningsAsErrors`.
- Skill-corpus docs (`docs/skills/frontcomposer/**/*.md`) must satisfy the front-matter + `agent-reference` section contract (see [api-contracts.md](./api-contracts.md) §2.4); they are embedded into the MCP server and snippet/reference-validated.

## PR checklist

- [ ] Conventional-commit messages (and PR title).
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release` clean (warnings = errors).
- [ ] Default test lane green; Governance + Contract lanes green.
- [ ] No hand-edited generated files; generator changes covered by `SourceTools.Tests` snapshots.
- [ ] Snapshots / public-API baseline / pacts updated intentionally if affected.
- [ ] `pwsh ./eng/validate-docs.ps1` passes if `docs/` changed.
- [ ] No `Version=` added to a `.csproj`; no unapproved submodule edits.
