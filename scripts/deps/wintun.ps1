param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://www.wintun.net/builds/wintun-0.14.zip' `
        -OutFile 'wintun.zip'
}
catch {
    exit 1
}

7z x 'wintun.zip'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'wintun\bin\amd64\wintun.dll' $OutputPath

rm -Recurse -Force 'wintun'
rm -Recurse -Force 'wintun.zip'
exit 0