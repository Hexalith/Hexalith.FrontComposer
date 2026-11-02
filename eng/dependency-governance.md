# Dependency governance source projection

The FrontComposer AppHost selects Platform commit
`92f87e6a49e4357f91b2ffb821e044931291c0e6` and Builds commit
`2afc90eb107d73490aa0299a32efbca4bc160b05`. The matching sealed
inventory, source-file digests, and root selectors are recorded in
`dependency-graph-policy.json`.

The push graph gate evaluates a candidate under its event base's committed
policy. The policy correction at `a6165112` therefore encountered the older
source digests in its base policy. This follow-up commit makes the corrected
policy the event base for the next exact graph check.
