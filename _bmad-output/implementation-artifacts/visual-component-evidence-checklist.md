# Visual Component Evidence Checklist

Use this checklist for any story that changes Fluent component layout, scoped CSS, generated UI markup,
hover/focus/touch behavior, or visual baselines.

| Item | Required evidence |
| --- | --- |
| Rendered DOM attachment | bUnit or browser evidence showing the styled node actually exists in rendered markup. |
| Scoped CSS reachability | Proof that CSS isolation selectors reach a real node, or use inline/component parameters when no scoped node exists. |
| Fluent web-component targeting | Proof against actual Fluent v5 light DOM/parts before using `::part()` or tag selectors. |
| Computed-style or behavior proof | Browser/computed-style evidence when source inspection cannot prove the visual result. |
| Accessibility interaction | Keyboard focus, hover, and touch paths named when tooltip or icon-only UI changes. |
| Visual/browser lane ownership | If local Playwright/Kestrel is blocked, record the CI lane, owner, and expected artifact path. |
| Snapshot/baseline intent | Verify snapshots and visual baselines are either unchanged by evidence or intentionally updated. |
