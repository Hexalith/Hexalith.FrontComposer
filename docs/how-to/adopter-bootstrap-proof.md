---
title: "Prove the three-call adopter bootstrap"
description: "Run a package-based generated projection and command proof against an exact FrontComposer candidate."
genre: how-to
audience: adopter
ownerStory: 12-1-i-adopt-kit-1-publish-a-deterministic-three-call-adopter-pro
status: published
reviewed: 2026-09-23
uid: frontcomposer.how-to.adopter-bootstrap-proof
slug: how-to/adopter-bootstrap-proof/
---

# Prove the three-call adopter bootstrap

The selected external maintainer owns the independent run and its dated result. Hexalith.Tenants is the selected maintainer until a dated Product decision selects a substitute. The Tenants maintainer records external acceptance at `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md`, the PRD's G-6/SM-1 evidence path. If Product makes the dated D-7 decision to select Hexalith.Parties, the Parties maintainer records equivalently candidate-bound Parties evidence for the same gate. Running this fixture inside FrontComposer is useful validation, but does not close EXT-ADOPTER-1.

## Prepare the candidate

Use an exact, full 40-character FrontComposer Git SHA and a directory containing these **four `.nupkg` files at one exact version**: `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.Contracts.UI`, `Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer.SourceTools`. The package repository metadata must identify that SHA for every package. The runner checks each package's ID, version, repository URL, and repository commit before it builds or starts the fixture. A version label or historical package cannot establish candidate provenance by itself.

Install the .NET 10 SDK and matching `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` runtime patch versions. Record the chosen runtime version from `dotnet --list-runtimes`. Supply an EventStore base endpoint for the real command/query registration; this render-only proof does not require that endpoint to respond. The proof only renders the generated surfaces; it does not send a command or save a domain payload.

Copy `samples/AdopterProofKit/v1` and `eng/adopter_proof.py` to a clean consumer checkout, preserving their relative layout (`eng/` beside `samples/`). Use candidate packages from a local feed directory. The script restores the fixture in a temporary directory with a fresh NuGet cache, so prior packages cannot satisfy the run.

```bash
python3 eng/adopter_proof.py \
  --candidate-sha 0123456789abcdef0123456789abcdef01234567 \
  --package-version 12.1.0-candidate \
  --package-source /path/to/candidate-packages \
  --runtime-version 10.0.12 \
  --eventstore-endpoint https://eventstore.example.test \
  --result adopter-proof-result.json
```

Replace every illustrative value with the selected candidate's actual inputs. The command exits nonzero on provenance, runtime, build, startup, or render failure. Inspect the result's `failure` code before retrying; it is a named category and contains no endpoint, token, stack trace, payload, tenant, user, or machine path.

The fixture's host calls `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<ProofDomain>()` → `AddHexalithEventStore(...)`. Its domain annotations generate `ProofProjectionView` and the `/commands/Proof/CreateProofCommand` page. The fixture mounts the generated view at the module's `/proof/proof-projection` navigation route. The runner checks both rendered HTML surfaces, then checks that missing Quickstart and misordered EventStore fail with named startup diagnostics before any page renders. It also starts Quickstart alone and checks the existing no-modules home state. The host retains the EventStore command and query registrations throughout the success case.

The JSON result contains the execution date, full candidate SHA, verified package IDs and version, a Base64-encoded SHA-512 archive hash for each exact `.nupkg` under `package_identity.archive_sha512`, SDK and shared-runtime identity, named projection and command surfaces, four explicit assertions, and pass/fail. Each hash covers the complete package archive that the runner checked and restored, so a reviewer can compare the published result with the four candidate archives. The hash map is empty and `verified` is false if package verification did not complete. Publish only that JSON result with the candidate reference. Keep raw host logs local because they may contain operational details. A passing result from the selected external maintainer can be reviewed for EXT-ADOPTER-1; an internal fixture pass alone is supporting evidence.

To verify the published archive hashes independently, use the same package directory and version identified by the result:

```bash
python3 - adopter-proof-result.json /path/to/candidate-packages <<'PY'
import base64
import hashlib
import json
from pathlib import Path
import sys

result = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
identity = result["package_identity"]
assert identity["verified"] is True
assert len(identity["ids"]) == 4
assert set(identity["archive_sha512"]) == set(identity["ids"])
for package_id, expected in identity["archive_sha512"].items():
    archive = Path(sys.argv[2]) / f"{package_id}.{identity['version']}.nupkg"
    actual = base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode("ascii")
    assert actual == expected, package_id
print("All candidate archive hashes match.")
PY
```

Focused existing regressions complement this host proof. On 2026-09-23, `FrontComposerBootstrapGuardTests` passed 22 tests for ordering and named diagnostics; `FrontComposerServiceGraphTests` passed 21 for DI lifetimes and EventStore replacement; `Story11BootstrapShellRenderTests` passed 4 for the empty shell; `PackagedAnalyzerConsumerTests` passed 1 for generated package compilation; and the Shell authentication extension and redaction suites passed 13 and 10 respectively. The route-contract e2e spec covers a generated command route and focus, but was not run as part of this kit validation. These internal results do not substitute for the dated external run.
