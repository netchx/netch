param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/XTLS/Xray-core/releases/download/v1.4.2/Xray-windows-64.zip' `
        -OutFile 'xray-core.zip'
}
catch {
    exit 1
}

7z x 'xray-core.zip' -o'xray-core'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'xray-core\xray.exe' $OutputPath

rm -Recurse -Force xray-core
rm -Recurse -Force xray-core.zip
exit 0