param(
  [string] $ArtifactDir = "artifacts/property",
  [string] $FixturePath = "tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Lifecycle/Fixtures/command-idempotency-counterexamples.json"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ArtifactFullPath = Join-Path $RepoRoot $ArtifactDir
$FixtureFullPath = Join-Path $RepoRoot $FixturePath
$errors = New-Object System.Collections.Generic.List[string]

function Add-Failure([string] $Message) {
  $errors.Add($Message) | Out-Null
}

function Test-UnsafeText([string] $Text) {
  if ([string]::IsNullOrEmpty($Text)) { return $null }
  if ($Text -match 'C:\\Users\\|/home/[^/\s]+|/Users/[^/\s]+') { return "machine-local path" }
  if ($Text -match '(?i)(bearer\s+[a-z0-9._-]{12,}|api[_-]?key\s*[:=]|password\s*[:=]|secret\s*[:=]|token\s*[:=])') { return "secret-like value" }
  if ($Text -match '(?i)tenant[_-]?id\s*[:=]\s*["'']?[0-9a-f-]{16,}') { return "tenant identifier" }
  if ($Text.Length -gt 250000) { return "oversized artifact" }
  return $null
}

New-Item -ItemType Directory -Force -Path $ArtifactFullPath | Out-Null

if (!(Test-Path -LiteralPath $FixtureFullPath)) {
  Add-Failure "Missing command idempotency counterexample fixture file: $FixturePath"
}
else {
  $fixtureText = Get-Content -LiteralPath $FixtureFullPath -Raw
  $unsafe = Test-UnsafeText $fixtureText
  if ($unsafe) {
    Add-Failure "Fixture redaction scan failed: $unsafe"
  }

  try {
    $fixture = $fixtureText | ConvertFrom-Json -Depth 64
    foreach ($entry in @($fixture.fixtures)) {
      foreach ($field in @("propertyName", "seed", "size", "expectedVisibleOutcomeCount", "retentionReason")) {
        if ($null -eq $entry.$field -or [string]::IsNullOrWhiteSpace([string] $entry.$field)) {
          Add-Failure "Fixture '$($entry.name)' is missing $field."
        }
      }

      if ($null -eq $entry.minimalSequence -or @($entry.minimalSequence).Count -eq 0) {
        Add-Failure "Fixture '$($entry.name)' is missing minimalSequence."
      }
    }
  }
  catch {
    Add-Failure "Fixture JSON is malformed: $($_.Exception.Message)"
  }
}

$artifactFiles = @(Get-ChildItem -LiteralPath $ArtifactFullPath -Recurse -File -ErrorAction SilentlyContinue)
foreach ($file in $artifactFiles) {
  $unsafe = Test-UnsafeText (Get-Content -LiteralPath $file.FullName -Raw)
  if ($unsafe) {
    Add-Failure "Property artifact '$($file.Name)' failed redaction scan: $unsafe"
  }
}

$seedSummaryPath = Join-Path $ArtifactFullPath "property-seed-summary.md"
$seedSummary = if (Test-Path -LiteralPath $seedSummaryPath) {
  Get-Content -LiteralPath $seedSummaryPath -Raw
} else {
  "- Seed summary: missing before validation; this is allowed only for fixture-only validation."
}

$runEvidencePath = Join-Path $ArtifactFullPath "property-run-evidence.json"
if (Test-Path -LiteralPath $runEvidencePath) {
  try {
    $runEvidence = Get-Content -LiteralPath $runEvidencePath -Raw | ConvertFrom-Json -Depth 32
    foreach ($field in @("fsCheckPackage", "replaySeed", "size", "maxSize", "sequenceCount", "generatedOperationDistribution", "shrinkResult", "replayCommand")) {
      if ($null -eq $runEvidence.$field -or [string]::IsNullOrWhiteSpace([string] $runEvidence.$field)) {
        Add-Failure "Property run evidence is missing $field."
      }
    }
  }
  catch {
    Add-Failure "Property run evidence JSON is malformed: $($_.Exception.Message)"
  }
}
elseif ($artifactFiles.Count -gt 0) {
  Add-Failure "Property artifacts exist but property-run-evidence.json is missing."
}

$summary = @"
## Property Evidence

- FsCheck package: FsCheck.Xunit.v3 3.3.1
- CI suite: 1000 generated command sequences, deterministic replay seed configured on property attributes
- Nightly suite: 10000 generated command sequences using `FC_PROPERTY_MAX_TEST=10000`; replay seed is generated when `-Replay` is omitted
- Fixture path: $FixturePath
- Redaction scan: $(if ($errors.Count -eq 0) { "clean" } else { "failed" })
- Submodules: no recursive nested submodule command is used

$seedSummary
"@
$summary | Set-Content -LiteralPath (Join-Path $ArtifactFullPath "job-summary.md") -Encoding utf8

if ($errors.Count -gt 0) {
  $errors | Set-Content -LiteralPath (Join-Path $ArtifactFullPath "property-validation-errors.txt") -Encoding utf8
  throw "Property artifact validation failed:`n - $($errors -join "`n - ")"
}

Write-Host "Property artifacts validated."
