Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
$ErrorActionPreference = 'Stop'

$version = 'v5.0.16'
$archive = Join-Path $PWD 'v2ray-windows-64.zip'
$expectedSha256 = '0539baac700d8de4fd45b9f85f67fec8c6c4880664a99b475a8bb3061e37ff7f'
$expectedBinarySha256 = '62f1df5d02aa2cfb5d1f15828d3561c415a7687a2efe869ab38e6ce9a0c135d6'

try {
    Invoke-WebRequest -Uri "https://github.com/SagerNet/v2ray-core/releases/download/$version/v2ray-windows-64.zip" -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
        throw 'v2ray-sn archive SHA-256 mismatch'
    }

    $destination = Join-Path (Split-Path $PWD -Parent) 'release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entry = $zip.GetEntry('v2ray.exe')
        if ($null -eq $entry) { throw 'v2ray.exe is missing from the official archive' }
        $binary = Join-Path $destination 'v2ray-sn.exe'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $binary, $true)
        if ((Get-FileHash $binary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedBinarySha256) {
            Remove-Item $binary -ErrorAction SilentlyContinue
            throw 'v2ray-sn executable SHA-256 mismatch'
        }
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item $archive -ErrorAction SilentlyContinue
    Pop-Location
}
exit 0
