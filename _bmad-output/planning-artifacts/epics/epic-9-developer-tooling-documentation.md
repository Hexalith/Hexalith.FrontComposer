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
I want equivalent development experience across Visual Studio, JetBrains Rider, and VS Code with C# Dev Kit,
So that I can use my preferred IDE without losing IntelliSense, navigation, or debugging capabilities.

**Acceptance Criteria:**

**Given** Visual Studio 2026 (reference IDE)
**When** a developer works with FrontComposer source-generated types
**Then** IntelliSense provides completions for generated types and their members
**And** hover documentation shows XML doc comments from generated code
**And** go-to-definition navigates to the generated source file
**And** source generator debugging is supported (breakpoints in generator code)

**Given** JetBrains Rider 2026.1+
**When** a developer works with FrontComposer
**Then** all capabilities available in Visual Studio are also available in Rider (parity)
**And** any known Rider-specific limitations are documented

**Given** VS Code with C# Dev Kit
**When** a developer works with FrontComposer
**Then** IntelliSense, hover documentation, and go-to-definition work for generated types
**And** source generator debugging may have limitations (documented)
**And** the experience is sufficient for lightweight-tooling adopters

**Given** any IDE
**When** the developer hovers over a framework attribute (e.g., [Projection], [BoundedContext])
**Then** XML doc comments describe: what the attribute does, what it generates, and a link to documentation

**Given** CS1591 (missing XML doc comments) enforcement
**When** the project is pre-v1.0-rc1
**Then** CS1591 is a warning
**When** the project is at or past v1.0-rc1 (API freeze milestone)
**Then** CS1591 is an error for all types in PublicAPI.Shipped.txt

**References:** FR65, NFR71, NFR92

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
