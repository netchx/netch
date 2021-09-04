param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/teddysun/v2ray-plugin/releases/download/v4.42.1/v2ray-plugin-windows-amd64-v4.42.1.tar.gz' `
        -OutFile 'v2ray-plugin.tar.gz'
}
catch {
    exit 1
}

7z x 'v2ray-plugin.tar.gz'
if ( -Not $? ) { exit $lastExitCode }

7z x 'v2ray-plugin.tar'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'v2ray-plugin_windows_amd64.exe' "$OutputPath\v2ray-plugin.exe"

rm -Force 'v2ray-plugin.tar'
rm -Force 'v2ray-plugin.tar.gz'
exit 0