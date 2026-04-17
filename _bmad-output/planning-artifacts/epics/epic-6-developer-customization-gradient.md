# Epic 6: Developer Customization Gradient

Developer can customize generated UI at four levels -- annotation overrides, typed Razor templates, slot-level field replacement, and full component replacement -- with hot reload, build-time contract validation, error boundaries, and actionable error messages with diagnostic IDs. **Depends on Epics 1-4** (customization targets the generated views, command forms, DataGrid features, and shell components built in those epics).

### Story 6.1: Level 1 - Annotation Overrides

As a developer,
I want to override field rendering via declarative attributes without writing custom components,
So that I can adjust labels, formatting, column priority, and display hints with a one-line attribute change and immediate hot reload feedback.

**Acceptance Criteria:**

**Given** a projection property with [Display(Name = "Order Date")]
**When** the generated DataGrid renders
**Then** the column header uses "Order Date" instead of the humanized property name
**And** [Display(Name)] takes precedence over ALL auto-formatting rules including enum humanization

**Given** a projection property with [ColumnPriority(1)]
**When** FcColumnPrioritizer activates for projections with >15 fields
**Then** fields with lower priority numbers appear first in the visible column set

**Given** a DateTime property with [RelativeTime]
**When** the DataGrid cell renders
**Then** the value shows relative time (e.g., "3 hours ago") using fixed-width abbreviations
**And** switches to absolute after 7 days
**And** the default (without annotation) remains absolute date format

**Given** a decimal property with [Currency]
**When** the DataGrid cell renders
**Then** the value is locale-formatted with currency symbol, right-aligned

**Given** an annotation override is applied
**When** the developer saves the file with hot reload active
**Then** the change reflects in the running application without restart
**And** customization time is <= 5 minutes from reading docs to seeing the override (NFR84)

**Given** the annotation-level customization
**When** it is evaluated as a customization gradient level
**Then** it is compile-time only (attributes processed by the source generator)
**And** no runtime registration or custom component code is required

**Given** the sample domain (Counter or Task Tracker)
**When** Level 1 annotation overrides are demonstrated
**Then** at least one override is applied to the sample domain as a reference implementation (e.g., [Display(Name="...")] on a projection property)

**References:** FR39, UX-DR54 (Level 1), NFR84

---

### Story 6.2: Level 2 - Typed Razor Template Overrides

As a developer,
I want to override component rendering via typed Razor templates bound to domain model contracts,
So that I can rearrange section layouts and field groupings without replacing the entire component.

**Acceptance Criteria:**

**Given** a developer creates a Razor template for a projection view
**When** the template is bound to the domain model
**Then** a typed Context parameter provides access to all projection fields and metadata
**And** the template controls section-level layout (field grouping, ordering, visual hierarchy)
**And** individual field rendering still uses the framework's auto-generation within the template sections

**Given** a Level 2 template override
**When** it is registered
**Then** it is a compile-time artifact (processed by the source generator)
**And** the framework detects the template and uses it instead of the default layout

**Given** a Level 2 template
**When** the framework version changes
**Then** build-time contract validation checks the template's expected contract against the installed framework version (FR43)
**And** a warning is emitted if the contract doesn't match: "Template expects FrontComposer v{expected}, installed v{actual}. See HFC{id}."

**Given** FcStarterTemplateGenerator (Story 6.5)
**When** the developer requests a Level 2 starter template from the dev-mode overlay
**Then** the generated Razor source includes: typed Context parameter, exact Fluent UI components/parameters used by the default layout, and comments indicating the contract type
**And** the developer can paste and modify the template as a starting point

**Given** a Level 2 template override with hot reload active
**When** the developer modifies the template
**Then** the change reflects without application restart (FR44)

**Given** the sample domain
**When** Level 2 template overrides are demonstrated
**Then** at least one template override is applied to the sample domain rearranging section layout as a reference implementation

**References:** FR40, FR43 (partial), FR44 (partial), UX-DR54 (Level 2)

---

### Story 6.3: Level 3 - Slot-Level Field Replacement

As a developer,
I want to replace a single field's renderer with a custom component while all other fields remain auto-generated,
So that I can customize one problematic field without rewriting the entire view.

**Acceptance Criteria:**

**Given** a developer wants to override a specific field
**When** they register a slot override
**Then** IOverrideRegistry.AddSlotOverride is called with a refactor-safe lambda expression identifying the field (e.g., `registry.AddSlotOverride<OrderProjection>(o => o.Priority, typeof(CustomPriorityRenderer))`)
**And** the lambda expression ensures rename-safe field identification (compile error on field rename)

**Given** a slot override is registered for a field
**When** the generated view renders
**Then** the custom component renders for the overridden field
**And** all other fields render via the framework's auto-generation
**And** the custom component receives a typed FieldSlotContext<T> with: field value, field metadata, parent entity reference, and render context (density, theme, read-only state)

**Given** a slot-level override registration
**When** it executes
**Then** it is a runtime registration (not compile-time)
**And** DI never registers per-type renderers; single ProjectionRenderer<T> resolves via IOverrideRegistry for customs

**Given** a Level 3 slot override with hot reload active
**When** the developer modifies the custom component
**Then** the change reflects without application restart (FR44)

**Given** FcStarterTemplateGenerator
**When** the developer requests a Level 3 starter template
**Then** the generated source includes: typed FieldSlotContext<T> parameter, the current field's Fluent UI component and parameters, and the exact contract type

**Given** the sample domain
**When** Level 3 slot overrides are demonstrated
**Then** at least one slot override replaces a field renderer in the sample domain as a reference implementation

**References:** FR41, FR44 (partial), UX-DR54 (Level 3)

---

### Story 6.4: Level 4 - Full Component Replacement

As a developer,
I want to replace a generated component entirely with a custom implementation while preserving the framework's lifecycle wrapper, accessibility contract, and shell integration,
So that I have complete control over rendering for complex views without losing framework benefits.

**Acceptance Criteria:**

**Given** a developer wants to fully replace a generated view
**When** they register a view override
**Then** IOverrideRegistry.AddViewOverride is called with the projection type and custom component type
**And** the registration is runtime (not compile-time)

**Given** a full replacement component
**When** it renders
**Then** the framework's lifecycle wrapper (FcLifecycleWrapper) still wraps the component
**And** shell integration (navigation, breadcrumbs, density, theme) is preserved
**And** the custom component receives the full domain model context, render context, and lifecycle state

**Given** a full replacement component
**When** the custom component accessibility contract is evaluated
**Then** the 6 requirements are enforced:
**And** (1) expose accessible name via aria-label or visible text (build-time warning if missing)
**And** (2) preserve keyboard reachability in DOM order
**And** (3) preserve focus visibility (no overriding --colorStrokeFocus2)
**And** (4) announce state changes using same aria-live politeness categories
**And** (5) respect prefers-reduced-motion
**And** (6) support forced-colors mode with system color keywords

**Given** a Level 4 override with hot reload active
**When** the developer modifies the custom component
**Then** the change reflects without application restart (FR44)

**Given** FcStarterTemplateGenerator
**When** the developer requests a Level 4 starter template
**Then** the generated source includes: complete view structure with lifecycle wrapper integration, accessibility contract hooks, typed parameters, and comments for all Fluent UI components used in the default view

**Given** the customization gradient hierarchy
**When** all four levels are inspected
**Then** each level inherits capabilities from the level above (Level 2 includes Level 1 attributes; Level 3 includes Levels 1-2; Level 4 includes Levels 1-3)

**Given** the sample domain
**When** Level 4 full replacement is demonstrated
**Then** at least one full view replacement is applied to the sample domain as a reference implementation preserving lifecycle wrapper and accessibility contract

**References:** FR42, FR44 (partial), UX-DR31, UX-DR54 (Level 4)

---

### Story 6.5: FcDevModeOverlay & Starter Template Generator

As a developer,
I want an interactive diagnostic overlay that shows me what conventions are applied to each generated element and generates starter code for customization,
So that I can discover how to customize any part of the UI without reading documentation first.

**Acceptance Criteria:**

**Given** the application is running in debug mode (#if DEBUG)
**When** the developer presses Ctrl+Shift+D or clicks the dev-mode header icon
**Then** the FcDevModeOverlay activates showing dotted outlines around each auto-generated element
**And** info badges display the convention name on each element

**Given** the dev-mode overlay is active
**When** the developer clicks on an annotated element
**Then** a 360px FluentDrawer detail panel opens showing:
**And** convention name and description
**And** contract type (full type name)
**And** current customization level (default or overridden)
**And** recommended override level for common customization goals
**And** "Copy starter template" button (for Levels 2-4)
**And** before/after toggle for active overrides

**Given** the developer clicks "Copy starter template" for a Level 2 override
**When** FcStarterTemplateGenerator processes the request
**Then** it walks the auto-generation engine's in-memory component tree via IRazorEmitter service
**And** emits Razor source with: typed Context parameter, exact Fluent UI components, typed parameters, and contract type comments
**And** the source is copied to clipboard via JS interop

**Given** unsupported fields in the current view
**When** the dev-mode overlay is active
**Then** unsupported fields are highlighted with a red-dashed border
**And** the detail panel shows the exact unsupported type name and recommended override level

**Given** the dev-mode overlay
**When** keyboard navigation is used
**Then** annotations are keyboard-navigable (tab order)
**And** the detail panel has role="complementary"
**And** Escape closes the panel
**And** screen reader announces the convention name on focus

**Given** production builds
**When** the application compiles without DEBUG
**Then** FcDevModeOverlay is completely excluded (zero production footprint)
**And** FcStarterTemplateGenerator is registered only in development mode

**References:** FR39-42 (discovery path), UX-DR9, UX-DR11, UX-DR54

---

### Story 6.6: Build-Time Validation, Error Boundaries & Diagnostics

As a developer,
I want the framework to catch customization errors at build time, isolate rendering failures at runtime, and give me actionable error messages with documentation links,
So that a broken override never crashes the shell and I can fix problems without asking for help.

**Acceptance Criteria:**

**Given** a customization override (any level)
**When** the framework version changes between minor versions
**Then** build-time validation checks the override's expected contract against the installed framework version (FR43)
**And** a warning is emitted: "Override expects FrontComposer v{expected}, installed v{actual}. See HFC{id}."

**Given** a custom override component with accessibility issues
**When** the build runs
**Then** Roslyn analyzers check the custom component against the 6-requirement accessibility contract (UX-DR31)
**And** missing accessible names produce build-time warnings with WCAG citation + user scenario
**And** with TreatWarningsAsErrors=true for accessibility warnings, builds with inaccessible overrides are blocked

**Given** the build-time accent contrast check
**When** a custom accent color is configured
**Then** a Roslyn analyzer computes contrast ratio against both Light and Dark neutral backgrounds
**And** if either ratio fails WCAG AA (4.5:1 normal text, 3:1 large text/UI components), a build warning is emitted
**And** with TreatWarningsAsErrors=true, inaccessible accent colors block the build

**Given** a customization override throws a rendering exception at runtime
**When** the error boundary activates
**Then** the failure is isolated to the affected component only (FR47)
**And** a diagnostic panel renders in place of the faulty component with: error description, diagnostic ID (HFC2001), and a link to the documentation page
**And** the rest of the composition shell continues to function normally
**And** the error is logged with full context (component type, override level, exception details)

**Given** any customization or generation error
**When** the error message is produced
**Then** it includes: what was expected, what was found, how to fix it, and a diagnostic ID linking to a documentation page (FR45)
**And** the diagnostic ID follows the reserved range for the package (HFC0001-0999 Contracts, HFC1000-1999 SourceTools, HFC2000-2999 Shell)

**Given** hot reload is active for all four customization levels
**When** any override is modified and saved
**Then** the change reflects in the running application without restart (FR44)
**And** hot reload limitations (generic type changes, new attribute additions) trigger a build-time message: "Full restart required for this change type"

**References:** FR43, FR44, FR45, FR47, UX-DR31, UX-DR64, NFR36, NFR80, NFR86

---

**Epic 6 Summary:**
- 6 stories covering all 8 FRs (FR39-45, FR47)
- Relevant NFRs woven into acceptance criteria (NFR36, NFR80, NFR84, NFR86)
- Relevant UX-DRs addressed (UX-DR9, UX-DR11, UX-DR31, UX-DR54, UX-DR64)
- Stories are sequentially completable: 6.1 (annotation) -> 6.2 (template) -> 6.3 (slot) -> 6.4 (full replacement) -> 6.5 (dev-mode overlay) -> 6.6 (validation/errors)
- Each level inherits capabilities from previous levels

---
