param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://raw.githubusercontent.com/17mon/china_ip_list/master/china_ip_list.txt' `
        -OutFile 'chnip.txt'
}
catch {
    exit 1
}

mv -Force 'chnip.txt' $OutputPath
exit 0