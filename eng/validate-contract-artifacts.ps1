param(
  [string] $PactDir = "tests/Hexalith.FrontComposer.Shell.Tests/Pact",
  [string] $ArtifactDir = "artifacts/contracts",
  [string] $ProviderVerificationReport = "",
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
  $normalized = $Text.Replace("FC_CONTRACT_TOKEN", "ALLOWLISTED_SYNTHETIC_TOKEN")
  $lower = $normalized.ToLowerInvariant()

  foreach ($fragment in @("access_token=", "api_key=", "authorization_payload", "connectionstring", "cookie", "password=", "set-cookie")) {
    if ($lower.Contains($fragment)) {
      $leaks.Add($fragment)
    }
  }

  if (([regex]::IsMatch($normalized, '"authorization"\s*:', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase) `
      -or [regex]::IsMatch($normalized, '\bauthorization\s*:', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) `
      -and !$Text.Contains("Bearer FC_CONTRACT_TOKEN")) {
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

  # A 64-hex value is safe only where the document identifies it as a checksum/hash.
  # Never allowlist the value globally: the same bytes in a token/secret field must fail.
  $encodedTokenScan = [regex]::Replace(
    $normalized,
    '"[^"\r\n]*(?:sha|hash|checksum|digest|fingerprint)[^"\r\n]*"\s*:\s*"[0-9a-f]{64}"',
    '"ALLOWLISTED_HASH_FIELD":"ALLOWLISTED_SHA256"',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  if ([regex]::IsMatch($encodedTokenScan, '[A-Za-z0-9+/]{64,}={0,2}', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
    $leaks.Add("encoded token-like payload")
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

$frontComposerEvidenceRoot = Join-Path $repositoryRoot "_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24"
$liveEvidenceRoot = Join-Path $repositoryRoot "_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation"
$expectedProviderVerificationReport = Join-Path $liveEvidenceRoot "provider-verification.json"
$providerStatus = "NOT_REQUIRED"
$historicalStatus = "NOT_REQUIRED"
$appHostStatus = "NOT_REQUIRED"
if ($RequireProviderVerification) {
  # Required-and-rejected must never be summarized as required-and-absent. The frozen
  # Story 11.24 archive and the current compatibility lane have independent hash authority.
  $providerStatus = "REQUIRED_REJECTED"
  $historicalStatus = "REQUIRED_REJECTED"
  $appHostStatus = "REQUIRED_REJECTED"
  if ([string]::IsNullOrWhiteSpace($ProviderVerificationReport)) {
    $ProviderVerificationReport = $expectedProviderVerificationReport
  }

  # Resolve a relative argument against the repository root, not the caller's working directory,
  # so a correct relative path is accepted from anywhere.
  $resolvedProviderVerificationReport = [System.IO.Path]::GetFullPath($ProviderVerificationReport, $repositoryRoot)
  $resolvedExpectedProviderVerificationReport = [System.IO.Path]::GetFullPath($expectedProviderVerificationReport)
  if (![string]::Equals(
      $resolvedProviderVerificationReport,
      $resolvedExpectedProviderVerificationReport,
      [System.StringComparison]::Ordinal)) {
    $errors.Add("Provider verification must use the FrontComposer-owned report: $expectedProviderVerificationReport")
  } elseif (!(Test-Path -LiteralPath $resolvedProviderVerificationReport)) {
    $errors.Add("Provider verification is required for this lane but '$resolvedProviderVerificationReport' was not found.")
  } elseif ((Get-Item -LiteralPath $resolvedProviderVerificationReport).Length -eq 0) {
    $errors.Add("Provider verification report is empty: $resolvedProviderVerificationReport")
  } else {
    $providerText = Get-Content -LiteralPath $resolvedProviderVerificationReport -Raw
    $providerLeaks = Find-RedactionLeaks $providerText
    if ($providerLeaks.Count -gt 0) {
      $errors.Add("Redaction scan failed for provider verification report: $($providerLeaks -join ', ')")
    }

    $validator = Join-Path $repositoryRoot "eng/eventstore_runtime_evidence.py"
    $validationOutput = @(& python3 $validator `
      --evidence-root $frontComposerEvidenceRoot `
      --live-evidence-root $liveEvidenceRoot `
      --pact-dir $PactDir `
      --repository-root $repositoryRoot 2>&1)
    if ($LASTEXITCODE -ne 0) {
      foreach ($line in $validationOutput) {
        $errors.Add([string] $line)
      }
    } elseif ($providerLeaks.Count -eq 0) {
      # A leaking report is a rejected lane; the summary must never call it complete.
      $historicalStatus = "IMMUTABLE_ARCHIVE_VALID"
      $providerStatus = "CURRENT_PROVIDER_PASSED"
      $appHostStatus = "AUTHENTICATED_APPHOST_PASSED"
    }
  }
}

$summary = @"
## Contract Evidence

- Pact files: $($expectedPacts -join ', ')
- Interaction count: $($interactionDescriptions.Count)
- Historical Story 11.24 integrity: $historicalStatus
- Current provider verification: $providerStatus
- Current authenticated AppHost smoke: $appHostStatus
- Pact specification: 4.0
- PactNet package: 5.0.1
- Manifest: tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json
- Provider states: tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json
- Redaction scan: $(if ($errors.Count -eq 0) { "clean" } else { "failed" })
- Submodules: root-level checkout only; no recursive nested submodule command is used by this lane
- Provider verification required in this lane: $RequireProviderVerification
- Compatibility verdict: preserved as evidence and does not authorize or revoke the owner-approved runtime identity
"@
$summary | Set-Content -LiteralPath (Join-Path $ArtifactDir "job-summary.md") -Encoding utf8

if ($errors.Count -gt 0) {
  $errors | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation-errors.txt") -Encoding utf8
  $errors | ForEach-Object { Write-Error $_ }
  exit 1
}

"Contract artifacts validated successfully." | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation.txt") -Encoding utf8
Write-Host "Contract artifacts validated successfully."
