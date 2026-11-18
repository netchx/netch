$ErrorActionPreference = 'Stop'
$version = 'v1.19.34'
$asset = "mihomo-windows-amd64-v1-$version.zip"
$archive = New-TemporaryFile
try {
    Invoke-WebRequest -Uri "https://github.com/MetaCubeX/mihomo/releases/download/$version/$asset" -OutFile $archive.FullName
    $destination = Join-Path $PSScriptRoot '..\release'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive.FullName)
    try {
        $entry = $zip.GetEntry('mihomo-windows-amd64-v1.exe')
        if ($null -eq $entry) { throw 'Mihomo executable is missing from the official archive' }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $destination 'mihomo.exe'), $true)
    }
    finally { $zip.Dispose() }
}
finally {
    Remove-Item -LiteralPath $archive.FullName
}
exit 0
