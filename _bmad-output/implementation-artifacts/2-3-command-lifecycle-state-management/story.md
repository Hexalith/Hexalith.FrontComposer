# Story

As a developer,
I want a lifecycle state service that tracks each command through five states with ULID-based idempotency and guarantees exactly one user-visible outcome,
so that every command submission is traceable, replay-safe, and never produces silent failures or duplicate effects.

---
