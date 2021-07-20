param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/v2fly/v2ray-core/releases/download/v4.40.1/v2ray-windows-64.zip' `
        -OutFile 'v2ray-core.zip'
}
catch {
    exit 1
}

7z x 'v2ray-core.zip' -o'v2ray-core'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'v2ray-core\v2ctl.exe'  $OutputPath
mv -Force 'v2ray-core\v2ray.exe'  $OutputPath
mv -Force 'v2ray-core\wv2ray.exe' $OutputPath

rm -Recurse -Force v2ray-core
rm -Recurse -Force v2ray-core.zip
exit 0