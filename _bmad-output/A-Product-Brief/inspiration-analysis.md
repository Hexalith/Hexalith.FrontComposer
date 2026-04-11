# Inspiration Analysis: Hexalith FrontComposer

## Reference Sites

### 1. Fluent UI Blazor v5 Documentation
- **URL:** https://fluentui-blazor-v5.azurewebsites.net/
- **Why chosen:** This is the component library FrontComposer is built on -- the visual inspiration IS the foundation

**What drew the client:**
1. Sidebar navigation with collapsible groups -- scales to many components/modules
2. Clean, no-nonsense aesthetic -- content-focused, no decorative elements
3. Dark mode as first-class citizen -- developers live in dark mode

**Specific elements liked:**
- Hierarchical nav groups (Get Started, Components, Labs)
- Top toolbar with theme toggle, GitHub link
- Minimal footer with version info
- Clean content area with clear headings

## Synthesized Design Principles

| Category | Principle | Rationale |
|---|---|---|
| **Layout** | Sidebar navigation with collapsible groups | Scales to many microservices -- each service becomes a nav group |
| **Aesthetic** | Clean, no-nonsense, content-focused | Matches "understated" personality -- no decorative noise |
| **Theme** | Dark mode as first-class citizen | Developers live in dark mode -- must be default-ready |
| **Navigation** | Hierarchical grouping with expandable sections | Maps naturally to bounded contexts / microservice modules |
| **Content** | Functional over decorative -- data and actions front and center | Business users need efficiency, not eye candy |
| **Consistency** | Follow Fluent UI patterns exactly -- no custom styling | Reduces maintenance, improves LLM compatibility, stays predictable |

## Key Takeaway

FrontComposer should look and feel like a natural extension of Fluent UI Blazor -- not a custom-designed application layered on top. Same patterns, same components, same visual language. The shell IS the library.

---

**Status:** Complete
**Last Updated:** 2026-04-06
