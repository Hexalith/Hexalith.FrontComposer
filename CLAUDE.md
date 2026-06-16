# Claude Instructions

## Git submodules

This repository's submodules reference each other **circularly**
(`Hexalith.FrontComposer` ↔ `Hexalith.EventStore` ↔ `Hexalith.Tenants` …).
Recursive initialization therefore descends forever and, on Windows, fails with
`Filename too long` once nested paths exceed the 260-character limit.

**Rules:**

- **Never** initialize submodules that are nested inside other submodules.
- Initialize **only** the submodules at the root of the repository.
- **De-initialize** every nested submodule: only the root-level
  submodules may ever be initialized; all others must be de-initialized.
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

**De-initializing nested submodules:**

If a nested submodule (one declared inside another submodule, such as the
`Hexalith.*` entries inside `Hexalith.EventStore` or `Hexalith.Tenants`) ever
gets initialized, de-initialize it. Iterate over the root submodules and
de-initialize all of their nested submodules:

```sh
git submodule foreach 'git submodule deinit --all --force || true'
```

Verify that no nested submodule remains initialized (this should print nothing):

```sh
git submodule foreach --quiet 'git submodule status | grep -v "^-" && echo "STILL INITIALIZED in $name" || true'
```
