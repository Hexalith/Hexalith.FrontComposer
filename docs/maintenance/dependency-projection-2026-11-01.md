# Dependency projection maintenance, 2026-11-01

Every root submodule now selects the head of its repository's main branch. The
Platform source projection seals Platform commit
4c43913231b54214b40574d02ce5ed5ed30c470d and Builds commit
17616d439b36957a7c5da078e293e598bb339790. The owner inventory, root selector and
Builds fingerprints were recomputed from committed source. The `.gitmodules`
digest, canonical helper bytes, the seven inventory-only dependency identities
and the consumer configuration fingerprint are unchanged.

The earlier seal of Platform commit 55592ec3a57e26d11928971d5e04d9e50235a959 was
not enough. That commit pinned Builds 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604
and EventStore 9a962a1be8cdc0df814a63254eed0fca77328e54, two local sync commits
that were never published, so exact catalog acquisition failed with
`not our ref`. Every nested pin of the newly selected commits is on its own
repository's main branch.

The policy-transition event remains subject to its previous policy and can be
refused. Subsequent descendants use the landed policy as their event base;
they must still pass exact-object acquisition, graph validation, and the normal
runtime checks. No earlier CI result is reclassified as successful.

Verification: with the seal commit as event base, `dependency_graph.py
acquire-catalogs`, `validate --commit`, and the CI `acquire` plus `diff` pair
pass for the first descendant before it is pushed.
