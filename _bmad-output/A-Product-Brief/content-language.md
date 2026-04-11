# Content & Language Strategy: Hexalith FrontComposer

---
stepsCompleted: []
---

## Context

- **Product:** Hexalith FrontComposer
- **Positioning:** Event-sourced frontend composition framework for .NET / Blazor
- **Primary audience:** .NET developers (DDD/CQRS/ES expertise)
- **Secondary audience:** Business users (end users of composed applications)
- **Existing brand guidelines:** None -- built fresh

## Brand Personality

**Persona:** The Seasoned Architect -- a trusted senior colleague who's already thought through the hard problems and gives you a clear path forward.

| Attribute | What it means for FrontComposer | How it's expressed |
|---|---|---|
| **Opinionated** | Makes clear architectural choices so developers don't have to | Conventions over configuration, strong defaults, clear "this is how we do it" |
| **Expert** | Deep knowledge of DDD/CQRS/ES, earned through experience | Precise domain terminology, no hand-holding, assumes competence |
| **Reliable** | Predictable, consistent, does what it says | Stable APIs, comprehensive tests, no surprises |
| **Pragmatic** | Solves real problems, not theoretical ones | Minimal boilerplate, practical defaults, escape hatches when needed |
| **Understated** | Lets the work speak -- no hype, no marketing fluff | Clean documentation, factual README, results over promises |

## Tone of Voice

### Spectrum Positions

| Spectrum | Position (1-5) | Description |
|---|---|---|
| **Formality** (Formal ← → Casual) | **2** | Professional but not stiff |
| **Mood** (Serious ← → Playful) | **2** | Serious framework, not a toy |
| **Complexity** (Technical ← → Simple) | **1** | Audience expects technical precision |
| **Energy** (Reserved ← → Enthusiastic) | **2** | Confident and understated, not hype |

### We Say / We Don't Say

| Context | We Say | We Don't Say |
|---|---|---|
| Documentation intro | "FrontComposer composes Blazor UIs from Hexalith.EventStore domain models." | "Welcome to FrontComposer! We're excited to help you build amazing UIs!" |
| Getting started | "Define your commands and projections. FrontComposer handles the rest." | "It's super easy! Just follow these simple steps to get going!" |
| Error in docs | "The aggregate was not found. Verify the aggregate ID exists in the event store." | "Oops! Something went wrong. Please try again." |
| Breaking change | "v2.0 removes `RegisterView()`. Use `[ComposedView]` attribute instead." | "We've made some exciting improvements to how views are registered!" |
| Feature description | "Auto-generates command forms from domain command definitions." | "Magically creates beautiful forms without writing a single line of code!" |
| README badge | "Production-ready" | "Built with love" |

## Language Strategy

| Layer | Language(s) | Priority | Approach |
|---|---|---|---|
| **Framework code & API** | English | Primary | English only -- source code, comments, API surface |
| **Documentation / README / GitHub** | English | Primary | English only |
| **Composed UI (built-in)** | English, French | Primary | Ship with EN + FR resource files |
| **Composed UI (extensible)** | Any | Secondary | Standard Blazor i18n mechanism -- teams add their own languages |

**Localization Approach:**
- Use Blazor's recommended localization pattern (IStringLocalizer / IStringLocalizerFactory)
- FrontComposer ships with English and French resource files for all framework-generated UI (labels, messages, navigation)
- Microservice teams provide their own resource files for domain-specific content
- Standard .NET date/number/currency formatting via CultureInfo

**Tone Consistency:** Same technical, precise tone across all languages -- no cultural adaptation needed since the audience is developers/architects regardless of language.

## SEO & Discoverability Strategy

### Keywords by Intent

| Category | Intent | Keywords |
|---|---|---|
| **Problem** | Developer has a pain | "blazor microservices frontend", "blazor micro frontend", "compose blazor components from microservices", "blazor event sourcing ui" |
| **Architecture** | Looking for patterns | "blazor cqrs frontend", "blazor ddd ui", "event sourced frontend .net", "blazor domain driven design" |
| **Solution** | Looking for a framework | "blazor application framework", "blazor composition framework", "blazor microservice ui composition" |
| **Comparison** | Evaluating alternatives | "blazor vs oqtane", "abp alternative blazor", "blazor fluent ui framework" |
| **Ecosystem** | Already in Hexalith | "hexalith frontend", "hexalith blazor", "hexalith eventstore ui", "hexalith frontcomposer" |
| **Technology** | Stack-specific | "blazor fluent ui microservices", "dapr blazor frontend", "blazor server webassembly framework" |

### Page-Keyword Map

| Page | Primary Keyword | Secondary Keywords |
|---|---|---|
| Landing / README | "blazor microservices frontend framework" | "event sourced frontend", "blazor composition" |
| Getting Started | "blazor event sourcing getting started" | "hexalith frontcomposer setup" |
| Architecture docs | "blazor cqrs frontend architecture" | "blazor ddd ui", "domain model ui generation" |
| Comparison page | "blazor framework comparison" | "oqtane vs abp alternative" |
| API reference | "hexalith frontcomposer api" | "blazor command form generation" |

### URL Structure
- Docs: `docs.hexalith.com/frontcomposer/{slug}` or GitHub Pages
- Slugs: lowercase, hyphens, no special characters

### Structured Data
- Landing page: `SoftwareApplication` schema
- No local SEO required

## Content Structure Principles

### Developer-Facing Content

**Critical (immediate visibility):**
1. Quick-start guide -- productive in minutes
2. Architecture overview -- understand the two-layer composition model
3. Live demo -- see it working before committing

**Secondary:**
- API reference
- Configuration guide
- Custom component override docs
- Migration / versioning guide

### Composed Application Structure (auto-generated)

- **Navigation:** Auto-discovered from microservices, organized into functional areas (Orders, Inventory, Customers...)
- **Dashboard/Home:** Overview across all composed modules
- **Admin/Settings:** Standard admin area provided by FrontComposer

### Explicit Constraints (out of scope)

- No CMS
- No blog / marketing pages
- Authentication: Keycloak by default, compatible with Entra ID, GitHub, and Google auth -- no built-in auth UI

### Clarity Level
Strong vision -- clear picture of both the developer and business user experience.

## Content Type Guidelines

**Documentation / API Reference:**
- Lead with what it does, then how to use it
- Include code examples for every public API
- Use domain terminology precisely (command, projection, aggregate, event)
- Keep sentences short -- one concept per sentence

**UI Microcopy (generated by FrontComposer):**
- Use active voice
- Be specific about actions ("Send Command" not "Submit")
- Include context in errors (entity name, command that failed)
- Provide actionable guidance in empty states

**README / Getting Started:**
- Lead with the value proposition in one sentence
- Show a working example within the first scroll
- Assume .NET / Blazor familiarity, explain FrontComposer-specific concepts

## Content Ownership

| Content Type | Owner | Frequency |
|---|---|---|
| Documentation / API reference | Jerome | With each release |
| README / Getting started | Jerome | As framework evolves |
| Translations (FR) | Jerome | As needed |
| Community contributions | Contributors | Occasional |

## Writing Checklist

- [ ] Tone matches guidelines (technical, precise, concise, confident)
- [ ] Domain terminology used correctly and consistently
- [ ] No hype, filler words, or exclamation marks
- [ ] Code examples compile and run
- [ ] Keywords included naturally (not stuffed)
- [ ] Both EN and FR resource files updated (if UI content)
- [ ] Accessible language (technical but not obscure)

---

**Status:** Content & Language Strategy Complete
**Next Phase:** Visual Direction
**Last Updated:** 2026-04-06
