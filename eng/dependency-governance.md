# Dependency governance source projection

The FrontComposer AppHost selects Platform commit
`aaa30aff1038688a921494a78c9fc53fc5eb8836` and Builds commit
`06a51f55bfe2e03a06e1121d94a41e98f1e3e251`. The matching sealed
inventory, two canonical source-file digests, consumer configuration and root
selectors are recorded in `dependency-graph-policy.json`.

The push graph gate evaluates a candidate under its event base's committed
policy. The reviewed policy reseal at
`0456bba7198d6ed8c661fd24aa22286eae3bba87` cannot authorize its own
source or selector changes. This documentation-only follow-up makes that
reviewed policy the event base for the next exact graph check. No CI outcome
for the reseal or this follow-up is asserted here; the actual hosted gates
remain required.

Catalog preparation first validates both root mappings and their approved
existing object stores. It acquires only missing exact historical owner commits
from canonical policy-approved repositories before reading their inventories,
using bounded network retries. Acquisition does not move any checkout HEAD,
change working-tree bytes or initialize nested submodules. The seven
inventory-only Platform descendants remain outside global trust and execution.
