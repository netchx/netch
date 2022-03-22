Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

try {
    Invoke-WebRequest `
        -Uri 'https://www.wintun.net/builds/wintun-0.13.zip' `
        -OutFile 'wintun.zip'
}
catch {
    exit 1
}

7z x 'wintun.zip'
if ( -Not $? ) { exit $lastExitCode }

mv -Force 'wintun\bin\amd64\wintun.dll' '..\release\wintun.dll'

rm -Recurse -Force 'wintun'
rm -Recurse -Force 'wintun.zip'
exit 0