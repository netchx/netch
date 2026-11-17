Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = 'v0.6.2'
$archive = Join-Path $PWD "pcap2socks-$version-windows-amd64.zip"
$expectedSha256 = '59b3a2e5054c5a0252f3ac5f10b2862aa53a91e65b2e56e76d5706838d291953'
$expectedBinarySha256 = 'de84f4a32c8c9d4888197345d52ba793e4bdf510614852e76f336f627e43def3'

try {
    Invoke-WebRequest -Uri "https://github.com/zhxie/pcap2socks/releases/download/$version/pcap2socks-$version-windows-amd64.zip" -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
        throw 'pcap2socks archive SHA-256 mismatch'
    }

    $destination = Join-Path (Split-Path $PWD -Parent) 'release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entry = $zip.GetEntry('pcap2socks.exe')
        if ($null -eq $entry) { throw 'pcap2socks.exe is missing from the official archive' }
        $binary = Join-Path $destination 'pcap2socks.exe'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $binary, $true)
        if ((Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedBinarySha256) {
            Remove-Item $binary -ErrorAction SilentlyContinue
            throw 'pcap2socks executable SHA-256 mismatch'
        }
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item $archive -ErrorAction SilentlyContinue
    Pop-Location
}
exit 0
