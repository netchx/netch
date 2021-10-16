Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/v2fly/geoip/releases/download/202110140026/geoip.dat' `
        -OutFile 'geoip.dat'
}
catch {
    exit 1
}

mv -Force 'geoip.dat' '..\release\geoip.dat'
exit 0