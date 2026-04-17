# Developer Tool Specific Requirements

*Revised after Party Mode round 2 (Winston, Amelia, Paige, Barry). Package family collapsed from 11 to 8 headline packages, Layer 4 runtime services marked `[Experimental]` through v1.1, reference microservices cut from 5 to 3, documentation toolchain decided (DocFX), teaching errors moved from discipline to compile-time enforcement, most CI gates relocated to Non-Functional Requirements (Step 10), and a solo-maintainer sustainability filter introduced as a PRD-wide constraint all subsequent steps must honor.*

### Solo-Maintainer Sustainability Filter

Every requirement in this section and later sections must pass the test: **"Can a single maintainer sustain this over a 6-month v1 AND a 12-month v1.x without the CI matrix eating the time to build the actual framework?"**

Party Mode round 2 flagged the initial Step 7 draft as over-engineered for solo delivery. The core critique: it was specifying definition-of-done bars before the source generator existed. This filter is now a PRD-wide discipline:

- **Commitments that survive solo maintenance are the real commitments.** Everything else is v1.x+ aspiration and must be labeled as such.
- **Ceremony is the enemy.** Every NuGet package, every CI gate, every doc page, every test suite is a maintenance surface. The question is not "is this a good idea?" but "is this a good idea that can still be maintained at 2am after a release?"
- **Ship something embarrassing early rather than something perfect late.** Target a usable v0.1 at week 4 of implementation, not a perfect v1.0 at month 6. Iteration over specification.
- **The PRD is a hypothesis, not a contract.** Requirements documented here are the current best understanding; any of them can be replaced by implementation reality.

### Project-Type Overview

Hexalith.FrontComposer ships as a family of NuGet packages consumed by .NET developers building event-sourced microservice frontends on Hexalith.EventStore. The framework's "product surface" is the combination of: (1) NuGet packages, (2) the C# attribute and service API, (3) source-generator-emitted partial types that shape consumer code, (4) the `dotnet new hexalith-frontcomposer` project template, (5) the in-process MCP server exposing domain models as typed agent tools plus the skill corpus, and (6) the DocFX-generated documentation site. Each surface must be designed together — a weakness in any one undermines the others.

### Language Matrix

| Language / Runtime | v1 Support | Rationale |
|---|---|---|
| **C# on .NET 10** | ✅ First-class, primary | Framework is implemented in C# targeting .NET 10; all consumer APIs, partial types, and source generators target this runtime. |
| **F# on .NET 10** | ⚠️ Usable, not tested | F# can consume the framework's C# API but will not benefit from typed partial types or attribute-driven customization gradient ergonomically. |
| **VB.NET** | ❌ Not supported | Attribute surface assumes C# idioms. Build against VB.NET projects is untested. |
| **.NET 8 / .NET 9** | ❌ Not v1 | .NET 10 features are load-bearing. Back-porting adds unsustainable maintenance burden. |
| **Blazor Server (dev loop) + Blazor Auto (production)** | ✅ Primary | Per UX spec. |
| **Blazor WebAssembly standalone** | ✅ Supported, not primary | Works via the same Shell but is not the default configuration. |
| **Blazor Hybrid (MAUI/WPF/WinForms WebView)** | ❌ Out of scope for v1 | Known Fluent UI Blazor v5 integration gaps; insufficient testing capacity for solo maintainer. |

### NuGet Package Family

**8 headline packages**, collapsed from an earlier 11-package draft per Party Mode critique (Winston). `.Contracts` stays separate and dependency-free so domain assemblies can reference it without pulling Blazor. `.Generators` + `.Analyzers` merged into `.SourceTools`. `.McpServer` + `.Skills` merged into `.Mcp` (the skill corpus IS the MCP server's payload; version skew between them is the most painful possible failure). `.Templates` ships as a standalone `dotnet new` registration, not counted in the runtime family.

| Package | Purpose | Audience |
|---|---|---|
| **`Hexalith.FrontComposer`** | Meta-package that pulls in Shell + Contracts + SourceTools + EventStore. Default install for new projects. | All consumers |
| **`Hexalith.FrontComposer.Contracts`** | Typed attributes, gradient context types, rendering contract interfaces. **Tiny, dependency-free.** Domain assemblies reference this without pulling Blazor. | All consumers (both server-side domain assemblies and client-side shell) |
| **`Hexalith.FrontComposer.Shell`** | Composition shell, lifecycle wrapper, nav groups, command palette, session persistence, density preference, theme toggle. | Web surface consumers |
| **`Hexalith.FrontComposer.SourceTools`** | Roslyn source generators (typed partial types, one-source-three-outputs) + Roslyn analyzers (gradient compatibility, WCAG violations, build-time teaching errors). Merged from `.Generators` + `.Analyzers`. | All consumers (analyzer/generator reference, not runtime dependency) |
| **`Hexalith.FrontComposer.EventStore`** | Hexalith.EventStore integration layer. `ICommandService`, `IQueryService`, `ISignalRSubscriptionService`, ETag caching. | Consumers using Hexalith.EventStore as the backend (v1: all consumers) |
| **`Hexalith.FrontComposer.Mcp`** | In-process MCP server + skill corpus. Exposes domain models as typed agent tools + Markdown projection resources. Two-call lifecycle pattern. Typed-contract hallucination rejection. | Consumers wanting the chat surface / LLM-native story |
| **`Hexalith.FrontComposer.Aspire`** | .NET Aspire hosting extensions, `.WithDomain<T>()`, DAPR component templates. | Consumers using Aspire (v1: all consumers) |
| **`Hexalith.FrontComposer.Testing`** | xUnit + Shouldly + bUnit + Playwright + FsCheck test utilities, projection snapshot testing helpers, SignalR fault-injection wrappers. | Framework contributors + adopter test suites |

**Separately distributed:**

- **`Hexalith.FrontComposer.Templates`** — `dotnet new` project template. Versions on its own cadence, not part of the runtime family.
- **`Hexalith.FrontComposer.Cli`** (as `dotnet tool`) — includes `dotnet hexalith dump-generated <Type>` for inspecting source-generated output (per Amelia's fix for the generator-black-box pain point) and `dotnet hexalith migrate` for Roslyn-analyzer-driven cross-version migrations.

**Package distribution rules:**

- Published to nuget.org with semantic versioning via semantic-release from conventional commits.
- Pre-release versions use NuGet's prerelease suffix convention.
- Package signing with an OSS-signing certificate for stable releases. Pre-releases may be unsigned.
- SBOM generation (CycloneDX) per release. (Detail captured in Step 10 NFRs.)
- Symbols (`.snupkg`) published for IDE debugging.

**Contingency note:** Barry's Party Mode critique argued for 3 packages maximum. The 8-package choice is defensible but must be validated against real solo-maintainer load in the first 3 months of implementation. If maintenance overhead is unsustainable, collapse to 5 (meta + Contracts + Shell + SourceTools + EventStore) with Mcp/Aspire/Testing as optional installs under the meta-package umbrella.

### Versioning Model

**Lockstep versioning is a v1 constraint**, not a permanent rule. All 8 headline packages version together — a `Shell 1.3.0` is only compatible with `Contracts 1.3.0`, not `1.2.0` or `1.4.0`. Cross-package version mismatches are a build error enforced by the meta-package's dependency constraints.

**Rationale:** For a solo maintainer shipping frequently, independent per-package versioning is a matrix-testing nightmare that will consume the time the framework itself needs. Lockstep is the honest solo-dev trade-off.

**v2 escape hatch:** at v2.0, split into **compile contract** (Contracts, Shell, SourceTools — lockstep) and **satellites** (EventStore, Mcp, Aspire, Testing — independent within a compatible range). Adopters in v1 who want partial upgrades are told explicitly in the README: *"Pin the meta-package. This is v1. Partial upgrade paths arrive in v2."*

Binary compatibility within a minor version is enforced via `PublicApiAnalyzers` that fails CI on accidental breaking changes. Detail captured in Step 10 NFRs.

### Installation Methods

**Primary: NuGet meta-package**

```bash
dotnet add package Hexalith.FrontComposer
```

Pulls in Shell + Contracts + SourceTools + EventStore. Opt-in packages (`.Mcp`, `.Aspire`, `.Testing`) are added explicitly.

**Project template**

```bash
dotnet new install Hexalith.FrontComposer.Templates
dotnet new hexalith-frontcomposer -n MyCompany.MyProject
```

Scaffolds: Aspire AppHost, Counter sample microservice, FrontComposer shell registration, DAPR components directory, `.mcp.json` for Claude Code / Cursor integration, `.gitignore`, `README.md`, `docker-compose.yml` for one-command local topology startup.

**Global CLI tool**

```bash
dotnet tool install -g Hexalith.FrontComposer.Cli --prerelease
```

Provides: `dotnet hexalith dump-generated <Type>` (inspect source-generator output for a specific domain type — fixes Amelia's generator-black-box concern), `dotnet hexalith migrate` (run Roslyn analyzers and apply safe code fixes for cross-version migrations).

### API Surface

Five concentric layers. 90% of usage sits in Layer 1. Layer 4 is explicitly `[Experimental]` through v1.1, with stability committed at v1.2, because runtime service signatures will learn from real adoption in the first 6 months.

**Layer 1 — Attribute-driven declarative surface (STABLE after v1.0)**

| Attribute / Type | Purpose |
|---|---|
| `[Command]` | Marks a record as a domain command. Implies form generation, MCP tool emission, lifecycle wrapper. |
| `[Projection]` | Marks a type as a read model projection. Implies DataGrid / detail generation and MCP resource exposure. |
| `[BoundedContext(name)]` | Associates a domain with a nav group with optional display label (domain language, not architecture name). Build-time check validates the name is not a typo of an existing BC in the solution. |
| `[ProjectionRole(role)]` | Rendering role hint: `ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`. Permanently capped at 5 slots. |
| `[ProjectionBadge(slot)]` | Maps a status enum value to a semantic badge slot: `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Accent`. |
| `[ProjectionFieldSlot<TProjection>(x => x.Field)]` | Applied to a custom Blazor component; declares it as a slot-level override for a specific projection field. **Uses generic type parameter + lambda expression for refactor safety** — not `nameof` strings. This ergonomic change was made after Amelia's Party Mode critique. |
| `[RequiresPolicy(policyName)]` | Command-level authorization attribute. Integrates with ASP.NET Core authorization policies. **Added to Layer 1 per Party Mode (Amelia)** — missing it from the first draft was an oversight; without this attribute, security is an adopter's manual wiring job. |
| `[DisplayName]`, `[Description]` | Standard .NET attributes consumed by the label resolution chain. |

**Layer 2 — Registration services (STABLE after v1.0)**

```csharp
// In each microservice's Program.cs
services.AddHexalithDomain<TDomain>();

// In the Aspire AppHost
builder.AddFrontComposerShell()
       .WithDomain<TDomainA>()
       .WithDomain<TDomainB>();

// Optional chat surface
services.AddFrontComposerMcp();  // adds the in-process MCP server + skill corpus
```

Three lines per microservice + four to six lines of shell-level setup. Ten lines total for a single-microservice project; twenty for a multi-microservice shell project.

**Layer 3 — Customization gradient contracts (STABLE after v1.0)**

Four levels, each bound to typed contracts:

```csharp
// Annotation level (Layer 1)
[ProjectionRole(Role.ActionQueue)]
public record ShipmentProjection { ... }

// Template level — typed Razor template
<FrontComposerViewTemplate For="ShipmentProjection">
    ...custom layout...
</FrontComposerViewTemplate>

// Slot level — typed generic field override (Amelia's improved ergonomics)
[ProjectionFieldSlot<ShipmentProjection>(x => x.EstimatedDispatchAt)]
public partial class RelativeTimeSlot : IFieldSlot<DateTimeOffset>
{
    // FieldSlotContext<DateTimeOffset> is injected via source generator
    // IntelliSense exposes: Value, Validation, Metadata, Formatters
}

// Full replacement — complete view override preserving lifecycle wrapper + accessibility contract
[ProjectionView(For = typeof(ShipmentProjection))]
public partial class ShipmentDashboard : ComponentBase
{
    // Framework injects lifecycle wrapper + accessibility contract automatically
}
```

Compared to the earlier draft: **generic type parameter + lambda expression replaces the `nameof` string** to preserve refactor safety. `FieldSlotContext<T>` exposes `Value`, `Validation`, `Metadata`, and `Formatters` as discoverable IntelliSense properties so consumers never need to consult docs for basic slot usage.

**Layer 4 — Runtime services (`[Experimental]` through v1.1, STABLE at v1.2)**

`ICommandService`, `IQueryService`, `ISignalRSubscriptionService`, `ILifecycleWrapper`, `IRenderingContract`, `ISkillCorpus`, `IMcpToolManifest`.

These are the interfaces most likely to learn from real adoption. SignalR fault semantics, tenant scoping edge cases, and command-envelope shape changes are all expected in the first 6 months. Rather than commit to stability prematurely, v1 marks Layer 4 with `[Experimental]` (the Roslyn `RequiresPreviewFeaturesAttribute` or equivalent with an `RS`-prefix diagnostic), warns consumers who reference runtime services directly, and commits to stability at v1.2 after one field tour.

**Adopters who reference Layer 4 directly during v1.0–v1.1** acknowledge the experimental nature and accept that signature changes may occur in minor versions. Most adopters never touch Layer 4; they operate at Layers 1–3.

**Layer 5 — Source generator outputs (internal, not a stable contract)**

Typed partial types and generator-emitted metadata. Adopters must not reference generator output shape. Framework contributors operate here. `.g.cs` output paths are deterministic and documented so developers debugging generator output can find the emitted code.

### Reference Microservices (3, not 5)

Per Party Mode (Amelia): **three deep microservices that form a learning arc** beat five shallow one-role-each demos. The cut from 5 to 3 also reflects Barry's runway-preservation critique — 5 reference microservices with standalone repos + samples branches + tutorial pages is 5 products masquerading as samples, each with its own backlog, issues, and docs.

| Reference | Domain | What It Teaches |
|---|---|---|
| **`CounterDomain`** | Increment / reset / snapshot a counter | Minimum viable usage. 10-minute read. Every Layer 1 attribute used exactly once, nothing more. This is Journey 1's first-render exemplar. |
| **`OrdersDomain`** | Place / approve / reject / ship orders with validation, status badges, inline actions | The full gradient workout. Commands with FluentValidation, `ActionQueue` projection, `DetailRecord` expand-in-place, slot-level `[ProjectionFieldSlot]` override for the order timestamp column, `[RequiresPolicy]` authorization on the approve command. This is Journey 2 and Journey 3's exemplar. |
| **`OperationsDashboardDomain`** | Multi-domain composition (cross-references `OrdersDomain` + a minimal `InventoryDomain` snippet) | How bounded contexts compose in the shell. `Dashboard` role hint. Multi-domain navigation and session persistence. This is Journey 5 and Journey 6's exemplar for agent interaction across multiple bounded contexts. |

The two archetypes that were cut (a full `InventoryDomain` and `CustomersDomain`) are demonstrated in the Orders microservice as feature diffs rather than standalone projects. If community demand justifies them later, they become v2 additions.

All three reference microservices live in a **single monorepo** alongside the framework code, in a `samples/` directory — not separate repositories (per Barry's "monorepo. period." prescription for solo-maintainer sanity).

### Documentation Strategy

**DocFX. Not Blazor-native SSG.** Party Mode (Paige) decision: dogfooding Blazor for a Blazor framework is a side quest that will eat three weeks and deliver worse search. DocFX is boring, battle-tested, generates API reference from XML comments for free, integrates with the .NET toolchain, and is the right choice for a solo-maintained v1. The Blazor-dogfood docs story is a v2 aspiration if adopter feedback justifies it.

**Single source, two renderings, explicit narrative vs reference section markers.**

The skill corpus (consumed by LLM agents via MCP) and the human-facing documentation site are rendered from the same Markdown source files, but with section markers in the front-matter. The MCP renderer strips narrative sections and exposes only reference material. The DocFX-generated site keeps both. This prevents **voice collapse** — the failure mode Paige identified where writing for two audiences produces docs that are complete but unreadable for humans because the LLM (stricter reader) wins the implicit tie-break.

```markdown
---
narrative: true   # included on human docs site, stripped from MCP skill corpus
---

## Narrative: Why does `[ProjectionRole]` exist?

Early drafts of FrontComposer tried to infer view type from projection shape alone.
That failed for projections that could be rendered as either a queue OR a dashboard...

<!-- reference section follows -->

## Reference

`[ProjectionRole(Role role)]`

Applies a rendering role hint...
```

**Four documentation genres (Diátaxis), not three.**

Per Paige: a framework that ships with only reference + how-to + tutorials produces developers who can *use* the thing but can't *reason* about it. The missing genre is **explanation / concepts**:

| Genre | Purpose | Audience |
|---|---|---|
| **Tutorials** | Learn-by-doing, guided first experience | First-time users (Journey 1 Marco) |
| **How-to** | Task-oriented recipes | Developers solving specific problems (Journey 2 Marco) |
| **Reference** | Technical specification | Developers looking up API details (humans + LLM agents) |
| **Explanation / Concepts** | Why does this exist, what problem does it solve, how should you reason about it | Developers hitting edge cases and needing to understand the design |

The concepts layer is a v1 ship commitment, not a v1.x addition. Without it, adopters file bug reports instead of solving edge cases themselves.

**Teaching errors enforced at compile time, not by discipline.**

Per Paige: a solo maintainer cannot sustain "every error message must be teaching" as a discipline. It degrades to 40% compliance by month six. The fix is to make the error message template **part of the attribute definition**, enforced at compile time by a source generator test. You cannot ship a new attribute without filling in `Expected`, `Got`, `Fix`, and `DocsLink` fields. The build won't let you. This moves the burden from discipline to build-enforcement — the only version of this commitment that survives contact with a tired maintainer at 11pm.

**Migration guide trigger: skill-corpus-compile-break, not major-version-bump.**

Per Paige: breaking changes happen in minors, not just majors. The right trigger for a migration guide is "any change that would make a shipped skill corpus example fail to compile." Regardless of semver bucket, when that trigger fires, the PR must include:
1. Docs page describing the change and its reason
2. Old → new code example
3. Roslyn analyzer flagging old usage with a fix-it, shipped at least one minor version before the bump
4. Updated skill corpus in the same PR
5. Nightly LLM benchmark validating that generation correctness holds on the upgraded version for at least one week before release

**Day-1 highest-leverage doc: customization gradient cookbook.**

Per Paige: the customization gradient cookbook is the day-1 highest-leverage doc — the single page showing the same problem (relative-time rendering for a `DateTimeOffset` field) solved at each gradient level. *Revised in §Project Scoping per Party Mode round 3 (Barry + John):* the prose cookbook moves to **week 6** as an acceptance test *after* the gradient API exists, not before framework code. The week-4 design-time check is instead: *"can Jerome sketch the four gradient level signatures as C# interfaces in under 30 minutes without hand-waving?"* — writability of code, not prose.

### Developer-Visible Technical Constraints

These are the constraints Marco and his team see directly during dev-loop. **The broader CI gate matrix** (unit/component/E2E test coverage percentages, visual regression, axe-core, LLM benchmark cadence, deployment topology validation, SBOM, NuGet signing, Pact contract tests, mutation testing, performance regression gates) **is deliberately relocated to Non-Functional Requirements (Step 10)** to avoid conflating "what the consumer sees" with "how the framework is quality-gated internally." This relocation is part of the solo-maintainer sustainability filter discipline — Step 7 is for developer-tool surface, not CI plumbing.

**Hot reload** is a first-class commitment. Hot reload must work for: domain attribute changes, customization gradient overrides, Razor component edits. Hot reload breakage is a critical bug on par with a runtime crash.

**IDE matrix** — Visual Studio 2026 is the reference IDE. JetBrains Rider 2026.1+ must have parity (IntelliSense, hover docs, go-to-definition, generator debugging). VS Code with C# Dev Kit must work for lightweight-tooling adopters. IDE differences in source generator output are a known pain point; the framework ships a `rider-specific` test fixture validating Rider's handling of generator outputs.

**Source generator performance budget** — 500ms incremental per domain assembly, **4s** full solution rebuild for a 50-aggregate reference domain (per Winston's revision; the original 2s target was optimistic). CI gates on the incremental number, not the full rebuild — incremental is the developer-productivity metric that matters, full rebuild is a vanity metric. Implementation uses `IIncrementalGenerator` with `ForAttributeWithMetadataName` for cache efficiency.

**Trim compatibility** — all framework assemblies must be trim-compatible with Blazor WebAssembly AOT. `IsTrimmable="true"` in all project files; trim warnings block CI. Three known trim-hostile dependencies (FluentValidation, DAPR SDK, Fluent UI v5) require front-loaded evaluation in week 2 with pass/fail criteria. Full evaluation protocol and budget in §Non-Functional Requirements → Build, CI & Release → Trim Compatibility.

**`.g.cs` output path guarantee + `dotnet hexalith dump-generated <Type>` CLI** — per Amelia's generator-black-box concern. Developers debugging generator output must have a deterministic file path to find the emitted code AND a one-command CLI to dump the three outputs (Razor partial + MCP manifest + test specimen metadata) to disk for inspection. Without this, debugging the generator is debugging Hexalith itself, which is an unacceptable dev experience.

**Diagnostic ID scheme** — per Winston. Every framework diagnostic reserves an ID range per package (HFC0001–HFC5999). Each diagnostic maps to a docs page. Full range table in §Non-Functional Requirements → Maintainability.

**Structured logging contract** — per Winston. OpenTelemetry semantic conventions for end-to-end tracing. Full specification in §Non-Functional Requirements → Maintainability.

**Deprecation policy** — per Winston. One minor version minimum deprecation window. `[Obsolete]` messages link to diagnostic ID and migration path. Full convention in §Non-Functional Requirements → Maintainability.

### Tone & Language

Framework-generated UI text and developer-facing messages follow four attributes from the product brief:

| Attribute | Means | Example |
|---|---|---|
| **Technical & precise** | Use correct domain terminology (commands, projections, aggregates). No hand-waving. | Button: "Send Command" not "Submit" |
| **Concise & direct** | Short labels, clear messages, no filler. | Loading: "Loading projections..." not "Please wait..." |
| **Confident & authoritative** | Opinionated framework, opinionated voice. Clear guidance, not hedging. | Error: "Command `CreateOrder` failed: aggregate not found" not "Something went wrong" |
| **Helpful without patronizing** | Explain what happened and how to fix it. Don't dumb down, don't assume memorized docs. | Empty state: "No projections registered. Add a projection to see data here." not "Nothing here" |

**Rules:** Use domain language consistently. Be specific in error messages — name the entity/command that failed. Provide actionable guidance in empty states and errors. Never use vague or generic messages.

### Implementation Considerations (Dev-Loop Only)

The following are dev-loop concerns developers touch daily. Broader implementation concerns (SBOM, package signing, release automation, CI pipeline specifics) are captured in Step 10 NFRs.

**Build-time error quality** — every build-time error from the framework must include: (a) what the generator or analyzer saw, (b) what it expected, (c) how to fix it, (d) a diagnostic ID linking to the docs page. Enforced at compile time via the error template commitment above.

**Conventional commits enforcement** — a commit-msg hook (ships with project template) and CI lint step. Semantic release depends on clean commit messages. The hook is installed by the `dotnet new hexalith-frontcomposer` template so adopters inherit discipline without effort.

**Issue triage templates** — bug report, feature request, adopter question templates in `.github/ISSUE_TEMPLATE/`. Contribution guide explicit about what kinds of PRs are welcome (fixes + docs yes, new framework features require design discussion).

**Single-maintainer sustainability policy** documented in CONTRIBUTING.md: response-time expectations (not commitments), bus-factor acknowledgment, fork-friendliness. Adopters in regulated industries need to know the risk they are taking on when building production systems on a solo-maintained OSS framework.

### Solo-Maintainer Sustainability Verdict

The original Step 7 draft failed Party Mode round 2's sustainability filter. This revision collapses the package count (11 → 8), moves CI gates to Step 10, cuts reference microservices (5 → 3), defers Layer 4 stability to v1.2, and explicitly adopts monorepo structure. **Barry's meta-critique stands: the PRD must not become the work.** Subsequent PRD steps must honor the solo-maintainer sustainability filter, which is why it sits at the top of this section as a PRD-wide discipline rather than as a passing comment.
