# Hexalith.FrontComposer
The Hexalith Blazor Front Shell.

## Dependency source selection

The [dependency policy](eng/dependency-graph-policy.json) binds an exact
consumer source projection: Platform `c1997c60f31e08d945b44cb7aa2afd7800e8ff69`,
Builds `0577f0c2d70bd4c2999aa3d98a705c83919bfc00`, the two canonical Dapr
Compile inputs, and all root dependency selectors. SDK and published package
selections remain separate from this source projection; a pointer change alone
cannot authorize new code. The complete selection and compatibility holds are
recorded in [dependency governance](eng/dependency-governance.md).

The pointer synchronization on 2026-11-06 initially selected a different
projection without its policy proof, and CI refused it. The corrective commit
`b08612337b791fef0d819094d6c345c3d8ab4562` restored six dependency selectors
from the last fully green source `aa8578318971da04ce7d7bd07928d08c6b95bc1a`.
That historical restoration is preserved. The maintenance refresh now records
the current committed selections and matching seals. Nested checkouts remain
uninitialized and governance guards remain enabled.

CI evaluates both the actual previous source and candidate under the previous
source policy. This reseal requires a separate later-base activation push; it
cannot authorize itself. Current qualification requires fresh CI results and
does not inherit runtime, operator, deployment or publication acceptance from
the historical source pins or local checks.
