param(
    [string]$MatrixPath = "docs/ide-parity-matrix.json",
    [string]$OutPath = "artifacts/ide-parity/revalidation-dry-run.md"
)

$ErrorActionPreference = "Stop"

function Compare-VersionParts([string]$Actual, [string]$Minimum, [string]$MaximumExclusive) {
    $actualVersion = [version]$Actual
    $minimumVersion = [version]$Minimum
    $maximumVersion = [version]$MaximumExclusive
    return ($actualVersion -ge $minimumVersion) -and ($actualVersion -lt $maximumVersion)
}

function Add-IssueBlock([System.Text.StringBuilder]$Builder, [string]$Product, [string]$Detected, [string]$Minimum, [string]$Maximum, [string[]]$Rows) {
    [void]$Builder.AppendLine("## IDE parity version drift")
    [void]$Builder.AppendLine()
    [void]$Builder.AppendLine("- product: $Product")
    [void]$Builder.AppendLine("- detected version: $Detected")
    [void]$Builder.AppendLine("- current pin: $Minimum <= version < $Maximum")
    [void]$Builder.AppendLine("- fixture: IdeParityCounterFixture")
    [void]$Builder.AppendLine("- affected matrix rows: $($Rows -join ', ')")
    [void]$Builder.AppendLine("- expected behavior: generated-source navigation, diagnostics, XML docs, symbol search, and path contract remain stable")
    [void]$Builder.AppendLine("- observed behavior: configured version moved outside the pinned range")
    [void]$Builder.AppendLine("- evidence needed: refresh sanitized evidence manifests and Visual Studio calibration row before widening the range")
    [void]$Builder.AppendLine("- labels: ide-parity, conformance-revalidation")
    [void]$Builder.AppendLine("- fallback: GitHub issue creation unavailable or not requested; this dry-run artifact blocks the release checklist")
    [void]$Builder.AppendLine()
}

$matrix = Get-Content -LiteralPath $MatrixPath -Raw | ConvertFrom-Json
$rows = @($matrix.rows | Where-Object { $_.tier -ne "Out-of-scope" } | ForEach-Object { $_.rowId })
$builder = [System.Text.StringBuilder]::new()

$pins = @(
    @{ Product = "Visual Studio 2022"; Env = "FRONTCOMPOSER_IDE_VERSION_VISUALSTUDIO"; Minimum = "17.13"; Maximum = "17.14" },
    @{ Product = "JetBrains Rider"; Env = "FRONTCOMPOSER_IDE_VERSION_RIDER"; Minimum = "2026.1"; Maximum = "2026.2" },
    @{ Product = ".NET SDK"; Env = "FRONTCOMPOSER_DOTNET_SDK_VERSION"; Minimum = "10.0.103"; Maximum = "10.1.0" }
)

$blocking = $false
foreach ($pin in $pins) {
    $detected = [Environment]::GetEnvironmentVariable($pin.Env)
    if ([string]::IsNullOrWhiteSpace($detected)) {
        continue
    }

    if (-not (Compare-VersionParts -Actual $detected -Minimum $pin.Minimum -MaximumExclusive $pin.Maximum)) {
        $blocking = $true
        Add-IssueBlock -Builder $builder -Product $pin.Product -Detected $detected -Minimum $pin.Minimum -Maximum $pin.Maximum -Rows $rows
    }
}

if ($blocking) {
    $directory = Split-Path -Parent $OutPath
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    Set-Content -LiteralPath $OutPath -Value $builder.ToString() -Encoding UTF8
    Write-Error "IDE parity version drift detected. Dry-run issue artifact written to $OutPath."
}

Write-Host "IDE parity configured versions are within pinned ranges or no configured versions were supplied."
