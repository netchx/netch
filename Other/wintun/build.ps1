Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = '0.14.1'
$archive = Join-Path $PWD "wintun-$version.zip"
$expectedSha256 = '07c256185d6ee3652e09fa55c0b673e2624b565e02c4b9091c79ca7d2f24ef51'
$expectedBinarySha256 = 'e5da8447dc2c320edc0fc52fa01885c103de8c118481f683643cacc3220dafce'

try {
    Invoke-WebRequest -Uri "https://www.wintun.net/builds/wintun-$version.zip" -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
        throw 'Wintun archive SHA-256 mismatch'
    }

    $destination = Join-Path (Split-Path $PWD -Parent) 'release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entry = $zip.GetEntry('wintun/bin/amd64/wintun.dll')
        if ($null -eq $entry) { throw 'wintun.dll is missing from the official archive' }
        $binary = Join-Path $destination 'wintun.dll'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $binary, $true)
        if ((Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedBinarySha256) {
            Remove-Item $binary -ErrorAction SilentlyContinue
            throw 'wintun.dll SHA-256 mismatch'
        }
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item $archive -ErrorAction SilentlyContinue
    Pop-Location
}
exit 0
