# Visual Design Foundation

FrontComposer does not define a parallel visual system; it consumes Fluent UI Blazor v5's tokens exactly as designed. This section documents how Fluent UI's foundations map to FrontComposer's specific needs -- command lifecycle signaling, projection badges, auto-generated form density, and the framework's default brand differentiator (Teal accent). The zero-override strategy from the Design System Foundation section is the enforcement rule: every decision below either references a Fluent UI token or is implemented as an explicit, documented exception with architectural justification.

**Architectural boundary:** Microservices cannot inject CSS into the composed shell. All theming, token resolution, and visual decisions are shell-resolved. A microservice contributes commands, projections, and (via the customization gradient) typed component overrides -- it never contributes stylesheets, CSS variables, or design tokens. This boundary is what makes zero-override possible at composition scale: the shell is the single source of visual truth, and microservices are visual guests.

### At a Glance: Customization Reality

For evaluators deciding whether FrontComposer fits their constraints, here are the answers to the most common questions, with links to detailed sections below.

| Question | Answer | Details |
|---|---|---|
| **Can I change the accent color?** | Yes -- the default teal (`#0097A7`) is overridable at deployment time via Fluent UI's supported accent theming API. "Zero-override" refers to the framework, not the deployed app. | Color System → Brand accent override policy |
| **Can I customize individual components?** | Yes -- via the customization gradient (annotation → template → slot → full replacement). Custom components inherit lifecycle and accessibility wrappers. | Customization Strategy section |
| **Will this pass a WCAG 2.1 AA audit?** | Yes, provided adopters do not silence the build-time accessibility warnings and their custom components preserve the accessibility contract. | Accessibility Considerations → WCAG criterion mapping |
| **Does it support right-to-left languages?** | v1: inherited from Fluent UI, not explicitly verified by FrontComposer. v2: explicit RTL verification via specimen view. | RTL support below |
| **Does it sync user preferences across devices?** | No. v1 uses LocalStorage (per-device) for all user preferences (theme, density, settings). Cross-device sync is not in v1 scope. | Theme Support → Preference storage rationale |
| **Can my enterprise set deployment-wide defaults?** | Yes -- see the deployment default tier in the density precedence rule. | Density Strategy → Precedence rule |

### Key Decisions with Rejected Alternatives

Each major visual foundation decision was a selection from multiple options. For future contributors, the rejected alternatives are as informative as the chosen ones.

| Decision | Chosen | Principal rejected alternatives | Why rejected |
|---|---|---|---|
| **Design system strategy** | Fluent UI + zero-override + single default accent | (a) Fluent UI + theme-layer wrapper (conventional); (b) custom design system; (c) alternative library (MudBlazor, Radzen) | Theme layer blurs the single source of visual truth; custom is unmaintainable solo; alternative libraries lose Microsoft ecosystem alignment |
| **Badge palette cardinality** | Fixed 6 slots + customization gradient escape | (a) free-form color per developer; (b) 3 slots only; (c) 12-slot extended palette; (d) auto-infer from state name keywords | Free-form destroys consistency; 3 slots insufficient for neutral states; 12 invites slot creep; keyword inference violates "explicit beats implicit" |
| **Density preference model** | Three-level global preference + deployment default tier | (a) fixed hybrid (no user override); (b) two-level (compact/comfortable); (c) per-surface preference | Fixed hybrid fails accessibility users; two-level excludes screen-magnifier users; per-surface multiplies cognitive load |
| **Type specimen verification** | CI-enforced screenshot diffing, self-hosted baselines | (a) no verification; (b) manual checklist; (c) external visual regression service (Chromatic, Percy) | Manual checklists get skipped under deadline pressure; external services add dependency and cost unjustified for solo OSS |
| **Fluent UI dependency protocol** | Strict zero-override + report upstream + narrow critical-bug shim | (a) private patches; (b) vendored fork; (c) escape-hatch theme layer for "quick fixes" | Every alternative erodes the zero-override commitment that makes solo maintenance possible |

### Right-to-Left Language Support

**v1 position:** FrontComposer inherits Fluent UI Blazor v5's RTL behavior without additional verification. Fluent UI supports RTL through its `dir="rtl"` attribute propagation and ships RTL-aware components. In v1, FrontComposer's auto-generation does not explicitly verify that composed layouts, bounded-context-grouped navigation, DataGrid column order, or form field alignment mirror correctly for RTL languages. Adopters building for Hebrew, Arabic, Farsi, or Urdu audiences should validate with real RTL content before production deployment.

**v2 commitment:** Explicit RTL verification via the type specimen view -- render the specimen in both `dir="ltr"` and `dir="rtl"` modes, compare against separate baselines, and include RTL-specific regression tests. Until v2, RTL is best-effort inherited, not guaranteed.

This is an honest scoping: building RTL verification infrastructure in v1 would delay everything else by weeks, and the adoption funnel for v1 is primarily English and French speakers (per the content language strategy). The v2 commitment is firm, not aspirational.

### Color System

This section structures color in three layers: **semantic tokens** (what colors exist), **lifecycle language** (how colors signal system state), and **badge palette** (how colors communicate domain state). Each layer serves a different audience -- theming engineers read the semantic tokens, UX designers read the lifecycle language, and domain developers read the badge palette.

**Brand accent (default, overridable at deployment):** Teal `#0097A7` is the default accent, applied through Fluent UI's supported theming API (CSS custom property `--accent-base-color`). The hex was selected to be distinct from Microsoft's default Fluent blue while remaining within Fluent UI's semantic-token contrast envelope for both Light and Dark themes; it is not derived from a formal brand guideline but from a small comparative palette exercise against the Fluent neutral ramps.

**Override policy:** "Zero-override" is the framework's commitment, not an adopter constraint. Adopters can replace the default accent at deployment time by setting a different accent through the same supported Fluent UI API. The override is documented as a single configuration knob, not a theming system. An adopter who wants purple, orange, or their own corporate color simply sets it; the rest of the zero-override strategy (no custom CSS, no token hacking, no shadow DOM penetration) still applies. What FrontComposer does not support is multiple accents across bounded contexts within the same deployment -- see "Bounded-context sub-branding" below.

**Semantic token mapping:**

| Semantic Slot | Fluent Token | Usage in FrontComposer |
|---|---|---|
| **Accent / Brand** | `--accent-base-color: #0097A7` (default, overridable) | Primary buttons, active nav indicators, sync pulse animation |
| **Neutral** | `--neutral-*` ramp | Shell chrome, borders, dividers, disabled states, text on neutral backgrounds, focus rings (Fluent's default `--colorStrokeFocus2`) |
| **Success** | `--palette-green-*` | Confirmed command state, "Approved"-style badges, success message bars |
| **Warning** | `--palette-yellow-*` / amber | Stale data, reconnecting state, "Pending"-style badges, timeout escalation text |
| **Danger** | `--palette-red-*` | Rejected commands, "Rejected"-style badges, rollback message bars, destructive action confirmations |
| **Info** | `--palette-blue-*` | Informational toasts, "New" badges on newly arrived capabilities, help tooltips |

**Command lifecycle color language:**

| State | Color Slot | Visual Treatment |
|---|---|---|
| **Idle** | Neutral | No color signal -- default button/form appearance |
| **Submitting** | Accent | `FluentProgressRing` on button, button disabled during processing |
| **Acknowledged** | Neutral | Button returns to normal -- lifecycle transitions silently to syncing |
| **Syncing** | Accent | Subtle pulse animation on affected projection row/card -- only when the gap between `Acknowledged` and `Confirmed` exceeds 300ms |
| **Confirmed** | Success | Brief 400ms highlight, then fades to neutral -- never lingers |
| **Rejected** | Danger | Row highlight + `FluentMessageBar` with domain-specific rollback message |
| **Timeout escalation** | Warning | Progressive: 2-10s shows "Still syncing..." text; >10s shows action prompt |

**Brand-signal fusion frequency rule:** The accent serves both brand identity and lifecycle signaling. The sync pulse timer is measured from the `Acknowledged` state transition to `Confirmed`. If that gap is under 300ms, the pulse never fires. On the happy path the user sees no pulse, keeping accent presence meaningful rather than ambient. The pulse is reserved for moments where the system actually needs to explain itself, which makes each appearance load-bearing.

**SignalR-down interaction:** The 300ms-to-2s syncing window relies on SignalR to deliver the `Confirmed` state transition. If SignalR is disconnected when a command enters the syncing window, the pulse does not run indefinitely waiting for a confirmation that will never arrive. The lifecycle wrapper detects SignalR connection state via the `HubConnectionState` API; when disconnected, the lifecycle escalates immediately to the timeout message ("Connection lost -- unable to confirm sync status") rather than displaying the sync pulse. Reconnection reconciliation (documented in the Emotional Response section) then runs a single batched refresh when SignalR resumes.

**Sync pulse and focus ring coexistence:** When a focused element also enters the syncing state, both visual signals must remain distinguishable. The focus ring is a static outline (Fluent's `--colorStrokeFocus2`, neutral); the sync pulse is an animated background glow (accent). They coexist on the same element without conflict: the focus ring remains sharp and static while the pulse animates underneath. The focus ring is never dimmed, desaturated, or reweighted during syncing -- focus visibility is a keyboard accessibility commitment that outranks lifecycle feedback.

**Projection status badge mapping:**

Projection status badges use a **fixed palette of six semantic slots** that domain developers map their states to via annotation. Six is under Miller's cognitive limit and covers the semantic states observed across analyzed domains; additional states route to the customization gradient rather than expanding the palette.

| Semantic Slot | Default Color | Typical Domain States |
|---|---|---|
| `Neutral` | Fluent neutral-foreground | Draft, Created, Unknown |
| `Info` | Fluent info-foreground | Submitted, InReview, Queued |
| `Success` | Fluent success-foreground | Approved, Confirmed, Completed, Shipped |
| `Warning` | Fluent warning-foreground | Pending, Delayed, Partial, NeedsAttention |
| `Danger` | Fluent danger-foreground | Rejected, Cancelled, Failed, Expired |
| `Accent` | Accent (default teal) | Active, Running, Highlighted (rare) |

Developers annotate domain enum values with `[ProjectionBadge(BadgeSlot.Warning)]`. Unknown slots fall back to Neutral with a build-time warning.

**Known limitation:** The framework cannot detect semantically wrong slot annotations (e.g., mapping `Cancelled` to `Success`). Code review is the only mitigation.

**When six slots are not enough:** The escape path is not expanding the palette; it is providing a custom badge component through the customization gradient. Custom badge components must honor the custom-component accessibility contract (see Accessibility Considerations).

**Bounded-context sub-branding (scoped out of v1):** FrontComposer v1 enforces a single accent across all bounded contexts in a composed application. An adopter with six bounded contexts cannot assign six sub-brand accents through framework-supported configuration. This is a deliberate v1 scoping: bounded-context sub-branding interacts with the zero-override strategy, the customization gradient, and the consistency-is-trust principle in ways that need empirical validation before being committed to. Adopters requiring sub-branding in v1 must use the customization gradient to override component rendering per bounded context; this is possible but not supported as a first-class pattern. v2 may introduce a first-class sub-branding configuration if empirical demand justifies it.

**Theme support:**

- **Light theme:** Default Fluent UI light neutral ramp + accent
- **Dark theme:** Default Fluent UI dark neutral ramp + accent (per-theme contrast verification is required via the Type Specimen Verification commitment)
- **System:** Follows OS preference via `prefers-color-scheme` media query
- **Persistence:** User's choice stored in LocalStorage, restored on return
- **Switching:** Instant, no flash -- handled by Fluent UI's `<fluent-design-theme>` at the shell layer

**Preference storage rationale (per-device, not cross-device):** All user preferences in v1 -- theme selection, density level, settings panel state, last-visited navigation section, DataGrid filters -- are stored in LocalStorage on the user's current device. Cross-device preference sync is not in v1 scope. A user who works on both a laptop and a desktop will have independent preference sets on each. This is a deliberate simplicity trade-off: cross-device sync requires server-side preference storage with per-user authentication scope, which is a significant architectural addition for a v1 that is already scope-constrained. Adopters building multi-device user experiences should communicate this limitation to their users or implement cross-device sync at the application layer (outside the framework). v2 may introduce optional server-side preference storage if adoption patterns justify it.

No custom dark-mode tweaking. If a Fluent component looks wrong in dark mode, the fix is upstream in Fluent UI, not in FrontComposer.

### Typography System

**Zero-override strategy:** FrontComposer uses Fluent UI Blazor v5's type ramp exactly as defined. No custom font families, no custom sizes, no custom weights.

**Font families:**

| Usage | Font Stack | Source |
|---|---|---|
| **Body, headings, UI labels** | `"Segoe UI Variable", "Segoe UI", system-ui, sans-serif` | Fluent UI default |
| **Code, identifiers, domain IDs** | `"Cascadia Code", "Cascadia Mono", Consolas, "Courier New", monospace` | Fluent UI monospace stack |

Segoe UI Variable is the emotional anchor -- business users recognize it from Microsoft 365 and Windows 11, which directly serves the "Familiarity as foundation" emotional design principle.

**Text content vs type rendering boundary:** FrontComposer distinguishes sharply between text content customization (allowed, standard) and typography customization (forbidden). Text content flows through the label resolution chain (annotation → resource file → humanized CamelCase → raw field name) and produces strings. Typography selects which Fluent UI type ramp slot renders those strings. Resource files, EN/FR localization, and label annotations change text content, not typography; they do not select different fonts, sizes, or weights. The two systems are independent by design: the label resolution chain is about *what* to display, the typography system is about *how* to display it.

**Type ramp mapping for auto-generated UI (living table, version-pinned):**

This table is a **living specification**: mappings are expected to evolve when the type specimen view reveals hierarchy issues. To preserve the framework's determinism constraint (identical inputs → identical output), living tables follow strict versioning discipline:

- **Patch version (1.2.3 → 1.2.4):** Mapping changes are forbidden in patch releases.
- **Minor version (1.2.x → 1.3.0):** Mapping changes are permitted and must be documented in release notes under "Visual specification changes" with before/after specimen screenshots committed to the repository.
- **Major version (1.x.x → 2.0.0):** Structural restructuring of the mapping is permitted; migration notes required.
- **Code generators and LLMs:** Generated code that references specific ramp slots should pin to a specific FrontComposer minor version.

| UI Element | Fluent Typography Slot | C# API Constant | Purpose |
|---|---|---|---|
| App title (header) | `Title1` | `Typography.AppTitle` | Shell header, brand area |
| Bounded context heading | `Subtitle1` | `Typography.BoundedContextHeading` | Nav group headers, section titles |
| View title | `Title3` | `Typography.ViewTitle` | "Order List", "Send Increment Counter" |
| Section heading | `Subtitle2` | `Typography.SectionHeading` | Card titles, detail view groupings |
| Field label | `Body1Strong` | `Typography.FieldLabel` | Form labels, DataGrid column headers |
| Body text | `Body1` | `Typography.Body` | Descriptions, help text, empty state messages |
| Secondary text | `Body2` | `Typography.Secondary` | Timestamps, metadata, subtle hints |
| Caption | `Caption1` | `Typography.Caption` | Validation messages, "Last updated" indicators |
| Code / IDs | `Body1` + monospace | `Typography.Code` | Aggregate IDs, command names in dev-mode overlay |

**Typography API surface:** The C# constants above are exposed through the `Hexalith.FrontComposer.Typography` static class. Custom components that need to match the framework's type hierarchy reference the constant (e.g., `<FluentLabel Typo="@Typography.ViewTitle">`) rather than hard-coding the Fluent slot. When a living-table remapping occurs in a minor version, all components referencing the constant update automatically; components hard-coding the Fluent slot do not. The API surface is the stable contract; the mapping behind it is the living table.

**Hierarchy via weight and size, not font variety:** The entire product uses two font families. Hierarchy is achieved through Fluent UI's predefined weight combinations (Regular 400, Semibold 600, Bold 700) and the ramp's size steps.

**Label resolution chain** (defined in the Discovery section) applies to all typographic elements.

**Line height and spacing:** Inherited from Fluent UI's type ramp definitions.

**Type Specimen Verification commitment (CI-enforced):**

A type specimen view is rendered inside the FrontComposer shell as part of the CI pipeline on every pull request and every release branch. The specimen exercises every ramp slot, every semantic color token, both Light and Dark themes, and all three density levels in context: one DataGrid with column headers and six badge states, one flat command form with the five-state lifecycle wrapper, one expanded detail view, one multi-level nav group, and (from v2) one RTL rendering. The specimen is rendered to deterministic screenshots per theme × per density × per direction, compared against committed baselines, and a diff beyond tolerance fails CI and blocks merge.

**Change-control discipline for baseline updates:** When a specimen regeneration is intentional (a living-table remapping, a Fluent UI version bump, a baseline recalibration), the PR updating the baseline must include:

1. A rationale paragraph explaining why the baseline changed
2. Before/after screenshot pairs for the affected specimen slots
3. A reference to the specification change that justifies the baseline update
4. Reviewer sign-off specifically on the baseline change, separate from the code change

This prevents silent baseline updates that mask regressions.

Per-theme contrast verification and density parity testing are both performed against the specimen; the specimen pipeline runs all combinations (theme × density × language direction) and fails CI if any combination regresses. Zero-override does not mean zero-verification; it means verification happens at the specimen boundary.

### Spacing & Layout Foundation

**Base unit:** 4px grid, consumed through Fluent UI's spacing tokens. No custom spacing values.

**Spacing scale (Fluent UI tokens):**

| Token | Pixel Value | Usage |
|---|---|---|
| `spacing*XS` | 2px | Badge internal padding, inline icon gap |
| `spacing*S` | 4px | Tight groupings, button internal spacing |
| `spacing*M` | 8px | Default gap between related elements |
| `spacing*L` | 12px | Between form fields, DataGrid row padding |
| `spacing*XL` | 16px | Between form sections, card internal padding |
| `spacing*XXL` | 20px | Between major page sections |
| `spacing*XXXL` | 24px | Content margins, bounded context separation |

**Density strategy (three-level global preference with deployment default tier):**

Density is a three-level user preference applied uniformly across the shell. The levels are:

| Level | Purpose | Default Application |
|---|---|---|
| **Compact** | Maximize visible rows for queue-processing power users | Factory default for DataGrids and dev-mode overlay |
| **Comfortable** | Balanced accessibility and efficiency | Factory default for detail views, forms, and navigation sidebar |
| **Roomy** | Larger margins for screen magnifier users | Never a factory default; user-activated only |

The Roomy level is a first-class, permanent feature in v1. If adoption patterns reveal that Roomy is under-used, that observation can inform v2 planning through direct adopter feedback.

**Four-tier precedence rule:** When multiple sources specify density, the resolution order is:

1. **Explicit shell-level user preference** (set via settings UI, stored in LocalStorage)
2. **Deployment-wide default** (set by the adopter via configuration at application startup; enables enterprise policies like "all call-center users default to compact")
3. **Factory hybrid defaults** (compact for DataGrids, comfortable elsewhere)
4. **Per-component default** (lowest priority; a custom component cannot override shell, deployment, or factory layers)

A microservice cannot unilaterally override the user's density preference. Custom components requiring a different density must document the exception in code review. Silent per-component density overrides are a code review failure.

**Factory hybrid defaults (before any user or deployment preference):**

| Surface | Default Density | Rationale |
|---|---|---|
| DataGrids (projection lists) | Compact | Queue processing; top-10 actions |
| Detail views (expanded rows, cards) | Comfortable | Reading-oriented |
| Command forms (flat commands) | Comfortable | Input focus + validation feedback |
| Navigation sidebar | Comfortable | Click accuracy at 10+ bounded contexts |
| Dev-mode overlay | Compact | Dense tooling convention |

**User override mechanism:**

- **Storage key:** `frontcomposer:density` with values `{compact, comfortable, roomy}`
- **Implementation:** Written to `<body>` as CSS custom property `--fc-density`; one source of truth, zero per-component density logic
- **Per-device storage:** LocalStorage (cross-device sync is out of scope for v1 -- see Preference Storage Rationale above)

**Settings UI location:** Density, theme, and other user preferences are accessed via a settings icon in the top-right corner of the shell header (adjacent to the theme toggle and command palette trigger). The icon uses the Fluent `Settings` icon for immediate recognizability. Opening it reveals a `FluentDialog` panel with three density radio options, a theme selector, and a live preview of one DataGrid row, one form field, and one nav item at the selected density. The settings panel is keyboard-accessible via the command palette (Ctrl+K → "Settings") and via a direct keyboard shortcut (Ctrl+,).

**Density parity testing commitment:** All three density levels must pass the same visual verification bar during the Type Specimen Verification CI check. The specimen view is rendered three times -- once per density level -- and each rendering is compared against its own baseline. "Roomy" receives the same testing as the factory defaults precisely because its users are the most vulnerable to rendering regressions.

**Application shell layout:**

The shell uses Fluent UI Blazor v5's declarative `FluentLayout` with area-based composition:

```
+-------------------------------------------------------+
|  HEADER                                               |
|  App title | Breadcrumbs | Ctrl+K | Theme | Settings  |
+-----------+-------------------------------------------+
|           |                                           |
|  NAV      |  CONTENT                                  |
|           |                                           |
|  Bounded  |  Projection view / Command form /         |
|  contexts |  Detail view                              |
|  as       |                                           |
|  collap-  |                                           |
|  sible    |                                           |
|  groups   |                                           |
|           |                                           |
+-----------+-------------------------------------------+
```

- **Header height:** Fluent default (48px)
- **Sidebar width:** Fluent default collapsible (~240px expanded, ~48px collapsed)
- **Content max-width:** None for DataGrids; forms constrain to 720px for readability
- **No hero section** -- this is an application, not a landing page
- **Responsive breakpoints:** Fluent UI defaults (tablet collapses nav to drawer; mobile usable but not optimized)

**Grid system within content:**

- Forms: single column, max 720px wide, field groupings via `FluentCard` or `FluentAccordion`
- DataGrids: full-width, virtualized, horizontal scroll when column count exceeds viewport
- Dashboards (v2): 12-column responsive grid using Fluent UI spacing tokens

**Layout principles:**

1. **Shell chrome is minimal, content dominates.** The Fluent shell is the frame; the projection/command views are the content.
2. **Context preservation over navigation.** Expand-in-place is preferred over new pages.
3. **Consistent rhythm across bounded contexts.** The same tokens, ramp, and density rules apply to every auto-generated view from every microservice. Trust accumulates through predictability.

### Accessibility Considerations

**Baseline: WCAG 2.1 AA is the framework's committed conformance target -- not AAA, not WCAG 3.0 APCA.** All commitments below implement specific WCAG 2.1 criteria and are verified against them in CI.

**Inherited baseline:** Fluent UI Blazor v5 provides ARIA attributes, keyboard navigation, screen reader labels, high contrast mode, and focus management out of the box. FrontComposer does not reimplement any of these; it inherits them and commits to not breaking them through customization.

**FrontComposer's additional commitments (mapped to WCAG 2.1 criteria):**

| # | Commitment | WCAG 2.1 Criterion | Enforcement |
|---|---|---|---|
| 1 | Color is never the sole signal -- every badge, lifecycle state, and error combines color with text or icon | 1.4.1 Use of Color | CI check: specimen view asserts every badge has a text label; build warning for any badge rendered color-only |
| 2 | Focus visibility in custom components preserves Fluent's `--colorStrokeFocus2` (not the accent) | 2.4.7 Focus Visible | Code review checklist; custom component accessibility contract |
| 3 | Keyboard navigation parity -- command palette, nav groups, DataGrid rows, inline actions all keyboard-reachable in DOM order | 2.1.1 Keyboard, 2.4.3 Focus Order | Documented tab order per shell area; specimen view includes tab-order test |
| 4 | Screen reader announcement of lifecycle state changes: two-category policy for v1 (polite for confirmed/syncing/info; assertive for rejected/timeout). A third "non-interrupting critical" category is explicitly deferred to v2 | 4.1.3 Status Messages | Lifecycle wrapper enforces the two categories; v1 has no ambiguity |
| 5 | Reduced motion preference respected via `prefers-reduced-motion: reduce`; motion replaced with instantaneous state changes | 2.3.3 Animation from Interactions | CSS media query; specimen view tested in both motion modes |
| 6 | Domain-language labels humanized for screen readers via CamelCase expansion | 1.3.1 Info and Relationships, 4.1.2 Name, Role, Value | Label resolution chain; build-time warning for unhumanized field names |
| 7 | Form field associations: explicit `<label for="">`, `aria-describedby` for validation messages | 1.3.1 Info and Relationships, 3.3.2 Labels or Instructions | Generated by `EditContext`-wired form generator; CI asserts every auto-generated form field has an accessible name |
| 8 | Contrast guarantees: teal accent meets WCAG AA (4.5:1 for normal text, 3:1 for large text and UI components) against both Light and Dark neutral backgrounds; body text uses Fluent neutral-foreground which is AA-compliant by design | 1.4.3 Contrast (Minimum), 1.4.11 Non-text Contrast | Per-theme contrast verification via specimen view; CI asserts contrast ratios |
| 9 | Build-time accessibility warnings for missing accessible names, labels, landmark roles. Warnings are severity-configurable but FrontComposer's shipped build templates default to `TreatWarningsAsErrors=true` for accessibility warnings | 4.1.2 Name, Role, Value | Roslyn analyzers + build-time template |
| 10 | Dev-mode overlay itself is keyboard-accessible and screen-reader-friendly | 2.1.1 Keyboard | Dev-mode overlay tested in specimen view |
| 11 | Density preference respects accessibility needs via the Roomy level and parity testing -- see Density Strategy section | 1.4.4 Resize Text (partial) | Specimen view renders all three density levels |
| 12 | Zoom/reflow support: content reflows correctly at up to 400% browser zoom without loss of content or functionality | 1.4.10 Reflow | Specimen view renders at 100%, 200%, and 400% zoom levels |
| 13 | Non-text contrast: UI components (button borders, focus rings, dividers, form field borders) meet 3:1 contrast against adjacent colors in both themes | 1.4.11 Non-text Contrast | Specimen view asserts non-text contrast ratios |
| 14 | Windows High Contrast mode (forced-colors): all custom components use `forced-colors` CSS media query and system color keywords to remain legible | 1.4.6 Contrast (Enhanced) for HC mode | Specimen view rendered in forced-colors mode |

**Custom component accessibility contract:**

When an adopter provides a custom component through the customization gradient (slot, template, or full replacement), the component is required to preserve the accessibility baseline. The contract is:

1. **Expose an accessible name** -- either via the `aria-label` attribute or via visible text content. Build-time warning if neither is present.
2. **Preserve keyboard reachability** -- the component must be in DOM order and focusable where appropriate.
3. **Preserve focus visibility** -- do not override `--colorStrokeFocus2` or suppress focus outlines.
4. **Announce state changes** -- if the custom component has lifecycle states, announcements must use the same `aria-live` politeness categories as the framework.
5. **Respect reduced motion** -- any animation must honor `prefers-reduced-motion`.
6. **Support forced-colors mode** -- use system color keywords in CSS for Windows High Contrast.

The contract is enforced through Roslyn analyzers where possible and code review checklists where not. Custom components that do not satisfy the contract are flagged by the dev-mode overlay.

**Automated accessibility testing:** FrontComposer's CI pipeline integrates `axe-core` (via Playwright's `@axe-core/playwright`) and runs it against the type specimen view on every pull request. Any axe violation at the "serious" or "critical" severity level fails CI. "Moderate" and "minor" violations are reported but do not block merge.

**Real-screen-reader verification:** Automated accessibility testing catches structural and attribute issues but cannot verify that announcements sound correct to a human user. FrontComposer commits to manual verification of the five custom components and the type specimen view against **NVDA** (Windows), **JAWS** (Windows, for enterprise audit parity), and **VoiceOver** (macOS) before each release branch is cut. Verification logs are committed to `docs/accessibility-verification/` and dated per release.

**What FrontComposer does not guarantee:**

- **Accessibility of custom Blazor components that fail the custom-component accessibility contract.** The contract is the line; beyond it, adopters own their own accessibility.
- **Localized screen-reader pronunciation quality.** FrontComposer provides EN/FR resource files; other languages depend on microservice teams and OS-level screen reader voices.
- **Accessibility of adopter-provided content.** If an adopter writes a projection description in all-caps with no punctuation, FrontComposer cannot correct it.
- **Immunity from upstream Fluent UI Blazor v5 bugs.** The zero-override strategy creates a hard dependency on Fluent UI Blazor v5 correctness. The managed protocol:
  1. **Report upstream.** Issues affecting auto-generated output are reported via the `microsoft/fluentui-blazor` GitHub repository with minimal reproduction.
  2. **Blocking issues block releases.** No temporal tolerance for non-critical bugs.
  3. **Narrow critical-bug carve-out.** A private patch is permitted only when all three conditions are met: (a) the bug causes data loss, security vulnerability, or complete workflow blockage for a production adopter; (b) upstream cannot ship a fix within the adopter's tolerance window; (c) the patch is a time-boxed `[Obsolete]`-annotated shim removed automatically when upstream fixes are released. The shim is public, documented, and audited at every release. Non-critical bugs do not qualify.
  4. **No forks, no selective CSS overrides.** Even the critical-bug shim uses supported Fluent UI extension points, never CSS injection or shadow DOM penetration.
