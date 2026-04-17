# Design System Foundation

### Design System Choice

**Fluent UI Blazor v5** -- Microsoft's open-source component library for Blazor, built on Fluent UI Web Components v3 (the same rendering layer powering Microsoft 365, Teams, and Windows 11).

This is not a choice among alternatives; it is a fixed architectural constraint. The tech stack (Blazor), the design alignment (Microsoft ecosystem), the accessibility requirement (WCAG 2.1 AA), the LLM compatibility priority (MCP Server), and the solo-developer maintenance reality all converge on Fluent UI Blazor v5 as the only viable option.

### Rationale for Selection

1. **Architectural alignment** -- Fluent UI Blazor v5 is the only component library that provides native Blazor components (not JavaScript wrappers), design token theming via CSS custom properties, and first-class MCP Server integration for AI-assisted development.

2. **Emotional design support** -- Fluent UI's visual language is already familiar to business users from Microsoft 365. This directly serves the "Familiarity as foundation" emotional design principle. No custom styling or brand adaptation needed for the base experience.

3. **Maintenance reality** -- Solo project. A custom design system is unmaintainable. Fluent UI provides 65+ production-ready components with accessibility built in, maintained by Microsoft contributors. The framework inherits accessibility compliance rather than building it.

4. **Convention enforcement** -- Using Fluent UI exclusively (no custom styling, no overrides of the design system) enforces the consistency-is-trust principle. Every auto-generated view from every microservice looks identical because they all use the same unmodified components.

5. **LLM compatibility** -- The Fluent UI Blazor MCP Server provides component intelligence directly in the IDE. AI coding assistants generate correct, idiomatic v5 code. This supports the "architecture optimized for LLM code generation" success criterion.

### Implementation Approach

**Zero-override strategy:** FrontComposer uses Fluent UI Blazor v5 exactly as designed. No custom CSS overrides, no shadow DOM penetration, no design token hacking. The only customization is the accent color (Teal #0097A7) applied through Fluent UI's supported theming mechanism.

**Component mapping:**

| FrontComposer Concept | Fluent UI Component | Notes |
|---|---|---|
| Application shell | `FluentLayout` + `FluentLayoutItem` | Declarative area-based layout (Header, Navigation, Content, Footer) |
| Sidebar navigation | `FluentNav` (v5 renamed from FluentNavMenu) | Collapsible groups per bounded context |
| Navigation toggle | `FluentLayoutHamburger` | Built-in responsive hamburger with smooth animation |
| Command forms | `FluentTextField`, `FluentSelect`, `FluentCheckbox`, `FluentDatePicker`, etc. | Auto-generated from command field types |
| Form validation | `FluentValidationMessage` + EditContext | Blazor-native validation with FluentValidation library |
| Projection lists | `FluentDataGrid` (HTML `<table>` in v5) | Native HTML rendering, improved accessibility and testability |
| Detail views | `FluentCard`, `FluentAccordion` | Cards for grouped fields, accordion for progressive disclosure |
| Action buttons | `FluentButton` | Appearance variants: Primary (main action), Secondary (alternative), Outline (subtle) |
| Status indicators | `FluentBadge` | Colored badges for projection status on list rows |
| Loading states | `FluentSkeleton`, `FluentProgressRing` | Per-component skeletons, not full-page spinners |
| Empty states | Custom Blazor component with `FluentIcon` | Domain-specific message + creation CTA |
| Lifecycle feedback | `FluentProgressRing` (submitting), `FluentBadge` (syncing), `FluentMessageBar` (confirmed) | Note: `IToastService` removed in v5; use `FluentMessageBar` or custom notification component |
| Theme toggle | CSS custom properties via `<fluent-design-theme>` | Dark/Light/System with LocalStorage persistence |
| Command palette | Custom Blazor component with `FluentSearch` | No built-in Fluent UI command palette; must be custom-built following GitHub's pattern |
| Icons | `FluentIcon` from `Microsoft.FluentUI.AspNetCore.Components.Icons` | 2,200+ icons, strongly-typed, Filled and Outlined variants |
| Providers | `<FluentProviders />` | Single provider component replaces v4's individual providers |

**Service registration:**

```csharp
builder.Services.AddFluentUIComponents(config =>
{
    config.DefaultValues.For<FluentButton>()
          .Set(p => p.Appearance, ButtonAppearance.Primary);
});
```

**Key v5 migration considerations for implementation:**
- `FluentNavMenu` → `FluentNav` (renamed)
- `IToastService` → removed (use `FluentMessageBar` or custom)
- `SelectedOptions` → `SelectedItems` (binding change)
- `FluentDesignTheme` → CSS custom properties (theming change)
- `<FluentDesignSystemProvider>` → `<FluentProviders />` (simplified)

### Customization Strategy

**What FrontComposer customizes (supported by Fluent UI):**

- **Accent color:** Teal #0097A7 applied through CSS custom properties. This is the sole brand differentiation from default Fluent UI.
- **Default component values:** Using the v5 DefaultValues system to set application-wide defaults (e.g., all buttons default to Primary appearance).
- **Localization:** IFluentLocalizer for English + French resource files on all framework-generated UI strings.
- **DataGrid adapters:** EF Core or OData adapter for server-side query resolution.

**What FrontComposer does NOT customize:**

- No custom CSS overrides on Fluent UI components
- No custom design tokens beyond accent color
- No custom typography (Segoe UI / system font stack)
- No custom icons (Fluent UI icon library only)
- No custom spacing or elevation values
- No shadow DOM penetration

**Rationale for zero-override:** Every custom override is a maintenance burden, a potential accessibility regression, and a consistency risk. By using Fluent UI as-is, FrontComposer inherits all future Fluent UI improvements (bug fixes, accessibility enhancements, new components) without migration friction. The only custom components are those Fluent UI doesn't provide: command palette, eventual consistency lifecycle wrapper, and auto-generation boundary placeholders.

**Custom components needed (not in Fluent UI):**

| Component | Purpose | Built With |
|---|---|---|
| `FrontComposerCommandPalette` | Universal navigation and command search (Ctrl+K) | `FluentSearch` + custom overlay |
| `FrontComposerLifecycleWrapper` | Five-state command lifecycle composition wrapper | Custom Blazor component wrapping any inner component |
| `FrontComposerFieldPlaceholder` | Auto-generation boundary protocol -- unsupported field indicator | `FluentCard` + `FluentIcon` + `FluentAnchor` |
| `FrontComposerEmptyState` | Domain-specific empty state with creation CTA | `FluentIcon` + `FluentButton` + custom layout |
| `FrontComposerSyncIndicator` | Reconnection reconciliation and sync pulse | Custom Blazor component with CSS animation |
