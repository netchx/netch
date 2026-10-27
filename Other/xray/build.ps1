Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = 'v26.10.21'
$archive = Join-Path $PWD 'Xray-windows-64.zip'
$expectedSha256 = 'fc58c39ee2f2717b4c54d28d374362389e5319b20ae44aea76ea45a5243ec827'
$expectedBinarySha256 = 'c77a00eb84fb93ae044c2d771a1cabcc033b81e487253fbc35b12df6879c1e15'

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
finally { Remove-Item $archive -ErrorAction SilentlyContinue }
