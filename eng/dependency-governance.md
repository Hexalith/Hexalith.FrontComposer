# Dependency governance source projection

The maintenance selection uses these exact committed root dependencies:

| Root dependency | Selected commit |
| --- | --- |
| Hexalith.AI.Tools | `1745b6663ad32bb1288167135ed0f979366c53a0` |
| Hexalith.Builds | `0577f0c2d70bd4c2999aa3d98a705c83919bfc00` |
| Hexalith.Commons | `11555ace9efcae2928a00122dcba91243e5a8a1a` |
| Hexalith.EventStore | `0f22b3ed86a2ecc4a98f09b4cc76bc5a9bba38c9` |
| Hexalith.Memories | `246157a7ff8e2dcd8d356c833a77a02514490d2f` |
| Hexalith.Parties | `1308ad000c79ebc394fc395588f9af91092e8c4c` |
| Hexalith.Platform | `c1997c60f31e08d945b44cb7aa2afd7800e8ff69` |
| Hexalith.PolymorphicSerializations | `bce4eb9f45c6b131b960ab4fa39e50ff01e51960` |
| Hexalith.Tenants | `cf36618a2c27b5722a1ff811cfbbce4528c3c105` |

The matching Platform inventory, two canonical source-file digests, consumer
configuration, root Builds source and root selectors are sealed in
`dependency-graph-policy.json`. Platform remains evidence-only, with only the
existing canonical source pair consumed by the AppHost. All nine repositories
were fetched and synchronized individually; nested checkouts were not moved or
initialized. The Builds catalog and its audit were then committed in the owning
repository.

The 2026-11-06 registry refresh selects MediatR `14.3.0`,
Microsoft.Azure.Cosmos `3.63.4`, Microsoft.Identity.Web `4.18.0`,
OpenTelemetry.Instrumentation.Http `1.20.2`, Radzen.Blazor `12.1.12` and the
existing StyleCop beta channel at `1.3.171-beta`. The Builds audit binds catalog
commit `b4ae8e36a88ca48159b8949a0c8131871825e2d6` and refreshes the seven
affected families; its deterministic validation passes for 316 packages. The
13 Hexalith version properties and their 65 package IDs already match the
current compatible public versions. Both npm test workspaces select Playwright
`1.65.0`; Node 24 type declarations remain selected.

The new analyzer also requires multiline collection-expression layout fixes in
Builds. Those source changes contain whitespace only, and the complete Builds
Release solution build passes with all analyzer gates enabled. The Shell test
cleanup helpers poll the existing bunit invocation list by identifier to avoid
concurrent dictionary enumeration while preserving exactly-once checks.

Compatibility holds remain explicit:

- The SDK stays at stable .NET `10.0.403`.
- Microsoft.Build and Microsoft.Build.Framework stay at `18.9.6`; the newer
  inspected versions require unsupported .NET 11 assets.
- Microsoft.OpenApi stays at `2.13.0` because ASP.NET Core OpenApi 10 requires
  the 2.x API.
- AngleSharp stays at patched `1.9.1`. The published `1.10.0` fails the Shell
  and Testing suites with a `TypeLoadException`: latest stable bunit `2.13.3`
  wrappers do not implement the new `add_BeforeToggle` member. Upgrade this pin
  when a compatible bunit release passes those suites.

The FrontComposer catalog profile requires the selected external MCP ASP.NET
Core package version `2.3.1` exactly. Other strict package expectations and all
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

## Activating and replaying this seal

The maintenance seal is commit
`8a51e0a1891f08bbfb51e168b69f766c787b1f46`. Exact committed-source
validation of that revision passed. This documentation-only activation must
be pushed separately, with the seal as its actual push event base. It changes
no dependency selector, consumer configuration or policy byte.

Run the following while checked out at this activation commit, or assign its
full 40-hex commit to `candidate`. The immediate-parent check prevents replay
against an unrelated later source. Temporary bare object stores do not
initialize any nested checkout.

```bash
seal=8a51e0a1891f08bbfb51e168b69f766c787b1f46
candidate="$(git rev-parse HEAD)"
test "$(git rev-parse "${candidate}^")" = "$seal"
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
