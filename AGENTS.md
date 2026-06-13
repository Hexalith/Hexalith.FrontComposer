# Agent Instructions

## Git submodules

This repository's submodules reference each other **circularly**
(`Hexalith.FrontComposer` ↔ `Hexalith.EventStore` ↔ `Hexalith.Tenants` …).
Recursive initialization therefore descends forever and, on Windows, fails with
`Filename too long` once nested paths exceed the 260-character limit.

**Rules:**

- **Never** initialize submodules that are nested inside other submodules.
- Initialize **only** the submodules at the root of the repository.
- **Never** use `--recursive` when running `git submodule update`.
- **Never** use `--remote` (it moves submodules off their pinned commits).

Root-level submodules only:

- `Hexalith.Builds`
- `Hexalith.Commons`
- `Hexalith.EventStore`
- `Hexalith.Tenants`

**Correct commands:**

```sh
git pull
git submodule update --init Hexalith.Builds Hexalith.Commons Hexalith.EventStore Hexalith.Tenants
```

or, equivalently, the non-recursive form (root submodules only):

```sh
git submodule update --init
```

Do **not** run `git submodule update --init --recursive` or
`git submodule update --remote`.
