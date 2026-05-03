# Story 9.3: IDE Parity & Developer Experience

Status: ready-for-dev

> **Epic 9** - Developer Tooling & Documentation. Covers **FR65**, **NFR8**, **NFR71**, **NFR77**, and **NFR92**. Builds on Story **9-1** generated-output/drift diagnostics and Story **9-2** CLI inspection/path contracts. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, and **L15**.

---

## Executive Summary

Story 9-3 makes "IDE parity" a testable contract instead of a vendor claim:

- Publish `docs/ide-parity-matrix.md` as the authoritative capability-by-IDE matrix.
- Define Must/Should/Out-of-scope tiers for Visual Studio, JetBrains Rider, and VS Code with C# Dev Kit.
- Separate repo-owned generated-output guarantees from vendor IDE behavior evidence so parity does not become a vague certification claim.
- Add conformance fixtures that verify generated-source navigation, diagnostics, XML docs, symbol search, and NFR8 generator performance for Must-tier rows.
- Treat generated output paths from Story 9-2 as a public contract consumed by IDEs, CLI inspection, and adopter scripts.
- Document IDE-specific limits honestly, including VS Code Dev Kit licensing and Rider generated-code debugging behavior, rather than hiding them behind vague parity language.
- Require evidence manifests and fail-closed matrix validation so manual IDE evidence cannot silently go stale, drift from the fixture commit, or inject unsafe log/table content.

---

## Story

As a developer,
I want an equivalent development experience across Visual Studio, JetBrains Rider, and VS Code with C# Dev Kit,
so that I can use my preferred IDE without losing IntelliSense, navigation, refactoring, or debugging confidence.

### Adopter Job To Preserve

An adopter should be able to open a FrontComposer solution on Windows, macOS, Linux, or a containerized VS Code environment; edit `[Command]`, `[Projection]`, `[BoundedContext]`, and customization attributes; and get clear generated-type IntelliSense, HFC diagnostics, navigation, XML docs, and debugging guidance without guessing which IDE-specific behavior is trustworthy.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The IDE Parity Conformance Matrix exists | Story 9-3 is considered complete | `docs/ide-parity-matrix.md` is published, lists every capability by IDE and support tier, and is the authoritative definition of parity. No prose claim overrides the matrix. The matrix schema includes row ID, capability, tier, IDE/version, OS or container image where applicable, fixture name, validation method, expected result, evidence artifact, owner, last verified date, known limitation, and release gate. |
| AC2 | A capability is marked Must in the matrix | CI conformance or release validation runs | Repo-owned Must failures in generated paths, XML docs, diagnostics, symbol output, or NFR8 performance block CI or release. Vendor IDE UI/indexing differences block release only when evidence shows a FrontComposer generated-output defect; otherwise the row records a pinned-version limitation and linked revalidation issue before release. |
| AC3 | IDE versions are pinned for a release | The matrix is generated or updated | It records exact product names, versions, platforms, extension versions, .NET SDK, C# extension/C# Dev Kit versions, and support windows. Do not invent a product name such as "Visual Studio 2026" unless official release evidence exists at v1 freeze; use the exact installed product name such as Visual Studio 2022 17.13+ when that is the supported baseline. |
| AC4 | Visual Studio parity is communicated externally | The matrix describes the calibration IDE | Visual Studio is named as the calibration IDE used to baseline the suite, not "the reference IDE"; the matrix remains authoritative and macOS/Linux adopters have first-class Rider or VS Code + Dev Kit paths. |
| AC5 | VS Code parity is claimed | The onboarding docs and matrix are published | C# Dev Kit is a stated prerequisite; Microsoft account/proprietary license implications are acknowledged; OmniSharp-only VS Code is explicitly unsupported in v1. |
| AC6 | Any IDE in the matrix opens a FrontComposer sample | The developer works with source-generated types | Must-tier rows verify IntelliSense completions on generated types, XML-doc hover content, go-to-definition to generated source, HFC diagnostic squiggles, solution-wide symbol search including generated types, and NFR8 incremental-generator behavior within the 500 ms budget. |
| AC7 | Any IDE invokes Should-tier capabilities | The developer uses richer workflows | Find All References across domain symbol to generated usage, rename workflow, analyzer code-fix application, hot reload behavior, and generator-host debugging are documented per IDE with evidence or explicit limitations. |
| AC8 | A developer renames a domain member | The IDE workflow is followed | Rename parity is defined as: edit the domain symbol, rebuild/regenerate, inspect generated Razor/Fluxor/MCP output through generated files or `frontcomposer inspect`, and verify drift diagnostics or generated output update. Generated files remain read-only by design. |
| AC9 | Generated code debugging is documented | The audience is an adopter | The matrix distinguishes setting breakpoints directly in generated files from stepping into generated code at runtime. If an IDE cannot set breakpoints in generated source, the limitation is a matrix row, not a hidden failure. |
| AC10 | Generator-host debugging is documented | The audience is a framework contributor | Contributor-only guidance for `Debugger.Launch()`, JIT attach, Roslyn/compiler server behavior, and IDE-specific generator-host limitations lives in `CONTRIBUTING.md`, not adopter onboarding prose. |
| AC11 | A framework attribute such as `[Projection]` or `[BoundedContext]` is hovered | Any supported IDE displays quick info | XML doc comments explain what the attribute does, what it generates, and link to the relevant diagnostic/documentation page. |
| AC12 | Consumers rely on generated output locations | IDE navigation, CLI inspect, or scripts execute | `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs` and sibling `.g.cs` files are treated as public path contracts guarded by tests across Debug/Release and multi-targeted fixtures. Story 9-3 verifies and documents this Story 9-2 contract; it must not change generator output layout except for test/documentation fixes. |
| AC13 | Remote or containerized development is supported | The v1 release candidate is validated | At least one documented validation scenario covers containerized VS Code + C# Dev Kit prerequisites; automated CI is used only when licensing, authentication, and headless behavior are legal and reproducible. Remote-SSH, GitHub Codespaces, and Dev Containers limitations are enumerated in the matrix. |
| AC14 | A vendor IDE, extension, or .NET SDK version moves outside the pinned range | CI, a scheduled job, or a release checklist detects the change from configured inputs | A GitHub issue is filed with the `ide-parity` and `conformance-revalidation` labels, the affected matrix rows, current pin, detected version, fixture used, expected behavior, observed behavior, evidence needed, and release owner before widening the range. |
| AC15 | CS1591 XML-doc enforcement changes near API freeze | The project approaches v1.0-rc1 | Before v1.0-rc1, CS1591 remains a warning for all public types. At or after API freeze, CS1591 is scoped via `.editorconfig` file globs to files backing `PublicAPI.Shipped.txt`, and adopter templates receive the same scoped behavior. |
| AC16 | Conformance output is logged in CI | A row fails or is skipped | Reports use deterministic row IDs, project-relative paths, bounded messages, redacted local paths, and no telemetry, account identifiers, machine names, tokens, package feed credentials, repository secrets, temp paths containing usernames, control characters, or raw exception dumps. |
| AC17 | The story implementation considers IDE extension work | Scope is planned | Story 9-3 ships matrix, docs, conformance harness, and source/docs metadata. It does not create custom Visual Studio, Rider, or VS Code extensions unless party-mode review explicitly re-scopes the story. |
| AC18 | IDE behavior evidence is collected | A matrix row cannot be validated by stable automation | The row records whether evidence is CI-automated, scheduled, or manual release evidence. Manual evidence includes pinned IDE/version, OS, fixture, reproduction steps, expected result, observed result, sanitized artifact location, owner, last verified date, and revalidation trigger. |
| AC19 | Manual or scheduled evidence is accepted for a release gate | The conformance report evaluates the matrix | Each evidence artifact has a manifest that records row ID, fixture name, repository commit SHA, generated-output path contract version, IDE/extension/.NET SDK versions, OS/container image, validation command or manual steps, artifact hash, owner, last verified date, and expiration/revalidation trigger. Missing, stale, mismatched, or tampered manifests fail the release gate instead of degrading to a warning. |
| AC20 | The matrix or evidence manifests are parsed | A row is malformed, duplicated, unsupported, or references evidence outside the repo artifact root | Validation fails closed for duplicate row IDs, unknown tiers/gates/evidence types, missing owner or last verified date, absolute/local-user paths, path traversal, unsupported URI schemes, and evidence references that cannot be resolved to sanitized project-relative artifacts. |
| AC21 | Matrix rows, issue bodies, Markdown reports, CSV exports, or JSON evidence include values from IDE output or local tooling | The conformance harness writes artifacts | Output is encoded or escaped to prevent Markdown/HTML/script injection, spreadsheet formula injection, terminal control sequences, and clickable links to local absolute paths or secrets. Sanitization is tested with adversarial IDE names, diagnostic messages, file paths, and exception text. |
| AC22 | Version revalidation wants to file a GitHub issue | Authentication, labels, or network access are unavailable | The job produces a deterministic dry-run issue artifact with the same required fields and marks the release checklist item blocking. It must not drop the detected version drift silently or mutate repository content beyond the explicit report artifact. |
| AC23 | The conformance fixture runs on case-sensitive, case-insensitive, symlinked, or container-mounted workspaces | Generated-output paths and evidence paths are compared | Paths are normalized to project-relative forward-slash form for reports, but comparisons preserve case-sensitivity rules for the current filesystem. Evidence cannot escape the repository through symlinks, junctions, or container mount aliases. |

---

## Tasks / Subtasks

- [ ] T1. Publish the matrix contract (AC1-AC5, AC13-AC18)
  - [ ] Add `docs/ide-parity-matrix.md` with a stable table schema: row ID, capability, support tier, Visual Studio evidence, Rider evidence, VS Code + C# Dev Kit evidence, known limitation, test owner, and release gate.
  - [ ] Require each row to include IDE/version, OS or container image where applicable, fixture name, validation method, expected result, evidence artifact, owner, last verified date, known limitation, and revalidation trigger.
  - [ ] Use Must / Should / Out-of-scope exactly; avoid vague labels such as "partial" without a concrete limitation.
  - [ ] Include exact IDE/extension version pins, platform coverage, .NET SDK version, SourceTools package version, generated-output path version, and date last validated.
  - [ ] Add a note that Visual Studio is the calibration IDE only. The matrix, not Visual Studio, is the authoritative parity reference.
  - [ ] State C# Dev Kit licensing/prerequisite assumptions and unsupported OmniSharp-only behavior.
  - [ ] Define gate semantics: repo-owned generated-output failures block CI/release; vendor IDE limitations become pinned evidence rows plus revalidation issues unless they prove a FrontComposer output defect.
  - [ ] Include a machine-readable fenced block or generated JSON companion if implementation chooses to gate rows from structured data.
  - [ ] Define the evidence-manifest schema required by AC19, including commit SHA, fixture hash or artifact hash, generated-output path contract version, owner, expiration/revalidation trigger, and sanitized artifact location.
  - [ ] Validate matrix and manifest data fail-closed for duplicate row IDs, unsupported tiers/gates/evidence types, stale evidence, path traversal, absolute local paths, and unresolved evidence references.

- [ ] T2. Build the conformance fixture shape (AC2, AC6, AC12, AC16)
  - [ ] Add a small conformance sample under `samples/Counter` or a dedicated temporary fixture builder that includes `[Command]`, `[Projection]`, `[BoundedContext]`, customization metadata, XML docs, HFC diagnostics, and generated output.
  - [ ] Define deterministic fixture names, generated component/type names, expected HFC diagnostic IDs, expected XML doc targets, expected symbol-search terms, and expected generated output files.
  - [ ] Keep fixtures synthetic and deterministic; do not depend on the developer's local `obj/` output or uncommitted generated files.
  - [ ] Reuse Story 9-1 comparison and diagnostic seams plus Story 9-2 generated-output path contract.
  - [ ] Generate output under `obj/{Config}/{TFM}/generated/HexalithFrontComposer` through the package-owned path, not an IDE-specific virtual path.
  - [ ] Add matrix-row IDs to tests so CI failures point directly to matrix rows.
  - [ ] Keep path constants or templates referenced by tests and docs from one source where practical; do not introduce a second hardcoded generated-layout contract.
  - [ ] Record the fixture commit, generated-output path contract version, and deterministic fixture identity in each automated or manual evidence manifest.
  - [ ] Test generated/evidence path normalization on Debug/Release, multi-targeted fixtures, case-sensitive and case-insensitive path comparisons where available, and symlink/junction escape attempts.

- [ ] T3. Verify Must-tier language-service capabilities (AC6, AC11, AC12)
  - [ ] Add tests or scripted conformance probes for generated type completion visibility where automation is feasible.
  - [ ] Verify XML documentation exists on public attributes and generated symbols used by the sample.
  - [ ] Verify `DiagnosticDescriptor.HelpLinkUri` is present for HFC diagnostics that the IDE surfaces in this story's sample.
  - [ ] Verify generated output can be resolved to the public path contract for Debug/Release and a multi-targeted fixture.
  - [ ] Capture Visual Studio calibration evidence first: generated file path, symbol names, diagnostic IDs, navigation targets, and symbol-search terms.
  - [ ] Verify solution-wide symbol index evidence by scripted IDE automation where available, or by documented manual evidence rows when no vendor automation is reliable.

- [ ] T4. Document refactoring and source-generation workflows (AC7, AC8)
  - [ ] Document rename as domain-edit -> rebuild/regenerate -> inspect generated output -> verify diagnostics/output; do not promise in-place edits to generated files.
  - [ ] Add matrix rows for Find All References, Go to Definition, symbol search, code-fix application, hot reload after attribute edits, and generator-host debugging.
  - [ ] Record IDE-specific limitations as data in the matrix rather than footnotes that cannot be gated.
  - [ ] Link Story 9-2 CLI commands as the fallback inspection path when an IDE does not expose generated source clearly.

- [ ] T5. Split adopter debugging from contributor debugging (AC9, AC10)
  - [ ] Add or update `CONTRIBUTING.md` with contributor-only source-generator debugging guidance: `Debugger.Launch()`, JIT attach, compiler server cache behavior, rebuild/clean guidance, and Roslyn package pin sensitivity.
  - [ ] In adopter docs, focus on debugging generated application code behavior and known breakpoint/step-into limitations per IDE.
  - [ ] Do not put generator-host internals in the adopter matrix except as contributor-facing Should rows.
  - [ ] If Rider or VS Code cannot set breakpoints directly in generated files, record that as a limitation and require step-into/runtime-call evidence instead.

- [ ] T6. Add VS Code remote/container conformance (AC5, AC13, AC16)
  - [ ] Add a Dev Container or CI job path that opens/builds the conformance sample with VS Code + C# Dev Kit prerequisites documented.
  - [ ] Ensure the run can execute in CI without leaking Microsoft account identifiers, local paths, machine names, or extension telemetry.
  - [ ] If C# Dev Kit licensing, authentication, or headless limitations prevent reproducible CI, record a manual or scheduled evidence row instead of silently skipping validation.
  - [ ] Record known limitations for Remote-SSH, GitHub Codespaces, and Dev Containers.
  - [ ] Keep OmniSharp unsupported in v1; do not add a second VS Code support path inside this story.
  - [ ] Do not install, update, or accept licenses for IDEs/extensions inside CI. CI may read configured versions and validate repo-owned outputs; vendor UI evidence remains scheduled/manual unless the environment is already legal and reproducible.

- [ ] T7. Add version revalidation automation (AC3, AC14)
  - [ ] Store the supported IDE/extension range in a structured file or matrix block that CI can compare.
  - [ ] Add a scheduled or release-gate script that detects unsupported vendor major/minor changes from configured inputs and files a GitHub issue.
  - [ ] The issue must include matrix row IDs, current pin, detected version, IDE/SDK/OS, fixture used, expected behavior, observed behavior, required evidence, whether the Visual Studio calibration row passes, and release owner.
  - [ ] The job must be non-destructive and must not install/update IDEs, extensions, or git submodules recursively.
  - [ ] If GitHub issue creation cannot run because tokens, labels, or network access are unavailable, emit a deterministic dry-run issue artifact and mark the release checklist item blocking.

- [ ] T8. Scope CS1591 and public XML-doc requirements (AC11, AC15)
  - [ ] Add XML docs to public FrontComposer attributes and any public generated-facing contracts used by the conformance sample.
  - [ ] Keep CS1591 as warning before v1.0-rc1.
  - [ ] Add the planned `.editorconfig` glob-scoped behavior for API freeze, including exact public API/generated-facing globs, warning-only exclusions, and how `PublicAPI.Shipped.txt` controls the stricter set.
  - [ ] Ensure adopter templates receive scoped warning/error behavior without a flag-day project-wide break.

- [ ] T9. Tests and verification (AC1-AC18)
  - [ ] Matrix schema tests proving every Must row has a test owner, evidence type, version pin, and release gate.
  - [ ] Generated-output path contract tests for Debug/Release, multi-TFM, and stable forward-slash project-relative paths.
  - [ ] Diagnostic surface tests proving HFC IDs, severity, docs links, and XML docs are available to IDEs.
  - [ ] Sanitization tests for conformance logs containing local paths, usernames, machine names, account-like strings, control characters, temp paths with usernames, access tokens, package feed credentials, repository secrets, and raw exception text.
  - [ ] Injection-safety tests for Markdown tables, HTML snippets, JSON evidence, CSV exports, terminal logs, and generated issue bodies using adversarial IDE names, diagnostics, file paths, and exception messages.
  - [ ] Evidence manifest tests for missing commit SHA, fixture mismatch, stale last-verified date, tampered artifact hash, unresolved evidence path, duplicate row ID, unknown evidence type, and path traversal or symlink escape.
  - [ ] Version revalidation tests for in-range, lower-than-min, higher-minor, higher-major, unknown product, and missing version cases.
  - [ ] Version revalidation fallback tests proving unavailable GitHub auth/network produces a blocking dry-run issue artifact instead of a silent pass.
  - [ ] Container smoke test for the VS Code + C# Dev Kit path if CI credentials/prerequisites allow it; otherwise require documented manual evidence and a release-blocking checklist item.
  - [ ] Full regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.

---

## Dev Notes

### Existing SourceTools and Tooling State

- `Hexalith.FrontComposer.SourceTools` targets `netstandard2.0`, is marked `IsRoslynComponent=true`, and currently pins `Microsoft.CodeAnalysis.CSharp` to `4.12.0`.
- Keep IDE parity work from upgrading Roslyn broadly. Higher Roslyn versions have already been documented as analyzer load-context risk in `Directory.Packages.props`.
- Current generator tracking includes `Parse`, `ParseCommand`, and `ParseProjectionTemplate`; conformance and performance tests must preserve existing incremental-cache assertions.
- Current HFC diagnostics live in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`; HFC1056 and HFC1057 already show the `HelpLinkUri` pattern.
- Story 9-1 owns build-time drift detection and generated-output structural baselines. Story 9-2 owns CLI inspect/migrate and the public generated-output path. Story 9-3 consumes both.

### IDE Version and Vendor Reality Check

- Official Microsoft release notes identify **Visual Studio 2022 version 17.13** as the 17.13 product line. The story must not hardcode "Visual Studio 2026" unless official release evidence exists at v1 freeze.
- Microsoft documents C# Dev Kit as an enhanced VS Code C# experience that uses Visual Studio licensing terms for some usage. This is an adopter-facing prerequisite, not an implementation detail.
- VS Code navigation docs describe Go To Definition support, but generated-source parity must still be proven by the conformance sample because source generators and generated paths are the hard case.
- JetBrains Rider 2026.1 docs state Rider can inspect and navigate generated source and use generated symbols, but generated-code breakpoint behavior has limitations. Treat direct generated-file breakpoints and step-into debugging as separate matrix rows.
- The matrix should prefer exact evidence over assumptions. If a vendor behavior is not automatable in CI, record the manual evidence owner and make the release gate explicit.

### Advanced Elicitation Hardening

These hardening points were applied by `/bmad-advanced-elicitation 9-3-ide-parity-and-developer-experience` on 2026-05-03 and refine the party-mode evidence model without expanding product scope.

- Evidence freshness is part of the contract. Manual and scheduled IDE evidence must carry a manifest tied to the repository commit, fixture identity, generated-output path contract version, artifact hash, owner, last verified date, and revalidation trigger.
- Matrix validation fails closed. Duplicate row IDs, unknown support tiers, unknown gates, unresolved evidence, stale manifests, absolute local paths, path traversal, unsupported URI schemes, and symlink/junction escapes are release-gate failures.
- Sanitization covers generated reports as well as logs. Markdown tables, JSON evidence, CSV exports, terminal output, and generated issue bodies must be encoded against formula injection, Markdown/HTML/script injection, terminal control sequences, local absolute links, and secret-looking values.
- Version revalidation has a deterministic no-network fallback. If GitHub issue creation is unavailable, the job writes a dry-run issue artifact with the same fields and marks the release checklist item blocking.
- CI must not install or update vendor IDEs/extensions or accept licenses. Repo-owned checks stay automated; vendor UI evidence stays manual/scheduled unless the environment is already legal and reproducible.

### Matrix Row Shape

Recommended row shape:

```markdown
| Row ID | Capability | Tier | Visual Studio | Rider | VS Code + C# Dev Kit | Fixture | Validation | Evidence | Owner | Last Verified | Limitation | Gate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| IDE-MUST-001 | Generated type completion | Must | Pass: VS 2022 17.13+ on Windows | Pass: Rider 2026.1.x on macOS/Linux | Pass: VS Code + C# Dev Kit pinned minor in Dev Container evidence | `IdeParityCounterFixture` | CI path contract + manual IDE capture | `artifacts/ide-parity/IDE-MUST-001.json` | SourceTools | 2026-05-03 | C# Dev Kit requires licensed prerequisite | CI for repo-owned output; release checklist for vendor UI |
```

Each row must be actionable: a developer should know what capability is expected, where evidence lives, and what to do when it fails.

### Validation Evidence and Gate Semantics

- Repo-owned checks are CI-blocking when deterministic: fixture generation, generated-output path resolution, diagnostics and help links, XML docs, symbol output, NFR8 timing budget, matrix schema, and log sanitization.
- Vendor IDE behavior is release evidence unless a stable automation harness exists. A vendor UI/indexing difference blocks release only when the evidence points to a FrontComposer generated-output defect; otherwise record the pinned limitation and file a revalidation issue.
- Manual evidence rows must include pinned IDE/version, OS or container image, fixture, reproduction steps, expected result, observed result, sanitized artifact path, owner, last verified date, and revalidation trigger.
- Retries are allowed for environment setup only. Assertion failures, missing generated output, missing XML docs, missing diagnostics, or unsanitized logs are product/test failures, not flaky infrastructure.

### Minimum Conformance Fixture Set

- Generated-source go-to-definition from Razor/component usage.
- XML docs surfaced from public attributes and generated-facing symbols.
- HFC diagnostics visible on source inputs with `DiagnosticDescriptor.HelpLinkUri`.
- Symbol search finds generated type/member names from the deterministic sample.
- Rename workflow proves domain source edit -> rebuild/regenerate -> inspect generated output; direct generated-code rename is unsupported.
- NFR8 generator performance records cold and warm expectations separately using the existing benchmark boundary.

### Conformance Harness Boundaries

- Prefer scripted probes and deterministic fixtures over brittle UI automation. Use IDE automation only where vendor tooling is stable enough.
- A matrix row can be gated by documented manual evidence only when no reliable headless automation exists; Must rows still need release-owner signoff.
- Keep conformance evidence small and sanitized. Do not persist screenshots with account names, machine names, absolute paths, private solution paths, or extension telemetry.
- Do not add custom IDE extensions in this story. If a capability requires a custom extension to be true, record the gap and defer a product/architecture decision.
- Do not initialize or update git submodules for conformance. Read root-level submodule boundaries only if a test must exclude them.

### Generated Output and Debugging Contract

- Public generated path from Story 9-2:

```text
obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs
```

- Sibling `.g.cs` files for Fluxor actions, reducers, registration, MCP manifest, baseline metadata, and command lifecycle support are part of the generated-source family.
- IDE Go To Definition may resolve to virtual generated files, on-disk generated files, metadata views, or no source depending on vendor support. Story 9-3's job is to capture and gate the supported result, not force every IDE into the same implementation.
- Generated files remain read-only by design. Rename/refactor workflows operate on domain source and regenerate outputs.
- Story 9-3 verifies the generated-output layout as a consumer of Story 9-2. It must not change generator output layout except for tests or documentation that prove the existing contract.

### CS1591 and XML Docs

- XML docs are required for public attributes and public generated-facing contracts that appear in IDE Quick Info.
- Do not turn CS1591 into a project-wide Error before API freeze. That would create adoption churn unrelated to IDE parity.
- At v1.0-rc1, scope stricter CS1591 enforcement through `.editorconfig` globs matching public API files and ship the same scoped pattern in adopter templates.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 1-8 | Story 9-3 | Hot-reload/full rebuild messaging and NFR8 generator performance expectations. |
| Stories 1-4 through 1-5 | Story 9-3 | Incremental generator pipeline, parse/transform/emit split, and generated hint naming. |
| Stories 4-1 through 4-6 | Story 9-3 | Projection roles, DataGrid/rendering output, badges, field groups, and generated UI metadata visible in IDEs. |
| Stories 6-2 through 6-6 | Story 9-3 | Template/slot/view override contracts and contract-version diagnostics. |
| Story 7-3 | Story 9-3 | Authorization policy diagnostics and XML-doc quick info for policy attributes. |
| Stories 8-1 through 8-6 | Story 9-3 | MCP manifest generated outputs and schema/version generated-source families. |
| Story 9-1 | Story 9-3 | Drift diagnostics, baseline comparison seams, HelpLinkUri expectations, and performance/caching tests. |
| Story 9-2 | Story 9-3 | Generated-output path contract and CLI fallback inspection path. |
| Story 9-4 | Story 9-3 | Final diagnostic ID docs pages, PublicAPI governance, and deprecation docs links. |
| Story 9-5 | Story 9-3 | Public DocFX publication of IDE parity, onboarding, and troubleshooting docs. |

### Scope Guardrails

Do not implement these in Story 9-3:

- Source generator drift detection. Owner: Story 9-1.
- CLI inspect/migrate commands. Owner: Story 9-2.
- Final diagnostic ID governance and deprecation policy. Owner: Story 9-4.
- Full DocFX documentation site. Owner: Story 9-5.
- Custom Visual Studio, Rider, or VS Code extensions unless the story is explicitly re-scoped after party-mode review.
- Broad Roslyn package upgrades.
- OmniSharp-only VS Code support path.
- Recursive submodule initialization or nested submodule scans.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Final diagnostic documentation URLs for all HFC IDs. | Story 9-4 / Story 9-5 |
| DocFX publication of the IDE parity matrix and onboarding docs. | Story 9-5 |
| Visual/specimen accessibility gates for generated UI screenshots. | Story 10-2 |
| Mutation testing expansion for diagnostics and matrix parser. | Story 10-4 |
| Vendor-specific custom IDE extensions if matrix evidence proves built-in tooling cannot meet Must rows. | Deferred product/architecture decision |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-9-developer-tooling-documentation.md#Story-9.3`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md`] - drift diagnostics, generated-output baseline, diagnostic docs, and performance handoff constraints.
- [Source: `_bmad-output/implementation-artifacts/9-2-cli-inspection-and-migration-tools.md`] - CLI inspect fallback and public generated-output path contract.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review vs. elicitation sequencing and hardening roles.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - source generator as infrastructure and IDE DX constraint.
- [Source: `Directory.Packages.props`] - Roslyn, Fluent UI, MCP, and test package pins.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] - analyzer/generator target framework and Roslyn component packaging constraints.
- [Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`] - current incremental generator pipeline.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] - HFC descriptor and HelpLinkUri pattern.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs`] - incremental caching regression pattern.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs`] - NFR8 benchmark harness.
- [Source: Microsoft Learn `Visual Studio 2022 version 17.13 Release Notes`](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.13) - official Visual Studio 17.13 product naming.
- [Source: Microsoft Learn `C# Dev Kit for Visual Studio Code`](https://learn.microsoft.com/en-us/visualstudio/subscriptions/vs-c-sharp-dev-kit) - C# Dev Kit feature and licensing model.
- [Source: VS Code Docs `Navigate and Edit`](https://code.visualstudio.com/docs/csharp/navigate-edit) - C# Dev Kit navigation feature baseline.
- [Source: VS Code Docs `C# Dev Kit FAQ`](https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq) - C# Dev Kit scope, closed-source extension note, and troubleshooting areas.
- [Source: JetBrains Rider Docs `Work with C# source generators`](https://www.jetbrains.com/help/rider/Source_generators.html) - generated-source inspection, navigation, and debugging limitations.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-03: Story created via `/bmad-create-story 9-3-ide-parity-and-developer-experience` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-03: Party-mode review applied evidence-model, gate-semantics, fixture-contract, sanitization, and Story 9-2 ownership hardening. Ready for advanced elicitation on a later run.
- 2026-05-03: Advanced elicitation applied evidence-manifest freshness, fail-closed matrix validation, injection-safe report output, dry-run version revalidation fallback, and vendor tooling license/installation guardrails. Ready for development.

### File List

(to be filled in by dev agent)

## Party-Mode Review

- Date/time: 2026-05-03T11:36:53+02:00
- Selected story key: `9-3-ide-parity-and-developer-experience`
- Command/skill invocation used: `/bmad-party-mode 9-3-ide-parity-and-developer-experience; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: The round found that the story's direction is valuable, but the original wording risked turning IDE parity into subjective vendor certification. The agents asked for sharper repo-owned contract ownership, explicit matrix/evidence schema, deterministic fixtures, clear CI/release/manual gate semantics, C# Dev Kit/container fallback behavior, flakiness controls, and stronger sanitization requirements.
- Changes applied: Added repo-owned vs vendor-evidence framing; expanded AC1, AC2, AC12, AC13, AC14, AC16, and AC18; hardened T1-T9 with matrix schema, deterministic fixture expectations, Visual Studio calibration evidence, single-source generated-path contract guidance, container fallback, issue-template fields, CS1591 globs, and sanitization vectors; added validation gate semantics and minimum fixture sections; clarified Story 9-3 consumes rather than changes Story 9-2 generated-output layout.
- Findings deferred: Exact pinned IDE/extension versions, whether Rider/VS Code evidence is automated or manual for each row, screenshot/video vs structured checklist evidence format, and vendor-update issue cadence remain implementation-time matrix decisions.
- Final recommendation: `ready-for-dev`

## Advanced Elicitation

- Date/time: 2026-05-03T11:42:46+02:00
- Selected story key: `9-3-ide-parity-and-developer-experience`
- Command/skill invocation used: `/bmad-advanced-elicitation 9-3-ide-parity-and-developer-experience`
- Batch 1 method names: Security Audit Personas; Pre-mortem Analysis; Failure Mode Analysis; First Principles Analysis; Occam's Razor Application.
- Reshuffled Batch 2 method names: Red Team vs Blue Team; Chaos Monkey Scenarios; Self-Consistency Validation; Comparative Analysis Matrix; Hindsight Reflection.
- Findings summary: The elicitation found that the party-mode evidence model was directionally sound, but still needed stronger freshness, tamper-resistance, path-boundary, report-injection, and no-network fallback rules. It also confirmed that the story should keep vendor IDE automation modest and avoid turning CI into an IDE installer or license-acceptance surface.
- Changes applied: Added AC19-AC23; hardened T1, T2, T6, T7, and T9; added Advanced Elicitation Hardening notes; updated completion notes and trace. The accepted changes stay within the existing matrix, evidence, conformance, and release-gate scope.
- Findings deferred: Exact evidence expiration windows, exact artifact hash algorithm, final evidence artifact directory layout, exact issue-file naming convention, and which vendor IDE rows can graduate from manual to scheduled/automated evidence remain implementation-time decisions.
- Final recommendation: `ready-for-dev`
