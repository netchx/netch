param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/teddysun/xray-plugin/releases/download/v1.4.2/xray-plugin-windows-amd64-v1.4.2.tar.gz' `
        -OutFile 'xray-plugin.tar.gz'
}
catch {
    exit 1
}

7z x 'xray-plugin.tar.gz'
if ( -Not $? ) { exit $lastExitCode }

7z x 'xray-plugin.tar'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'xray-plugin_windows_amd64.exe' "$OutputPath\xray-plugin.exe"

rm -Force 'xray-plugin.tar'
rm -Force 'xray-plugin.tar.gz'
exit 0