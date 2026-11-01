# Dependency projection maintenance, 2026-11-01

The Platform source projection now seals the already selected Platform commit
55592ec3a57e26d11928971d5e04d9e50235a959 and Builds commit
d3fb9d3d62c32953a1769a6b4cdb63ad3c983a3f. The owner inventory and consumer
configuration fingerprints were recomputed from committed source. Canonical
helper bytes and the seven inventory-only dependency identities are unchanged.

The policy-transition event remains subject to its previous policy and can be
refused. Subsequent descendants use the landed policy as their event base;
they must still pass exact-object acquisition, graph validation, and the normal
runtime checks. No earlier CI result is reclassified as successful.

Verification: the existing ConsumerProjectionTests pass, including the case
that rejects candidate self-authorization and accepts a later-base evaluation.
