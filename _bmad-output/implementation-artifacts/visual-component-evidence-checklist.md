# Visual Component Evidence Checklist

Status: active review-promotion gate
Owner: Test Architect
Source: E8-AI-1, Epic 8 retrospective follow-through

Use this checklist before moving any story to review when it changes Fluent component layout, scoped
CSS, generated UI markup, hover/focus/touch behavior, responsive behavior, or visual baselines.

Promotion rule: source-string assertions are not enough for Fluent component layout or CSS changes.
The story record must include rendered-DOM proof or computed-style/browser proof for each applicable
visual risk before review promotion. If a row is not applicable, record a short N/A rationale. If local
browser execution is blocked, use the standard Test Evidence language: exact command, local result,
blocker timing, fallback evidence, CI lane, owner, expected artifact, and named responsibility ID when one
exists. Do not use a generic "Playwright blocked locally" note as the only evidence for a visual/layout
claim.

| Item | Required evidence |
| --- | --- |
| Rendered DOM attachment | bUnit or browser evidence showing the styled node actually exists in rendered markup. |
| Scoped CSS reachability | Proof that CSS isolation selectors reach a real node, or use inline/component parameters when no scoped node exists. |
| Fluent web-component targeting | Proof against actual Fluent v5 light DOM/parts before using `::part()` or tag selectors. |
| Computed-style or behavior proof | Browser/computed-style evidence when source inspection cannot prove the visual result. |
| Accessibility interaction | Keyboard focus, hover, and touch paths named when tooltip or icon-only UI changes. |
| Shell accent-as-thread guard | For Shell chrome or visual stories, name `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background` with Passed/Failed/Blocked result, or record N/A for visual stories that do not touch Shell chrome. |
| Visual/browser lane ownership | If local Playwright/Kestrel is blocked, record the required command, blocker timing, fallback evidence, CI lane, owner, expected artifact path, and named responsibility ID when one exists. |
| Snapshot/baseline intent | Verify snapshots and visual baselines are either unchanged by evidence or intentionally updated. |

## Story Record Template

Add a short evidence block to the story's Dev Agent Record, Test Evidence, or review handoff:

```md
Visual component evidence checklist:
- Required: yes/no
- Rendered DOM attachment: <test, screenshot, Playwright assertion, or N/A rationale>
- Scoped CSS / Fluent targeting: <selector proof, inline/component-parameter proof, or N/A rationale>
- Computed style / behavior: <browser/computed-style proof, focused bUnit behavior proof, or N/A rationale>
- Accessibility interaction: <keyboard/hover/touch proof or N/A rationale>
- Shell accent-as-thread guard: <FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background result, blocker/fallback/CI authority, or N/A rationale>
- Visual/browser lane: <required command, local result, blocker timing, fallback evidence, named CI responsibility ID + CI lane + owner + artifact path>
- Snapshot/baseline intent: <unchanged, intentionally updated, or N/A rationale>
```
