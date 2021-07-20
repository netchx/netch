param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/cbeuw/Cloak/releases/download/v2.5.4/ck-client-windows-amd64-v2.5.4.exe' `
        -OutFile 'ck-client.exe'
}
catch {
    exit 1
}

mv -Force 'ck-client.exe' $OutputPath
exit 0