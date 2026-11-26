Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = 'v2.7.0'
$archive = Join-Path $PWD 'tun2socks-windows-amd64-v3.zip'
$expectedSha256 = 'ea9caa464664325afaaeded3e157a2152e29c5cfaa20a54157bcf330d6a93873'
$expectedBinarySha256 = 'f08d327819f722da3ee3f591cb7e9e36b06640a92b3f63f5073a454be29be35f'

try {
    Invoke-WebRequest -Uri "https://github.com/xjasonlyu/tun2socks/releases/download/$version/tun2socks-windows-amd64-v3.zip" -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
        throw 'tun2socks archive SHA-256 mismatch'
    }

    $destination = Join-Path (Split-Path $PWD -Parent) 'release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entry = $zip.GetEntry('tun2socks-windows-amd64-v3.exe')
        if ($null -eq $entry) { throw 'tun2socks-windows-amd64-v3.exe is missing from the official archive' }
        $binary = Join-Path $destination 'tun2socks-windows-amd64-v3.exe'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $binary, $true)
        if ((Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedBinarySha256) {
            Remove-Item $binary -ErrorAction SilentlyContinue
            throw 'tun2socks executable SHA-256 mismatch'
        }
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item $archive -ErrorAction SilentlyContinue
    Pop-Location
}
exit 0
