Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = 'v26.11.9'
$archive = Join-Path $PWD 'Xray-windows-64.zip'
$expectedSha256 = '1262522266337b824f07beb8e1375786b437683b3890105469088f8e17e0a69d'
$expectedBinarySha256 = '1ca7a2cf61ba2c5d20b21a80d405b57933939751081cb289f07bd8ddf7cc3185'

try {
    Invoke-WebRequest -Uri "https://github.com/XTLS/Xray-core/releases/download/$version/Xray-windows-64.zip" -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
        throw 'Xray archive SHA-256 mismatch'
    }

    $destination = Join-Path (Split-Path $PWD -Parent) 'release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entry = $zip.GetEntry('xray.exe')
        if ($null -eq $entry) { throw 'xray.exe is missing from the official archive' }
        $binary = Join-Path $destination 'xray.exe'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $binary, $true)
        if ((Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedBinarySha256) {
            Remove-Item $binary
            throw 'Xray executable SHA-256 mismatch'
        }
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item $archive -ErrorAction SilentlyContinue
    Pop-Location
}
exit 0
