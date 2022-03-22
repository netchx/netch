Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

try {
    Invoke-WebRequest `
        -Uri 'https://raw.githubusercontent.com/17mon/china_ip_list/master/china_ip_list.txt' `
        -OutFile '..\..\release\bin\chnip.txt'
}
catch {
    exit 1
}

exit 0