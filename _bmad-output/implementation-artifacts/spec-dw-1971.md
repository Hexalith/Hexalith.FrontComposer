---
title: 'DW-1971 Add a generated-command visual baseline'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Capture the live Configure Counter page in the Linux Playwright matrix once the command heading, breadcrumb and form have rendered. Keep the existing visual governance and route behavior checks.

</frozen-after-approval>

## Implementation Notes

- The governed snapshots use the live generated form and focused command heading. Review added the baseline rationale and updated the existing visual commands. Linux Chromium, Firefox and WebKit comparisons passed with updates disabled.
