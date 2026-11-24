---
title: 'DW-1954 Pin the public schema test-vector identifier'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Assert the literal v1 vector identifier in the runtime Contracts golden test. Changes to the identifier or to the default carried by the runtime canonical payload must fail the test.

</frozen-after-approval>

## Implementation Notes

- The Contracts test checks the literal hfc-schema-v1 on a runtime canonical payload. Review confirmed that this path uses the public record constructor default.
