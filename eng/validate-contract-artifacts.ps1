param(
  [string] $PactDir = "tests/Hexalith.FrontComposer.Shell.Tests/Pact",
  [string] $ArtifactDir = "artifacts/contracts",
  [string] $ProviderVerificationReport = "",
  [switch] $RequireProviderVerification
)

$ErrorActionPreference = "Stop"

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

  if ([regex]::IsMatch($normalized, '[A-Za-z0-9+/]{64,}={0,2}', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
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

if ([string]::IsNullOrWhiteSpace($ProviderVerificationReport)) {
  $ProviderVerificationReport = Join-Path $ArtifactDir "provider-verification.json"
}

$providerStatus = "BLOCKED_HANDOFF"
if ($RequireProviderVerification) {
  if (!(Test-Path -LiteralPath $ProviderVerificationReport)) {
    $errors.Add("Provider verification is required for this lane but '$ProviderVerificationReport' was not found.")
  } elseif ((Get-Item -LiteralPath $ProviderVerificationReport).Length -eq 0) {
    $errors.Add("Provider verification report is empty: $ProviderVerificationReport")
  } else {
    $providerStatus = "VERIFICATION_REPORT_PRESENT"
    $providerText = Get-Content -LiteralPath $ProviderVerificationReport -Raw
    $providerLeaks = Find-RedactionLeaks $providerText
    if ($providerLeaks.Count -gt 0) {
      $errors.Add("Redaction scan failed for provider verification report: $($providerLeaks -join ', ')")
    }
  }
}

if (!$RequireProviderVerification) {
  $providerReport = @"
Provider verification result: BLOCKED_HANDOFF
Provider: Hexalith.EventStore
Reason: deterministic provider-state setup, teardown, TCP startup, health probe, port isolation, and stale-process detection must run beside the EventStore provider host.
Handoff: tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md
"@
  $providerReport | Set-Content -LiteralPath (Join-Path $ArtifactDir "provider-verification-blocked.txt") -Encoding utf8
}

$summary = @"
## Contract Evidence

- Pact files: $($expectedPacts -join ', ')
- Interaction count: $($interactionDescriptions.Count)
- Provider verification: $providerStatus
- Pact specification: 4.0
- PactNet package: 5.0.1
- Manifest: tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json
- Provider states: tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json
- Redaction scan: $(if ($errors.Count -eq 0) { "clean" } else { "failed" })
- Submodules: root-level checkout only; no recursive nested submodule command is used by this lane
- Provider verification required in this lane: $RequireProviderVerification
- Release status: blocked unless pacts verify against the pinned EventStore provider version
"@
$summary | Set-Content -LiteralPath (Join-Path $ArtifactDir "job-summary.md") -Encoding utf8

if ($errors.Count -gt 0) {
  $errors | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation-errors.txt") -Encoding utf8
  $errors | ForEach-Object { Write-Error $_ }
  exit 1
}

"Contract artifacts validated successfully." | Set-Content -LiteralPath (Join-Path $ArtifactDir "contract-validation.txt") -Encoding utf8
Write-Host "Contract artifacts validated successfully."
