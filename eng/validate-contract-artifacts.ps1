param(
  [string] $PactDir = "tests/Hexalith.FrontComposer.Shell.Tests/Pact",
  [string] $ArtifactDir = "artifacts/contracts",
  [string] $ProviderVerificationReport = "artifacts/contracts/provider-verification.json",
  [switch] $RequireProviderVerification
)

$ErrorActionPreference = "Stop"

# Every repository-relative input resolves against the repository root, not the caller's
# working directory, so the documented command works from anywhere.
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$PactDir = [System.IO.Path]::GetFullPath($PactDir, $repositoryRoot)

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

$expectedPacts = @(
  "frontcomposer-eventstore-command-dispatch.json",
  "frontcomposer-eventstore-query-execution.json",
  "frontcomposer-eventstore-cache-validation.json",
  "frontcomposer-eventstore-auth-tenant-propagation.json"
)

$requiredFiles = $expectedPacts + @(
  "interaction-manifest.json",
  "provider-state-catalog.json",
  "provider-verification-handoff.md"
)

$errors = New-Object System.Collections.Generic.List[string]

foreach ($file in $requiredFiles) {
  $path = Join-Path $PactDir $file
  if (!(Test-Path -LiteralPath $path)) {
    $errors.Add("Missing contract artifact: $file")
    continue
  }

  if ((Get-Item -LiteralPath $path).Length -eq 0) {
    $errors.Add("Empty contract artifact: $file")
  }
}

function Read-Json($path) {
  Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -Depth 64
}

$interactionDescriptions = New-Object System.Collections.Generic.List[string]
$pactInteractionKeys = New-Object System.Collections.Generic.HashSet[string]
$providerStates = New-Object System.Collections.Generic.HashSet[string]

foreach ($file in $expectedPacts) {
  $path = Join-Path $PactDir $file
  if (!(Test-Path -LiteralPath $path)) {
    continue
  }

  $pact = Read-Json $path
  if ($pact.consumer.name -ne "Hexalith.FrontComposer.Shell") {
    $errors.Add("$file has unexpected consumer '$($pact.consumer.name)'")
  }

  if ($pact.provider.name -ne "Hexalith.EventStore") {
    $errors.Add("$file has unexpected provider '$($pact.provider.name)'")
  }

  if ($pact.metadata.pactSpecification.version -ne "4.0") {
    $errors.Add("$file does not declare Pact specification 4.0")
  }

  foreach ($interaction in @($pact.interactions)) {
    $description = [string] $interaction.description
    $stateNames = @($interaction.providerStates | ForEach-Object { [string] $_.name })
    if ($stateNames.Count -ne 1) {
      $errors.Add("$file interaction '$description' must declare exactly one provider state.")
      continue
    }

    $providerState = [string] $stateNames[0]
    $method = [string] $interaction.request.method
    $path = [string] $interaction.request.path
    $interactionDescriptions.Add($description)
    [void] $providerStates.Add($providerState)

    if ([string]::IsNullOrWhiteSpace($description) -or [string]::IsNullOrWhiteSpace($method) -or [string]::IsNullOrWhiteSpace($path)) {
      $errors.Add("$file contains an interaction with a missing description, method, or path.")
    } else {
      [void] $pactInteractionKeys.Add("$description|$providerState|$method|$path")
    }
  }
}

if ($interactionDescriptions.Count -eq 0) {
  $errors.Add("Zero Pact interactions were found.")
}

$duplicates = $interactionDescriptions | Group-Object | Where-Object { $_.Count -gt 1 }
foreach ($duplicate in $duplicates) {
  $errors.Add("Duplicate Pact interaction description: $($duplicate.Name)")
}

$manifestPath = Join-Path $PactDir "interaction-manifest.json"
if (Test-Path -LiteralPath $manifestPath) {
  $manifest = Read-Json $manifestPath
  $manifestPactFiles = @($manifest.pactFiles)
  if ($manifestPactFiles.Count -ne $expectedPacts.Count) {
    $errors.Add("Manifest pactFiles must contain the exact ordered Pact file list.")
  } else {
    for ($index = 0; $index -lt $expectedPacts.Count; $index++) {
      if (![string]::Equals([string] $manifestPactFiles[$index], $expectedPacts[$index], [System.StringComparison]::Ordinal)) {
        $errors.Add("Manifest pactFiles must contain the exact ordered Pact file list.")
        break
      }
    }
  }
  if ([int] $manifest.interactionCount -ne $interactionDescriptions.Count) {
    $errors.Add("Manifest interactionCount $($manifest.interactionCount) does not match pact count $($interactionDescriptions.Count).")
  }

  $manifestInteractionKeys = New-Object System.Collections.Generic.HashSet[string]
  foreach ($entry in @($manifest.interactions)) {
    foreach ($field in @("description", "providerState", "method", "path", "generatedSource", "adapterPath", "owningAcceptanceCriteria", "classifierExpectation")) {
      if ([string]::IsNullOrWhiteSpace([string] $entry.$field)) {
        $errors.Add("Manifest entry '$($entry.description)' is missing $field.")
      }
    }

    $key = "$($entry.description)|$($entry.providerState)|$($entry.method)|$($entry.path)"
    if (!$manifestInteractionKeys.Add($key)) {
      $errors.Add("Duplicate manifest interaction: $key")
    }
  }

  foreach ($key in $pactInteractionKeys) {
    if (!$manifestInteractionKeys.Contains($key)) {
      $errors.Add("Pact interaction missing from manifest: $key")
    }
  }

  foreach ($key in $manifestInteractionKeys) {
    if (!$pactInteractionKeys.Contains($key)) {
      $errors.Add("Manifest interaction missing from pact files: $key")
    }
  }
}

$catalogPath = Join-Path $PactDir "provider-state-catalog.json"
if (Test-Path -LiteralPath $catalogPath) {
  $catalog = Read-Json $catalogPath
  $catalogStates = New-Object System.Collections.Generic.HashSet[string]
  foreach ($state in @($catalog.states)) {
    [void] $catalogStates.Add([string] $state.name)
    foreach ($field in @("setup", "teardown", "seededTenant", "seededUser", "seededAggregateId", "expectedResult", "owningRepository", "testOnlySeam")) {
      if ([string]::IsNullOrWhiteSpace([string] $state.$field)) {
        $errors.Add("Provider state '$($state.name)' is missing $field.")
      }
    }
  }

  foreach ($state in $providerStates) {
    if (!$catalogStates.Contains($state)) {
      $errors.Add("Provider state '$state' is used by a pact but missing from provider-state-catalog.json.")
    }
  }
}

function Find-RedactionLeaks([string] $Text) {
  $leaks = New-Object System.Collections.Generic.List[string]
  $normalized = [regex]::Replace(
    $Text,
    '(?i)["'']authorization["'']\s*:\s*["'']Bearer FC_CONTRACT_TOKEN["'']',
    '"ALLOWLISTED_HEADER":"ALLOWLISTED_SYNTHETIC_TOKEN"')
  $lower = $normalized.ToLowerInvariant()

  foreach ($fragment in @("access_token=", "api_key=", "authorization_payload", "connectionstring", "password=")) {
    if ($lower.Contains($fragment)) {
      $leaks.Add($fragment)
    }
  }

  # The cookie rule is a key shape, not the bare substring: "cookies": [...] and
  # "cookieHeader" must fail while an ordinary source path such as
  # FrontComposerAuthCookieOptions.cs must not.
  foreach ($secretKey in @("access[_-]?token", "client[_-]?secret", "private[_-]?key", "sas[_-]?token", "api[_-]?key", "password", "cookie[A-Za-z0-9_.\-]*")) {
    if ([regex]::IsMatch($normalized, '"?' + $secretKey + '"?\s*[=:]', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
      $leaks.Add("quoted secret key: $secretKey")
    }
  }

  if ([regex]::IsMatch($normalized, '(?:["'']authorization["'']|(?<![A-Za-z0-9_])authorization(?![A-Za-z0-9_]))\s*:(?!\s*\{)\s*', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
    $leaks.Add("raw Authorization header")
  }

  if ([regex]::IsMatch($normalized, 'Bearer\s+[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
    $leaks.Add("jwt bearer token")
  }

  if ([regex]::IsMatch($normalized, '[A-Za-z]:(?:\\)+Users(?:\\)+[^\\]+(?:\\)+', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
    $leaks.Add("local user path")
  }

  if ([regex]::IsMatch($normalized, '[A-Z0-9_]{8,}=.{6,}')) {
    $leaks.Add("environment-shaped secret")
  }

  # Prose artifacts such as provider-verification-handoff.md are published too, so they are
  # scanned with the same grammar; only path-shaped matches are excluded, never whole files.
  if ($true) {
    $sha256Fields = @(
    "byLocationDigest", "catalog_sha256", "certificate_sha256", "contractsInventorySha256", "decisionRecordSha256",
    "evidenceManifestSha256", "observedReleaseInventorySha256", "oi18SubjectSha256", "policySha256", "programSha256",
    "projectSha256", "releaseInventorySha256", "release_inventory_sha256", "rosterSha256",
    "runner_sha256_at_catalog_commit", "runtimeInputTreeSha256", "schema_sha256_at_catalog_commit",
    "sha256", "subjectSha256", "subject_sha256", "testInventorySha256", "treeSha256",
    "validator_sha256", "workflow_sha256_at_source")
    $knownSha256 = ($sha256Fields | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $encodedTokenScan = [regex]::Replace(
      $normalized,
      '"(?:' + $knownSha256 + ')"\s*:\s*"[0-9a-fA-F]{64}"',
      '"ALLOWLISTED_HASH_FIELD":"ALLOWLISTED_SHA256"',
      [System.Text.RegularExpressions.RegexOptions]::None)
    $encodedTokenScan = [regex]::Replace(
      $encodedTokenScan,
      '"(?:contentHashSha512|nupkgSha512)"\s*:\s*"[A-Za-z0-9+/]{86}=="',
      '"ALLOWLISTED_HASH_FIELD":"ALLOWLISTED_SHA512"',
      [System.Text.RegularExpressions.RegexOptions]::None)
    # A repository path segment is not a credential: exclude matches that touch a path
    # separator or a dot rather than skipping the artifact.
    foreach ($match in [regex]::Matches($encodedTokenScan, '(?<![A-Za-z0-9+/_.\\-])[A-Za-z0-9+/_-]{64,}={0,2}(?![A-Za-z0-9+/_=.\\-])', [System.Text.RegularExpressions.RegexOptions]::None)) {
      $leaks.Add("encoded token-like payload")
      break
    }
  }

  return $leaks
}

$redactionLines = New-Object System.Collections.Generic.List[string]
foreach ($file in $requiredFiles) {
  $path = Join-Path $PactDir $file
  if (!(Test-Path -LiteralPath $path)) {
    continue
  }

  $text = Get-Content -LiteralPath $path -Raw
  $leaks = Find-RedactionLeaks $text
  if ($leaks.Count -gt 0) {
    $errors.Add("Redaction scan failed for ${file}: $($leaks -join ', ')")
  } else {
    $redactionLines.Add("${file}: clean")
  }
}

$redactionLines | Set-Content -LiteralPath (Join-Path $ArtifactDir "redaction-scan.txt") -Encoding utf8

$providerStatus = "NOT_REQUIRED"
if ($RequireProviderVerification) {
  # The EventStore provider application exits nonzero on any failed interaction; this check
  # only refuses a missing, empty, or leaking report before it is published as a CI artifact.
  $providerStatus = "REQUIRED_REJECTED"
  $resolvedProviderVerificationReport = [System.IO.Path]::GetFullPath($ProviderVerificationReport, $repositoryRoot)
  if (!(Test-Path -LiteralPath $resolvedProviderVerificationReport)) {
    $errors.Add("Provider verification is required but '$resolvedProviderVerificationReport' was not found.")
  } elseif ((Get-Item -LiteralPath $resolvedProviderVerificationReport).Length -eq 0) {
    $errors.Add("Provider verification report is empty: $resolvedProviderVerificationReport")
  } else {
    $providerLeaks = Find-RedactionLeaks (Get-Content -LiteralPath $resolvedProviderVerificationReport -Raw)
    if ($providerLeaks.Count -gt 0) {
      $errors.Add("Redaction scan failed for provider verification report: $($providerLeaks -join ', ')")
    } else {
      $providerStatus = "PRESENT_AND_CLEAN"
    }
  }
}

$summary = @"
## Contract Validation

- Pact files: $($expectedPacts -join ', ')
- Interaction count: $($interactionDescriptions.Count)
- Provider verification report: $providerStatus
- Pact specification: 4.0
- PactNet package: 5.0.1
- Manifest: tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json
- Provider states: tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json
- Redaction scan: $(if ($errors.Count -eq 0) { "clean" } else { "failed" })
- Submodules: root-level checkout only; no recursive nested submodule command is used by this lane
"@
$summary | Set-Content -LiteralPath (Join-Path $ArtifactDir "job-summary.md") -Encoding utf8

if ($errors.Count -gt 0) {
  $errors | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation-errors.txt") -Encoding utf8
  $errors | ForEach-Object { Write-Error $_ }
  exit 1
}

"Contract artifacts validated successfully." | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation.txt") -Encoding utf8
Write-Host "Contract artifacts validated successfully."
