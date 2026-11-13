# Dependency governance source projection

The maintenance selection uses these exact committed root dependencies:

| Root dependency | Selected commit |
| --- | --- |
| Hexalith.AI.Tools | `8a3885dd22aea88edd57771cfe592c382df2c583` |
| Hexalith.Builds | `0677db83636bca702d74c212c72f8b08a84616f3` |
| Hexalith.Commons | `1f9ab9afebdaa0f64333b5117e0ac45185fad4a4` |
| Hexalith.EventStore | `15c8ccd525d743f68eed8ff19ba95c6c6bd2e563` |
| Hexalith.Memories | `e19d7a704ef10ba01cbaf141776160e6467dff19` |
| Hexalith.Parties | `b6b4f8a5c83b77ab57a9b947436317162b4fb337` |
| Hexalith.Platform | `fc047fe7ede228d7d356772212c478ccbf36bcdc` |
| Hexalith.PolymorphicSerializations | `e3b455fcdbfdb702c9b74491778f7adaa6ce3625` |
| Hexalith.Tenants | `6188ea48cab9fdf4207072135c66aab5a7b7fe32` |

The current Platform inventory, two unchanged canonical source-file digests, consumer
configuration, root Builds source and root selectors are resealed in
`dependency-graph-policy.json`. Platform remains evidence-only, with only the
existing canonical source pair consumed by the AppHost. All nine repositories
were fetched and synchronized individually; nested checkouts were not moved or
initialized. The Builds catalog and its audit were then committed in the owning
repository.

The 2026-11-11 refresh selects the remote-equal root Builds catalog at
`0677db83636bca702d74c212c72f8b08a84616f3`, including Aspire `13.6.5`,
OpenTelemetry `1.21.0`, Microsoft.NET.Test.Sdk `18.12.0`, and Tenants `7.0.7`.
The existing catalog audit records the exact compatible selection. FrontComposer
imports that catalog without local version overrides. The selected-catalog
policy mirrors now match Localization and TimeProvider.Testing `11.0.0`, Immutable `10.0.15`, and external MCP
ASP.NET Core `2.4.0`.

The active SDK is stable .NET `11.0.100`, while package targets remain `net10.0`
and `netstandard2.0`. Test workflows explicitly install the .NET and ASP.NET Core
`10.0.15` runtimes; the live source-resource lane retains SDK `10.0.302` alongside
the root SDK, plus SDK `10.0.404` for dependencies that pin the 10.0.4xx band.
IDE and hot-reload documentation retains its historical manual
validation dates and does not claim that SDK 11 manual checks have run.
The main three-engine npm workspace pins Playwright `1.64.1` and its matching
core override; the legacy Chromium-only workspace resolves `1.65.0`.
Node 24 type declarations remain selected. The local mutation tool selects
Stryker `5.1.0`; DocFX stays at `2.81.0`.
The Quality default lane uses `--no-artifact-post-processing` to retain one
complete TRX and Cobertura report per test module under SDK 11. Its evidence
gate still requires eight distinct test modules and eight measured coverage
reports; automatic SDK aggregation would add a duplicate summary.

The Parties compatibility repair qualified [Platform v2.1.0](https://github.com/Hexalith/Hexalith.Platform/releases/tag/v2.1.0)
at `b76e0c7c70162653bbff6a8263ebfc4500c1631b` through
[tuple CI](https://github.com/Hexalith/Hexalith.Platform/actions/runs/43462834314),
[catalog CI](https://github.com/Hexalith/Hexalith.Platform/actions/runs/43462834460),
and the existing [source release workflow](https://github.com/Hexalith/Hexalith.Platform/actions/runs/43468621091).
The original retained artifacts independently verify the unchanged 349 legacy
and 412 pre-extension controls, plus 478 current, 97 base and 9 extension
controls, with zero failures, skips or unexecuted cases. The release remains
source-only and grants no additional native, candidate or Story acceptance.

Its matching producer is [Builds v5.2.2](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v5.2.2),
source `02ed624aa19781cac75d2c9112940e9bd830441a`. Its
[CI](https://github.com/Hexalith/Hexalith.Builds/actions/runs/43451348835) and
[normal release](https://github.com/Hexalith/Hexalith.Builds/actions/runs/43455256492)
passed. Both `Hexalith.Builds.Evidence.Cli` and `Hexalith.Builds.Module.Cli`
5.2.2 were independently verified against official NuGet repository signatures,
official SHA512 values and their exact source/version identities; all 354
unsigned publisher payload files match. This is the Platform/Parties producer;
FrontComposer retains its separately qualified NET10-compatible root Builds
catalog and its existing package target frameworks.

Compatibility holds remain explicit:

- The main browser cohort retains `1.64.1`: Firefox bundled with `1.65.0`
  crashes during cross-world native `aria-labelledby` array reflection, including
  the complete WCAG axe scan. Rules, frames, locators, and assertions stay enabled.
  Script-set reflected-name locator support newly added in `1.65.0` remains
  deferred; the older cohort does not provide equivalent coverage of that feature.
  Revisit after a released patched Firefox passes the full matrix and native
  reflection regression; see the [recorded compatibility hold](../docs/accessibility-verification/baseline-change-rationale.md).
- ASP.NET Core authentication/server packages including OpenIdConnect stay at `10.0.15`: their .NET 11
  assets require `net11.0`. Compatible Microsoft.Extensions and System.Text.Json packages
  now use `11.0.0`; SignalR Client/Common retain `10.0.15` because the 11.x netstandard fallback lacks the invocation-binder method needed by the net10.0 Blazor server. Immutable retains `10.0.15` because 11.x conflicts with the generator test reference assemblies.
- Microsoft.Build and Microsoft.Build.Framework stay at `18.9.6`; newer
  inspected versions require unsupported .NET 11 assets in the `net10.0` consumers.
- Microsoft.OpenApi stays at `2.13.0` because ASP.NET Core OpenApi 10 requires
  the 2.x API.
- AngleSharp stays at patched `1.9.1`. The published `1.10.0` fails the Shell
  and Testing suites with a `TypeLoadException`: latest stable bunit `2.13.3`
  wrappers do not implement the new `add_BeforeToggle` member. Upgrade this pin
  when a compatible bunit release passes those suites.

The FrontComposer catalog profile requires the selected external MCP ASP.NET
Core package version `2.4.0` exactly. Other strict package expectations and all
automated build and release gates remain intact.

The push graph gate evaluates both the event base and candidate under the event
base's committed policy. This reseal cannot authorize its own changed source or
selectors. After it lands, a separate later-base activation push must be
evaluated under the landed reseal. Successful local checks do not assert any
hosted CI, release or NuGet publication outcome; those authoritative gates and
publication verification remain required.

Catalog preparation first validates both root mappings and their approved
existing object stores. It acquires only missing exact historical owner commits
from canonical policy-approved repositories before reading their inventories,
using bounded network retries. Acquisition does not move any checkout HEAD,
change working-tree bytes or initialize nested submodules. The seven
inventory-only Platform descendants remain outside global trust and execution.

## Story 2.14 .NET 11 prerequisite candidate

The Story 2.14 prerequisite candidate aligns Shell, Testing, the combined UI and AppHost,
and their source/test/sample consumers with the selected .NET 11 dependency graph.
Shell and Testing now require .NET 11. This drops their .NET 10 consumer target and is a
SemVer-major compatibility change for their next publication. Contracts retains
`net10.0;netstandard2.0`, Contracts.UI retains `net10.0`, Schema retains
`net10.0;netstandard2.0`, and SourceTools retains `netstandard2.0`.
Generated forms select `EditContext.ValidateAsync()` for .NET 11 and retain the supported
synchronous API on earlier target frameworks. Async validation holds command admission,
resumes on the renderer context, and rechecks disposal and scope before dispatch.

This candidate does not change or activate the committed dependency source seal.
The consumer configuration includes project target frameworks, so the owner-controlled
policy/activation transition must qualify the new framework projection before release.
The prior-policy, exact-source CI, package/API compatibility, publication and downstream
consumer-adoption gates remain required. No package publication is authorized by this
prerequisite implementation.

## Activating and replaying this seal

Publish the initial seal commit, containing the complete selected dependency
pointers, consumer configuration, and matching policy, as its own push. The
previous event-base policy still evaluates that push; the new seal cannot
authorize itself. Do not infer release readiness from publishing the seal.

After the seal is remote-visible, create and push a separate documentation-only
activation commit whose immediate parent is that seal. This activation changes
no dependency selector, consumer configuration, or policy byte. Its actual push
event base must be the landed seal, so hosted CI evaluates it under that policy.
The commands below derive the seal from the latest policy-changing committed
revision in the chosen candidate's first-parent history. They do not name an
obsolete selection or treat an uncommitted policy as active.
Run the following while checked out at this activation commit, or assign its
full 40-hex commit to `candidate`. The immediate-parent check prevents replay
against an unrelated later source. Temporary bare object stores do not
initialize any nested checkout.

```bash
set -euo pipefail
candidate="$(git rev-parse HEAD)"
seal="$(git log --first-parent -1 --format=%H "$candidate" -- eng/dependency-graph-policy.json)"

test -n "$seal"
test "$(git rev-parse "${candidate}^")" = "$seal"
git diff --exit-code "$seal" "$candidate" -- eng/dependency-graph-policy.json
objects="$(mktemp -d)"
python3 eng/dependency_graph.py acquire-catalogs --event-base "$seal" --candidate "$candidate"
python3 eng/dependency_graph.py validate --commit "$candidate"
python3 eng/dependency_graph.py acquire --event-base "$seal" --candidate "$candidate" --destination "$objects"
python3 eng/dependency_graph.py --root "$objects" diff --event push --event-base "$seal" --candidate "$candidate"
```

Each command must exit zero; JSON results must report `ok: true`. The graph
diff must report no dependency changes and no affected-module rebuilds. The
hosted push CI must independently pass under the seal's active policy and
produce its run-bound dependency release handoff. Release additionally requires
successful Quality for that exact candidate; local replay alone cannot
substitute for those hosted results or authorize publication.

## Inactive GOV-1 publication-candidate boundary

`eng/publication_candidate.py` implements the local AD-7/AD-19 transfer-data
producer and verifier. `create` snapshots explicitly inventoried regular files,
computes their hashes and sizes, checks the independent package/evidence
inventories, and writes a deterministic, exclusive-create ZIP containing the
closed `hexalith.publication-candidate.v1` descriptor. Its output coordinates
are builder diagnostics; the Actions artifact ID exists only after upload.

`verify` consumes independently authenticated run/artifact coordinates, the
original raw CI handoff, the exact Release evaluator, and the committed active
policy. The CLI revalidates that CI handoff against exact objects. It bounds and
snapshots the raw ZIP, scans the central and local structures, checks ZIP64 and
data descriptors, streams CRC/size/hash verification, and rejects overlap,
encryption, symlinks, undeclared files, compression bombs and destination-name
collisions before materializing fresh regular data files. Every descriptor
asset name is checked against the combined publisher-produced durable names.
Package and symbol identities, canonical versions and unsigned contents are
checked against the independent package inventory; release notes remain bounded
UTF-8 data. The owner supplies the required FR-24 evidence-path array separately
from candidate bytes. These checks do not assess the contents of test/SBOM
evidence or create a final manifest or publication authorization.

The library API requires its caller to authenticate the run, artifact, CI and
policy inputs independently; it performs no network authentication. This helper
has no publication credentials, API writer or attester. Protected integration
requires an owner-accepted Builds split successor, bundling every executable
byte into its pinned owner closure, and AD-12 delayed activation. Existing
Release callers and policy authorizations have not been switched. The Quality
workflow runs its hostile-data fixtures as a deny-only check; this does not
supply the separate trusted dependency-governance or live hostile-credential
proofs. All GOV-1 closure bundles and release/adoption gates remain open.

```bash
python3 eng/publication_candidate.py create --help
python3 eng/publication_candidate.py verify --help
python3 -m unittest tests/eng/test_publication_candidate.py tests/eng/test_dependency_handoff.py
```
