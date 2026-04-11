# Visual Direction: Hexalith FrontComposer

---
stepsCompleted: []
---

## Context

- **Product:** Hexalith FrontComposer
- **Personality:** The Seasoned Architect -- opinionated, expert, reliable, pragmatic, understated
- **Tone:** Technical, precise, concise, confident
- **Primary inspiration:** Fluent UI Blazor v5 documentation site
- **Design system:** Microsoft Fluent UI
- **Existing visual identity:** None -- blank canvas

## Existing Brand Assets

| Asset | Status | Action |
|---|---|---|
| Logo | None | Create -- needed for GitHub, NuGet, docs |
| Colors | None | Create -- align with Fluent UI palette |
| Typography | None | Use Fluent UI defaults |
| Imagery | None | Not a priority for a framework |

**Brand Constraints:**
- Must follow Microsoft/.NET branding guidelines when using Microsoft trademarks (Fluent UI, .NET, Blazor, DAPR logos/names)

## Visual References

### Primary Reference
- **Fluent UI Blazor v5** (https://fluentui-blazor-v5.azurewebsites.net/)
- Sidebar navigation with collapsible groups
- Clean, no-nonsense, content-focused aesthetic
- Dark mode as first-class citizen

### Design Directive
Follow web design best practices. No specific anti-references -- the constraint is principled adherence to established patterns rather than avoiding specific styles.

### Mood Keywords
1. **Functional** -- every element serves a purpose
2. **Systematic** -- follows design system rules, not ad-hoc decisions
3. **Professional** -- enterprise-grade, trustworthy
4. **Clean** -- whitespace, clear hierarchy, no clutter
5. **Native** -- feels like a natural Fluent UI application, not a themed website

## Design Style

- **UI Style:** Flat Design (Fluent UI native) -- subtle depth cues via elevation only on flyouts/dialogs
- **Aesthetic:** Minimalism -- maximum whitespace, only essential elements, signal over noise
- **Principle:** Don't fight the design system -- use Fluent UI exactly as intended

## Color Direction

- **Color System:** Fluent UI token system -- no custom overrides
- **Accent Color:** Teal/Cyan (`#0097A7`) -- distinct Hexalith identity, differentiates from Microsoft default blue
- **Themes:** Dark + Light mode, both fully supported via Fluent theming engine
- **Palette:** Cool, muted, professional

## Typography Direction

- **Headlines & Body:** Fluent UI defaults (Segoe UI / system font stack)
- **Code:** Cascadia Code / Consolas (monospace)
- **Hierarchy:** Clear hierarchy via weight and size, not font variety
- **Branding:** Custom identity comes from accent color and logo only, not typography overrides

## Layout Direction

### Application Shell
- **Left sidebar:** Collapsible navigation, auto-discovered from microservices, grouped by bounded context
- **Top bar:** App title, theme toggle, user profile, global actions
- **Content area:** Where microservice UI fragments render -- forms, data grids, detail views
- **No hero section** -- this is an application, not a landing page

### Content Layout Within Pages
- Data grids for projection lists (Fluent UI DataGrid)
- Cards for dashboard overview widgets
- Single-column forms for command submission
- Responsive grid adapts desktop to tablet

### Navigation
- **Desktop:** Persistent sidebar with expandable groups per bounded context
- **Tablet/Mobile:** Collapsible drawer
- Sticky top bar with breadcrumbs

## Visual Effects

| Effect | Level | Rationale |
|---|---|---|
| **Shadows** | Subtle | Fluent UI elevation tokens only -- dialogs, flyouts, cards |
| **Animations** | Subtle | Fluent UI transitions only -- expand/collapse, page transitions |
| **Parallax** | None | Application, not a website |
| **Hover effects** | Subtle | Fluent UI built-in hover states on interactive elements |

### Performance
- Blazor WebAssembly lazy loading per microservice module
- No custom animations or heavy visual effects
- Fluent UI's built-in performance optimizations

## Imagery

### Photography
Not applicable -- this is a code framework, not a brand or marketing site.

### Icons
- Fluent UI's built-in icon library exclusively
- No custom icons -- consistency with the design system
- Functional purposes only (navigation, actions, status indicators)

### Illustrations / Diagrams
- Architecture diagrams for documentation (two-layer composition model, microservice discovery flow)
- Logo for GitHub, NuGet, docs header
- No decorative illustrations

## Design Constraints

- Must follow Microsoft/.NET branding guidelines for trademarks
- Fluent UI design system is the authority -- no fighting it
- Desktop-first, responsive down
- WCAG accessibility from day one
- Dark mode as first-class citizen
- No photography, no decorative imagery
- Icons from Fluent UI library only

## Visual DNA Summary

```
Style:        Flat Design / Minimalism -- Fluent UI native, no custom overrides
Colors:       Fluent UI token system + Teal (#0097A7) accent, dark + light themes
Typography:   Segoe UI system stack + Cascadia Code for code blocks
Mood:         Functional, Systematic, Professional, Clean, Native
Key Element:  Sidebar navigation with auto-discovered microservice groups
```

---

**Status:** Visual Direction Complete
**Next Phase:** Platform Requirements
**Last Updated:** 2026-04-06
