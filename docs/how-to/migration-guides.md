---
title: "Apply migration guidance"
description: "Use migration-guide requirements when FrontComposer APIs or shipped skill corpus examples change."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-07-11
uid: frontcomposer.how-to.migration-guides
slug: how-to/migration-guides/
fromVersion: "9.1.0"
toVersion: "9.2.0"
diagnosticId: "HFCM9001"
skillCorpusImpact: "review-required"
codeFixAvailable: false
---

# Apply migration guidance

Migration pages are required when a shipped skill corpus example breaks, even when the semantic-version bucket is minor.

Old code:

```csharp no-compile reason="Obsolete API shown only as migration input."
services.AddFrontComposerDebugOverlay();
```

New code:

```csharp no-compile reason="Host setup snippet requires the application service collection."
services.AddFrontComposerDevMode();
```

Why it changed: the CLI migration contract moved migration reports to dry-run/apply semantics, and the HFC diagnostic governance contract reserves the related rows.

Affected packages: `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`.

The `9.1.0` to `9.2.0` edge is `cli-supported`: the CLI-only `HFCM9001` safe fix is an allowlisted
syntactic rename of `IdentifierNameSyntax` nodes named `AddFrontComposerDebugOverlay` to
`AddFrontComposerDevMode`, excluding identifiers inside `nameof`. This is not semantic validation:
it can rename same-named non-API identifiers, including variable references and method groups.
Review the diff before applying it. No IDE analyzer/code fix is published (`codeFixAvailable: false`).
`HFCM9002` remains synthetic/manual-only; production emission is not approved and normal builds do
not produce those migration sidecars.

For customization diagnostics, follow the [Customization prerequisites](../reference/components/customization.md)
and [customization cookbook](../how-to/customization-gradient-cookbook.md). The Shell assembly itself
must be compiled with `DEBUG`, and DI must contain an `IHostEnvironment` explicitly identifying
Development. Register logging and the environment before `AddFrontComposerDevMode()`; the
parameterless overload recognizes instance/factory environment registrations, or use the explicit
environment overload while keeping `IHostEnvironment` registered in DI. The published Release
NuGet package does not emit these customization diagnostics/panels, even for a Debug adopter in
Development.

Use the [classified migration index](../migrations/index.md) to select an exact edge. A
`manual-only` package/API guide does not enable CLI execution of that edge. Explicit versions
are required; the CLI does not infer an upgrade chain.

```shell
dotnet tool run frontcomposer migrate --project Acme.Orders.csproj --from 9.1.0 --to 9.2.0 --dry-run --format json
```

Dry-run is the default and writes nothing. Review the `frontcomposer.cli.migrate.v1` report,
then replace `--dry-run` with `--apply` to write eligible source changes. Each file is replaced
atomically through a same-directory temporary file after revalidating its path and source hash.
This is per-file atomicity: earlier successful writes remain after a later failure or cancellation;
there is no whole-run rollback. Handled write failures return code 4 and set `applied=false` on
emitted reports. Cancellation can return code 4 before any report is emitted; interrupted output
may leave an absent or partial report. Do not require an `applied` field when no complete report
was emitted.

Migration sidecars support arrays of diagnostic objects or objects containing a `diagnostics`
array of objects. Unsupported valid JSON shapes, such as scalar roots or non-object diagnostic
entries, may throw outside handled failures without a structured report or the documented
exit-code guarantees.

Generated output, `bin`, `obj`, `.git`, package caches, submodules, paths outside the project,
file symlinks, and linked ancestor directories (including junctions) below the selected project
directory are ineligible. Explicit `Compile` Link metadata does not authorize an outside-root or
linked write. The planner refuses
unsafe documents before source reads and rechecks safety at apply time. Complex MSBuild globs
and imported Compile items are not fully evaluated. Source reads support UTF-8 (with or without
BOM), UTF-16 LE/BE, and UTF-32 LE/BE; unknown encodings and files over 16 MiB are refused.

For inspection without migration, use `inspect --format json` and its
`frontcomposer.cli.inspect.v1` contract. See [CLI reference](../reference/cli.md) for generated
families, ordering, filters, sidecar handling, bounded output, and path opt-in. Sidecars support
arrays of diagnostic objects or objects containing a `diagnostics` array of objects. Handled parse
and read failures produce sanitized findings; unsupported valid JSON shapes, such as scalar roots
or non-object diagnostic entries, may throw outside handled failures without a structured report
or the documented exit-code guarantees.

Skill-corpus evidence: `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` remains the producer input.

Related links: [CLI inspect and migrate](../reference/cli.md), [diagnostics](../reference/diagnostics/index.md), and [executable migration guide](../migrations/9.1-to-9.2.md).

For the Contracts/Contracts.UI package split, runtime/testing ownership moves, and composed-query
migration, use the [FrontComposer 1.12 to 2.0 guide](../migrations/1.12-to-2.0.md). That major-version
guide is manual: HFC0001 identifies flattened query usage, while package/namespace moves require an
explicit reference and source audit.
