# Epic 9: Developer Tooling & Documentation

Developer has CLI tools (inspect generator output, migration), build-time drift detection, IDE parity (VS/Rider/VS Code), diagnostic ID ranges with doc links, deprecation with migration paths, and Diataxis-genre documentation site. Built incrementally alongside earlier epics.

### Story 9.1: Build-Time Drift Detection

As a developer,
I want the framework to detect mismatches between my backend domain declarations and the generated UI at build time,
So that I catch breaking changes as compile-time errors instead of discovering them as silent runtime bugs.

**Acceptance Criteria:**

**Given** a [Projection]-annotated type in the domain assembly
**When** the source generator compares the current type shape against the previously generated output
**Then** any drift is surfaced as a compile-time diagnostic (not runtime silent behavior)
**And** the diagnostic identifies: which type changed, what property was added/removed/modified, and the impact on the generated UI

**Given** a domain property is renamed
**When** the build runs
**Then** a diagnostic is emitted: "Property '{OldName}' was expected on {TypeName} but not found. '{NewName}' was added. If this is a rename, update the generated output. See HFC{id}."
**And** the diagnostic includes a documentation link with resolution steps

**Given** a domain property type changes (e.g., string -> int)
**When** the build runs
**Then** a diagnostic is emitted warning that the generated form input and DataGrid column will change rendering behavior
**And** the severity is Warning (not Error) to allow intentional changes to proceed

**Given** a [BoundedContext] name changes
**When** the build runs
**Then** a diagnostic is emitted that navigation sections will be affected
**And** persisted session state referencing the old context name will not restore

**Given** drift detection runs
**When** performance is measured
**Then** drift detection does not add measurable overhead beyond the existing incremental generator pipeline (<500ms budget, NFR8)

**References:** FR7, NFR8, NFR97 (teaching errors at compile time)

---

### Story 9.2: CLI Inspection & Migration Tools

As a developer,
I want CLI tools to inspect what the source generator produced and to apply automated code fixes when upgrading framework versions,
So that I can debug generation issues and upgrade confidently without manual code changes.

**Acceptance Criteria:**

**Given** a developer runs the CLI inspect command for a specific domain type
**When** the command executes
**Then** the source generator output for that type is displayed from a deterministic file path (obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs)
**And** the output includes: generated Razor component, Fluxor state types, domain registration, and any diagnostics emitted

**Given** the developer wants to see all generated output
**When** the CLI inspect command is run without a type filter
**Then** a summary is displayed: count of generated forms, grids, registrations, and any warnings/errors
**And** each generated file is listed with its path

**Given** a framework version upgrade with breaking API changes
**When** the developer runs the CLI migration tool
**Then** Roslyn analyzer code fixes are applied automatically for known migration patterns
**And** each applied fix is reported with: what changed, why, and the diagnostic ID
**And** the developer can review changes before committing (dry-run mode available)

**Given** the migration tool encounters a change it cannot auto-fix
**When** the manual fix is required
**Then** a clear message describes: what needs to change, where, and links to the migration guide

**Given** the CLI tools
**When** they are distributed
**Then** they are available as dotnet global tools or local tools via the framework's NuGet package

**References:** FR63, FR64, NFR77 (deprecation window)

---

### Story 9.3: IDE Parity & Developer Experience

As a developer,
I want an equivalent development experience across Visual Studio, JetBrains Rider, and VS Code with C# Dev Kit — defined by a conformance matrix, not by any single vendor's feature set,
So that I can use my preferred IDE without losing IntelliSense, navigation, refactoring, or debugging capabilities, and so "parity" means something testable rather than aspirational.

**Acceptance Criteria:**

**Given** the IDE Parity Conformance Matrix
**When** Story 9.3 is considered complete
**Then** `docs/ide-parity-matrix.md` is published and lists every capability × IDE × support tier (Must / Should / Out of scope)
**And** the matrix is the authoritative definition of parity for this story (no prose claim overrides it)
**And** CI gates on the Must tier rows — a regression in any Must row fails the conformance suite for the affected IDE

**Given** the calibration IDE policy
**When** parity claims are communicated externally
**Then** Visual Studio 2026 is named as the *calibration IDE* used to baseline the conformance suite (not as "the reference")
**And** the conformance matrix — not any single IDE — is the authoritative parity reference
**And** Mac-only adopters have a first-class path (Rider or VS Code + Dev Kit); VS-for-Windows is not assumed

**Given** the tested IDE version pin list
**When** v1 ships
**Then** the following exact versions are part of the conformance suite: Visual Studio ≥ 17.13, JetBrains Rider 2026.1.2 through 2026.2.x, VS Code with C# Dev Kit ≥ the vendor-current minor at v1 freeze
**And** a vendor-side minor/major bump outside the pinned range auto-files a "conformance revalidation needed" GitHub issue
**And** the pinned range is published in the parity matrix and updated per release

**Given** the C# Dev Kit prerequisite for VS Code
**When** VS Code parity is claimed
**Then** C# Dev Kit is a stated prerequisite (adopters know a Microsoft account / proprietary extension is required)
**And** OmniSharp (the non-Dev Kit path) is explicitly documented as *unsupported in v1* — not silently broken
**And** the Dev Kit license implication is called out in the adopter onboarding doc as an acknowledged assumption

**Given** any IDE in the matrix
**When** a developer works with FrontComposer source-generated types
**Then** the Must-tier capabilities hold: IntelliSense completions on generated types, hover docs from XML comments, go-to-definition to the generated source file, HFC diagnostic squiggles, solution-wide symbol search includes generated types, and incremental generator performance within the NFR8 500 ms budget

**Given** any IDE in the matrix
**When** a developer invokes Should-tier capabilities
**Then** Find All References crosses the generator boundary (domain symbol ↔ generated usage)
**And** Rename refactoring is documented as a workflow: edit the domain symbol, let the generator regenerate, and verify the generated Razor/Fluxor output updates — generated files remain read-only by design
**And** code-fix (analyzer) application, hot reload on domain attribute edits, and generator-host breakpoints are supported per the matrix; any IDE-specific limitations are enumerated, not left to a "may have limitations" clause

**Given** generator debugging
**When** the audience is the adopter
**Then** the adopter-facing AC is: breakpoints in generated code (`*.g.razor.cs` / `*.g.cs`) are supported in every IDE on the Must tier
**When** the audience is the framework contributor
**Then** contributor-facing generator-host debugging (`Debugger.Launch()` protocol, JIT attach to the generator host) is documented in `CONTRIBUTING.md`, not in this adopter-facing story

**Given** a framework attribute (e.g., `[Projection]`, `[BoundedContext]`)
**When** the developer hovers over it in any IDE in the matrix
**Then** XML doc comments describe: what the attribute does, what it generates, and a link to the diagnostic/documentation page

**Given** the generator output path contract
**When** consumers (CLI inspect tools, IDE go-to-definition, adopter build scripts) rely on the location
**Then** `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs` (and sibling `.g.cs`) is treated as a public contract
**And** a contract test guards path stability across .NET SDK bumps

**Given** remote and containerized development environments
**When** a developer works via Remote-SSH, GitHub Codespaces, or Dev Containers
**Then** at least one end-to-end conformance run executes in a containerized VS Code + Dev Kit environment before v1 ships
**And** known limitations (e.g., generator debugging in Codespaces) are enumerated in the parity matrix

**Given** CS1591 (missing XML doc comments) enforcement
**When** the project is pre-v1.0-rc1
**Then** CS1591 is a warning for all public types
**When** the project is at or past v1.0-rc1 (API freeze milestone)
**Then** CS1591 is scoped via editorconfig file-globbing to only the files backing `PublicAPI.Shipped.txt` — not a blanket project-wide toggle
**And** the adopter project template ships this editorconfig so downstream CI does not experience a flag-day break
**And** non-public API files retain warning (not error) behavior to avoid incidental churn

**Dependencies:**
- Blocked on the IDE-specific test fixture infrastructure referenced in architecture §"Generator Diagnostics & DX" (item #11). Story 9.3 cannot ship without this conformance harness.
- Companion ADR: *ADR-0XX — IDE Parity Conformance Testing* (owns the matrix format, CI gating, and revalidation trigger).

**References:** FR65, NFR8, NFR71, NFR77, NFR92

---

### Story 9.4: Diagnostic ID System & Deprecation Policy

As a developer,
I want every framework diagnostic to resolve to a documentation page, and deprecated APIs to have clear migration paths,
So that I can self-service resolve any issue and plan upgrades without surprises.

**Acceptance Criteria:**

**Given** the diagnostic ID scheme
**When** IDs are assigned
**Then** reserved ranges are enforced per package:
**And** Contracts: HFC0001-0999
**And** SourceTools: HFC1000-1999
**And** Shell: HFC2000-2999
**And** EventStore: HFC3000-3999
**And** Mcp: HFC4000-4999
**And** Aspire: HFC5000-5999

**Given** any diagnostic emitted by the framework
**When** the developer sees the diagnostic ID
**Then** the ID resolves to a consistent, lookup-addressable documentation page
**And** the documentation page includes: problem description, common causes, resolution steps, and code examples

**Given** a framework API is deprecated
**When** the deprecation is applied
**Then** a minimum one-minor-version window is provided before removal (NFR77)
**And** the [Obsolete] message follows convention: "<old> replaced by <new> in v<target>. See HFC<id>. Removed in v<removal>."
**And** the diagnostic ID links to a migration path

**Given** binary compatibility within minor versions
**When** PublicApiAnalyzers run in CI
**Then** accidental breaking changes within a minor version fail CI (NFR69, NFR76)
**And** intentional breaking changes require a major version bump

**References:** FR66, FR67, NFR69, NFR76, NFR77, NFR80

---

### Story 9.5: Diataxis Documentation Site

As a developer,
I want a comprehensive documentation site organized by learning need, with a day-1 customization cookbook,
So that I can find tutorials when learning, how-tos when building, reference when checking, and concepts when understanding.

**Acceptance Criteria:**

**Given** the documentation site
**When** it is generated
**Then** DocFX produces the site (not Blazor-native SSG, NFR95)
**And** the site is organized into four Diataxis genres:
**And** **Tutorials**: step-by-step learning paths (e.g., "Build your first FrontComposer domain")
**And** **How-to guides**: task-oriented recipes (e.g., "How to override a field renderer")
**And** **Reference**: API documentation, attribute catalog, diagnostic ID lookup
**And** **Explanation/Concepts**: architectural decisions, design philosophy, pattern rationale

**Given** the single-source documentation strategy
**When** documentation is authored
**Then** explicit narrative vs. reference section markers separate the two rendering targets
**And** the MCP renderer strips narrative sections (returns reference only)
**And** the DocFX site keeps both narrative and reference
**And** this prevents voice collapse between human docs and agent docs (NFR96)

**Given** v1 launch
**When** the documentation site is published
**Then** the day-1 highest-leverage document is the customization gradient cookbook (NFR98)
**And** the cookbook shows the same problem solved at each of the four gradient levels
**And** it includes copy-pasteable code examples for each level

**Given** a framework change that breaks a shipped skill corpus example
**When** the change is merged
**Then** a migration guide is required regardless of semantic version bucket (FR69)
**And** the migration guide is published on the documentation site linked from the relevant diagnostic ID

**Given** error messages in the framework
**When** they are authored
**Then** the error message template (Expected/Got/Fix/DocsLink) is part of the attribute definition
**And** the source generator test enforces the template is filled in (build will not ship without it, NFR97)

**References:** FR68, FR69, NFR95-98

---

**Epic 9 Summary:**
- 5 stories covering all 8 FRs (FR7, FR63-69)
- Relevant NFRs woven into acceptance criteria (NFR8, NFR69, NFR71, NFR76-77, NFR80, NFR92, NFR95-98)
- Built incrementally: drift detection (with Epic 1 generator), CLI tools (with Epic 1-2), IDE parity (ongoing), diagnostics (with each package), docs (with v1 launch)
- Stories are sequentially completable: 9.1 (drift detection) -> 9.2 (CLI tools) -> 9.3 (IDE parity) -> 9.4 (diagnostics/deprecation) -> 9.5 (documentation site)

---
