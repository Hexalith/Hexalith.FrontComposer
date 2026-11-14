# Prepare both .NET 11 shared frameworks for current tests and tools.
[CmdletBinding()]
param(
    [string]$InstallDirectory = $env:DOTNET_ROOT
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($InstallDirectory)) {
    throw 'The SDK setup must set DOTNET_ROOT, or pass -InstallDirectory explicitly.'
}

$extension = if ($IsWindows) { 'ps1' } else { 'sh' }
$installer = Join-Path ([System.IO.Path]::GetTempPath()) "frontcomposer-dotnet-install-$([Guid]::NewGuid().ToString('N')).$extension"
try {
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.$extension" -OutFile $installer -TimeoutSec 60 -MaximumRetryCount 3 -RetryIntervalSec 2
    foreach ($runtime in @('dotnet', 'aspnetcore')) {
        if ($IsWindows) {
            & pwsh -NoProfile -File $installer -Runtime $runtime -Version '11.0.0' -InstallDir $InstallDirectory -NoPath -SkipNonVersionedFiles
        }
        else {
            & bash $installer --runtime $runtime --version '11.0.0' --install-dir $InstallDirectory --no-path --skip-non-versioned-files
        }
        if ($LASTEXITCODE -ne 0) {
            throw ".NET 11 $runtime runtime installation failed with exit code $LASTEXITCODE."
        }
    }

    $dotnetName = if ($IsWindows) { 'dotnet.exe' } else { 'dotnet' }
    $dotnetExecutable = Join-Path $InstallDirectory $dotnetName
    if (-not (Test-Path -LiteralPath $dotnetExecutable -PathType Leaf)) {
        throw "The requested installation is missing its .NET executable: $dotnetExecutable"
    }
    $installedRuntimes = @(& $dotnetExecutable --list-runtimes 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw ".NET runtime verification failed for $dotnetExecutable with exit code $LASTEXITCODE. $($installedRuntimes -join [Environment]::NewLine)"
    }
    foreach ($runtimeName in @('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App')) {
        if (-not ($installedRuntimes -match "^$([Regex]::Escape($runtimeName)) 11\.0\.0 ")) {
            throw "The requested installation at $InstallDirectory is missing $runtimeName 11.0.0."
        }
    }
}
finally {
    Remove-Item -LiteralPath $installer -Force -ErrorAction SilentlyContinue
}
