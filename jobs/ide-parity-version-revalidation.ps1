<#
.SYNOPSIS
    Story 9-3 IDE parity version revalidation.

.DESCRIPTION
    Reads configured IDE/SDK version pins from environment variables and compares them
    against the matrix's pinned ranges. On drift the script attempts to file a GitHub
    issue when `gh` is authenticated and the required labels exist; otherwise it writes
    a deterministic dry-run artifact and exits non-zero so the release-gate fails closed.

.PARAMETER MatrixPath
    Project-relative path to the IDE parity matrix JSON.

.PARAMETER OutPath
    Project-relative path for the dry-run issue artifact when GitHub creation is unavailable.

.PARAMETER NoGithub
    Force dry-run mode even if `gh` is authenticated. Useful for tests and local runs.

.OUTPUTS
    Exit code 0: pins in range, or no configured versions supplied.
    Exit code 1: drift detected; either a live issue was created or a dry-run artifact was written.
                 The release-gate must treat exit 1 as blocking.
#>
[CmdletBinding()]
param(
    [string]$MatrixPath = "docs/ide-parity-matrix.json",
    [string]$OutPath = "artifacts/ide-parity/revalidation-dry-run.md",
    [switch]$NoGithub
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RequiredLabels = @("ide-parity", "conformance-revalidation")

function Get-RepositoryRoot {
    $root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
    return $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
}

function Resolve-RepoBoundPath {
    [CmdletBinding()]
    param(
        [string]$Path,
        [string]$RepositoryRoot,
        [string]$ParameterName
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$ParameterName is required."
    }

    if ($Path -match '^[A-Za-z][A-Za-z0-9+\-.]*://') {
        throw "$ParameterName must be a repository-relative path, not a URI."
    }

    if ($Path -match '^[A-Za-z]:[^\\/]') {
        throw "$ParameterName must not be a drive-relative path."
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    } else {
        [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot $Path))
    }

    $rootWithSeparator = $RepositoryRoot
    if (-not $rootWithSeparator.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootWithSeparator = $rootWithSeparator + [System.IO.Path]::DirectorySeparatorChar
    }

    $comparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    if (-not ($candidate.Equals($RepositoryRoot, $comparison) -or $candidate.StartsWith($rootWithSeparator, $comparison))) {
        throw "$ParameterName must stay inside the repository root."
    }

    return $candidate
}

function Get-VersionParts {
    [CmdletBinding()]
    param([string]$Raw)

    if ([string]::IsNullOrWhiteSpace($Raw)) {
        throw "Version is required."
    }

    $segments = $Raw.Split('.', [StringSplitOptions]::RemoveEmptyEntries)
    $parts = New-Object 'System.Collections.Generic.List[int]'
    foreach ($segment in $segments) {
        $digitEnd = 0
        while ($digitEnd -lt $segment.Length -and [char]::IsDigit($segment[$digitEnd])) {
            $digitEnd++
        }
        if ($digitEnd -eq 0) {
            throw "Version segment '$segment' must start with a digit."
        }
        $digits = $segment.Substring(0, $digitEnd)
        $value = 0
        if (-not [int]::TryParse($digits, [ref]$value)) {
            throw "Version segment '$segment' overflows Int32."
        }
        [void]$parts.Add($value)
    }

    return ,$parts.ToArray()
}

function Compare-VersionParts {
    [CmdletBinding()]
    param([string]$Actual, [string]$Minimum, [string]$MaximumExclusive)

    $a = Get-VersionParts -Raw $Actual
    $min = Get-VersionParts -Raw $Minimum
    $max = Get-VersionParts -Raw $MaximumExclusive

    function Compare-Parts([int[]]$Left, [int[]]$Right) {
        $length = [Math]::Max($Left.Length, $Right.Length)
        for ($i = 0; $i -lt $length; $i++) {
            $l = if ($i -lt $Left.Length) { $Left[$i] } else { 0 }
            $r = if ($i -lt $Right.Length) { $Right[$i] } else { 0 }
            if ($l -ne $r) { return $l.CompareTo($r) }
        }
        return 0
    }

    return ((Compare-Parts $a $min) -ge 0) -and ((Compare-Parts $a $max) -lt 0)
}

function Get-EnvAcrossScopes([string]$Name) {
    foreach ($scope in @('Process', 'User', 'Machine')) {
        try {
            $value = [Environment]::GetEnvironmentVariable($Name, $scope)
            if (-not [string]::IsNullOrWhiteSpace($value)) { return $value }
        } catch [InvalidOperationException] {
            # Machine scope can throw on Linux; fall through.
        } catch [Security.SecurityException] {
        }
    }
    return $null
}

function Test-GhAvailable {
    if ($NoGithub) { return $false }
    if (-not (Get-Command -ErrorAction SilentlyContinue gh)) { return $false }
    try {
        $null = & gh auth status 2>&1
        if ($LASTEXITCODE -ne 0) { return $false }
    } catch {
        return $false
    }
    return $true
}

function Test-GhLabelsAvailable {
    foreach ($label in $RequiredLabels) {
        try {
            $null = & gh label list --search $label --limit 1 2>&1
            if ($LASTEXITCODE -ne 0) { return $false }
        } catch {
            return $false
        }
    }
    return $true
}

function Write-IssueBlock {
    [CmdletBinding()]
    param(
        [System.Text.StringBuilder]$Builder,
        [string]$Product,
        [string]$Detected,
        [string]$Minimum,
        [string]$Maximum,
        [string]$Os,
        [string]$Owner,
        [string]$Fixture,
        [string[]]$Rows,
        [bool]$GithubAvailable,
        [bool]$LabelsAvailable
    )

    [void]$Builder.AppendLine("## IDE parity version drift")
    [void]$Builder.AppendLine()
    [void]$Builder.AppendLine("- product: $Product")
    [void]$Builder.AppendLine("- detected version: $Detected")
    [void]$Builder.AppendLine("- current pin: $Minimum <= version < $Maximum")
    [void]$Builder.AppendLine("- OS/container: $Os")
    [void]$Builder.AppendLine("- fixture: $Fixture")
    [void]$Builder.AppendLine("- release owner: $Owner")
    [void]$Builder.AppendLine("- Visual Studio calibration row passes: evidence required before widening the pin")
    [void]$Builder.AppendLine("- affected matrix rows: $($Rows -join ', ')")
    [void]$Builder.AppendLine("- expected behavior: generated-source navigation, diagnostics, XML docs, symbol search, and path contract remain stable")
    [void]$Builder.AppendLine("- observed behavior: configured version moved outside the pinned range")
    [void]$Builder.AppendLine("- evidence needed: refresh sanitized evidence manifests and Visual Studio calibration row before widening the range")
    [void]$Builder.AppendLine("- labels: $($RequiredLabels -join ', ')")
    if (-not $GithubAvailable) {
        [void]$Builder.AppendLine("- fallback: GitHub CLI not authenticated; this dry-run artifact blocks the release checklist")
    } elseif (-not $LabelsAvailable) {
        [void]$Builder.AppendLine("- fallback: required labels not accessible; this dry-run artifact blocks the release checklist")
    }
    [void]$Builder.AppendLine()
}

function Write-AtomicFile([string]$Path, [string]$Content) {
    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
    $temp = Join-Path $directory (".{0}.{1}.tmp" -f ([System.IO.Path]::GetFileName($Path)), ([System.Guid]::NewGuid().ToString("N")))
    [System.IO.File]::WriteAllText($temp, $Content, (New-Object System.Text.UTF8Encoding($false)))
    Move-Item -LiteralPath $temp -Destination $Path -Force
}

# --- main ---
$RepositoryRoot = Get-RepositoryRoot
$MatrixPath = Resolve-RepoBoundPath -Path $MatrixPath -RepositoryRoot $RepositoryRoot -ParameterName "MatrixPath"
$OutPath = Resolve-RepoBoundPath -Path $OutPath -RepositoryRoot $RepositoryRoot -ParameterName "OutPath"

if (-not (Test-Path -LiteralPath $MatrixPath)) {
    throw "Matrix file not found: $MatrixPath"
}

$matrix = Get-Content -LiteralPath $MatrixPath -Raw | ConvertFrom-Json
if ($null -eq $matrix.PSObject.Properties['rows']) {
    throw "Matrix file '$MatrixPath' is missing the 'rows' array."
}

$rows = @($matrix.rows |
    Where-Object { $_.PSObject.Properties['tier'] -and $_.tier -ne "Out-of-scope" } |
    ForEach-Object { $_.rowId })
if ($rows.Count -eq 0) {
    throw "Matrix file '$MatrixPath' has zero in-scope rows; refusing to file an issue with empty rows."
}

$pins = @(
    @{ Product = "Visual Studio 2022"; Env = "FRONTCOMPOSER_IDE_VERSION_VISUALSTUDIO"; Minimum = "17.13"; Maximum = "17.14"; Os = "Windows" },
    @{ Product = "JetBrains Rider"; Env = "FRONTCOMPOSER_IDE_VERSION_RIDER"; Minimum = "2026.1"; Maximum = "2026.2"; Os = "Windows/macOS/Linux" },
    @{ Product = ".NET SDK"; Env = "FRONTCOMPOSER_DOTNET_SDK_VERSION"; Minimum = "10.0.103"; Maximum = "10.1.0"; Os = "All supported" }
)

$githubAvailable = Test-GhAvailable
$labelsAvailable = $false
if ($githubAvailable) {
    $labelsAvailable = Test-GhLabelsAvailable
}

$drifts = @()
foreach ($pin in $pins) {
    $detected = Get-EnvAcrossScopes -Name $pin.Env
    if ([string]::IsNullOrWhiteSpace($detected)) { continue }

    if (-not (Compare-VersionParts -Actual $detected -Minimum $pin.Minimum -MaximumExclusive $pin.Maximum)) {
        $drifts += [pscustomobject]@{
            Product  = $pin.Product
            Detected = $detected
            Minimum  = $pin.Minimum
            Maximum  = $pin.Maximum
            Os       = $pin.Os
        }
    }
}

if ($drifts.Count -eq 0) {
    Write-Host "IDE parity configured versions are within pinned ranges or no configured versions were supplied."
    exit 0
}

$builder = [System.Text.StringBuilder]::new()
foreach ($drift in $drifts) {
    Write-IssueBlock -Builder $builder `
        -Product $drift.Product `
        -Detected $drift.Detected `
        -Minimum $drift.Minimum `
        -Maximum $drift.Maximum `
        -Os $drift.Os `
        -Owner "SourceTools" `
        -Fixture "samples/IdeParityCounter" `
        -Rows $rows `
        -GithubAvailable $githubAvailable `
        -LabelsAvailable $labelsAvailable
}

$body = $builder.ToString()
$title = "IDE parity revalidation required: $($drifts[0].Product) $($drifts[0].Detected)"

if ($githubAvailable -and $labelsAvailable) {
    try {
        $labelArgs = @()
        foreach ($label in $RequiredLabels) { $labelArgs += @('--label', $label) }
        & gh issue create --title $title --body $body @labelArgs
        if ($LASTEXITCODE -ne 0) {
            throw "gh issue create exited with code $LASTEXITCODE."
        }
        Write-Host "Filed GitHub issue for IDE parity drift."
        exit 1
    } catch {
        Write-Warning "Live issue creation failed ($_); falling back to dry-run artifact."
    }
}

Write-AtomicFile -Path $OutPath -Content $body
Write-Host "IDE parity drift detected. Dry-run issue artifact written to $OutPath."
exit 1
