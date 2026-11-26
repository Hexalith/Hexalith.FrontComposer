---
title: 'Deferred Counter and command form follow-ups'
type: 'bugfix'
created: '2026-11-26'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Continue from the prior deferred-work stopping point at DW-1076. Complete the
next six actionable rows, record skipped rows and their reasons, and land one
commit per completed row after verification.

</frozen-after-approval>

## Implementation Notes

- DW-1079 times changes from file save to observation in the running Counter
  app. Record the default watcher crash and the rebuild fallback timings.
- DW-1083 fills gaps in rendered form, converter, and property-based coverage.
  Later stories cover the current placeholder and pending submit behavior.
- DW-1084 requires regenerated adopter applications before removing legacy
  command discovery. DW-1085's directly mounted form was replaced by the
  density-aware renderer, which the sample already uses unqualified.
- DW-1087 uses idempotent options registration and verifies adopter configuration.
- DW-1089 applies confirmed deltas in the store and keeps the scope-generation
  and correlation rules of the generated projection reducers.
- DW-1090 verifies the pinned Fluent picker's native typed date editors across
  cultures. DW-1091 defaults new nonnullable enum fields to the first declared
  member when zero is undefined and preserves explicit model values.

## Review Triage Log

- High, fixed. Parse the declared enum name when initializing the model so
  a member marked obsolete with a compiler error still compiles.
- High, fixed. Pass existing command IR to both emitters through the generator.
  The finite mutation gate passed with the sealed transforms restored.
- Medium, fixed. Apply queued deltas in order and retain the latest timestamp
  when confirmations arrive out of capture order.
- Medium, fixed. Exercise valid zero declared after a nonzero member and
  assert the selected option in the rendered form.
- Low, fixed. The generated placeholder regression asserts visible text,
  the accessible label, and the documentation link.
- Low, fixed. Document the watcher command and how to observe the probe.
  DW-1990 owns further repeated measurement and performance work.
- Low, closed. The public emitter overloads accept standalone rendering
  models. They have no declaration-order metadata. Both source-generator
  output paths receive the same command IR, which the mounted tests exercise.
- Low, closed. DW-1083 names integer and culture-sensitive decimal cases.
  The focused converter tests exercise those inputs and nullable clearing.
