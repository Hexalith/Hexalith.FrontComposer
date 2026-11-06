# Hexalith.FrontComposer
The Hexalith Blazor Front Shell.

## Dependency source selection

The [dependency policy](eng/dependency-graph-policy.json) binds an exact
consumer source projection: Platform `aaa30aff1038688a921494a78c9fc53fc5eb8836`,
Builds `06a51f55bfe2e03a06e1121d94a41e98f1e3e251`, the two canonical Dapr
Compile inputs, and all root dependency selectors. These recorded source pins
remain a compatibility constraint while newer runtime tuples are qualified.
The repository's SDK and published package selections are separate from this
source projection; a source pointer change alone cannot authorize new code.

The pointer synchronization on 2026-11-06 selected a different projection
without its corresponding policy proof, and CI refused it. The corrective
source commit retains all current implementation and documentation changes
and restores the six recorded dependency selectors from the last fully green
source `aa8578318971da04ce7d7bd07928d08c6b95bc1a`. No nested checkout is
initialized and no governance guard is disabled.

CI evaluates both the push's actual previous source and its candidate. A
correction after an invalid previous selection retains that refusal in the
historical run; subsequent exact-source checks must pass against the restored
closure. Current qualification requires fresh CI results and does not inherit
runtime, operator, deployment or publication acceptance from these source pins.
