# Agent Instructions

## AI assistant instructions

Before working in this repository, read
[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
(in the `Hexalith.AI.Tools` submodule) and follow it.

## Git submodules

This repository's submodules reference each other **circularly**
(`Hexalith.FrontComposer` ↔ `Hexalith.EventStore` ↔ `Hexalith.Tenants` …).
Recursive initialization therefore descends forever and, on Windows, fails with
`Filename too long` once nested paths exceed the 260-character limit.

**Rules:**

- **Never** initialize submodules that are nested inside other submodules.
- Initialize **only** the submodules declared by the root `.gitmodules` file
  under `references/`.
- **De-initialize** every nested submodule: only the root-declared
  `references/Hexalith.*` submodules may ever be initialized; all others must
  be de-initialized.
- **Never** use `--recursive` when running `git submodule update`.
- **Never** use `--remote` (it moves submodules off their pinned commits).

Do **not** run `git submodule update --init --recursive` or
`git submodule update --remote`.

**De-initializing nested submodules:**

If a nested submodule (one declared inside another submodule, such as the
`Hexalith.*` entries inside `references/Hexalith.EventStore` or
`references/Hexalith.Tenants`) ever gets initialized, de-initialize it. Iterate
over the root-declared submodules and de-initialize all of their nested
submodules:

```sh
git submodule foreach 'git submodule deinit --all --force || true'
```

Verify that no nested submodule remains initialized (this should print nothing):

```sh
git submodule foreach --quiet 'git submodule status | grep -v "^-" && echo "STILL INITIALIZED in $name" || true'
```
