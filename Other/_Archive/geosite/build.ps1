Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat' `
        -OutFile '..\..\release\bin\geosite.dat'
}
catch {
    exit 1
}

exit 0