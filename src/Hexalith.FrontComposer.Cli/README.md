# Hexalith FrontComposer CLI

`frontcomposer` is the command-line inspection and migration tool for Hexalith FrontComposer.

Use `frontcomposer inspect` to view generated source output under the deterministic
`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer` contract, and
`frontcomposer migrate --from <version> --to <version>` to preview or apply allowlisted
FrontComposer migration fixes.

Dry-run is the default migration mode. Source-writing migrations require `--apply`.

## Exit Codes and Fail Flags

| Code | Meaning |
| --- | --- |
| `0` | Command completed successfully. |
| `1` | `--fail-on-findings`, `--fail-on-warning`, or `--fail-on-error` intentionally promoted findings to a non-zero result. |
| `2` | Invalid, unsupported, or ambiguous input such as multiple discovered projects, `.slnx`, `.fsproj`, malformed `.sln` project entries, or an unsupported migration edge. |
| `3` | `inspect` could not find generated output and suggests building the selected project. |
| `4` | A filesystem, cancellation, workspace setup, or apply/write failure prevented a clean result. |

`inspect --fail-on-error` returns code `1` when any emitted diagnostic is an error.
`inspect --fail-on-warning` is stricter and returns code `1` when any warning or error is
emitted. When both flags are supplied, the warning-or-error rule wins. JSON output includes
the same warning/error counts under `summary`; consumers should use the exit code as the
applied fail behavior.

## Migration Output Notes

`migrate --format json` reports source paths as selected-project-relative paths whenever the CLI can
identify a project document. Apply-time failures use the planned source file's project-relative path.
Early planning failures that occur before any source document is identified, such as workspace
initialization failures, report the selected project file name instead.

`migrate --format json` emits:

- `schemaVersion`: `frontcomposer.cli.migrate.v1`.
- `applied`: `true` only when `--apply` ran to completion and every planned write succeeded.
- `summary`: `changed`, `unchanged`, `skipped`, `failed`, `manualOnly`, and `conflicts` counts.
- `entries[]`: `diagnosticId`, `kind`, project-relative `path`, bounded/redacted `what`,
  `expected`, `got`, `fix`, `docsLink`, terminal-safe informational `diff`, and
  `formattingApplied`.

Unified diffs are terminal/log-safe informational diffs, not a patch-applicability contract.
Control characters and long hunks are escaped or truncated before text and JSON output.

The migration planner reads explicit `<Compile Include="...">` project items and the SDK-style
default `**/*.cs` shape used by FrontComposer fixtures. More complex MSBuild glob semantics and item
transforms are intentionally conservative in Story 9-2. Project files that contain top-level
`<Import>` elements produce a warning because imported `Compile` items are not evaluated by the CLI;
files that are not resolved as explicit project documents are not migrated.

Project selection precedence is deterministic: explicit `--project` wins, explicit `--solution`
may select exactly one `.csproj`, and a current directory with exactly one `.csproj` is allowed.
Ambiguous discovery, `.slnx`, `.fsproj`, malformed `.sln` project entries, and unsupported
solution project types fail closed with sanitized guidance.

Migration reads UTF-8, UTF-8 with BOM, UTF-16 LE/BE, and UTF-32 LE/BE source files. Unknown byte
encodings fail closed rather than replacing invalid bytes, and files larger than 16 MiB are refused
before decoding. The `formattingApplied` field is reserved for future formatter-backed edges and is
currently always `false`.

Apply mode writes each changed source file through a same-directory temporary file before replacing
the original target. If the process is interrupted before replacement, the original source remains in
place and the temporary file is cleaned up on a best-effort basis.

## Manual-Only Migration Diagnostics (HFCM9002)

Manual-only migration diagnostics (HFCM9002) are detected by reading
`*.diagnostics.json` sidecar files written by the SourceTools generator under
`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`. **In Story 9-2 the
sidecar reader is wired to the synthetic test fixture only — there is no
production SourceTools emitter that writes these sidecars yet. AC11 ("a migration
diagnostic has no safe automated fix") fires today only against hand-crafted
fixtures.** Story 9-4 owns the final HFC ID assignment and will add the real
generator emitter so AC11 fires for adopter code.

Sidecar files that fail to parse or read are surfaced as a single sentinel
manual-only entry per file rather than being silently dropped, so corrupted
sidecars are visible in the migration output.

Sidecar `path` values must normalize to project-relative source paths. Drive-relative paths
(`C:foo.cs`), URI-shaped paths, traversal, and paths outside the selected project are reported as
sentinel manual-only entries under `__sidecar__/...` instead of being trusted.

On SDKs that provide `dnx`, `dnx frontcomposer ...` can be used as a convenience after the package
is available from a feed. Local tool manifests and `dotnet tool install` remain the primary
installation paths.
