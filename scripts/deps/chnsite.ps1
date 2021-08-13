param (
    [string]
    $OutputPath
)

try {
    Invoke-WebRequest `
        -Uri 'https://raw.githubusercontent.com/felixonmars/dnsmasq-china-list/master/accelerated-domains.china.conf' `
        -OutFile 'chnsite.txt'
}
catch {
    exit 1
}

foreach ( $data in (Get-Content -Path 'chnsite.txt') ) {
    $data = $data.Replace('server=/', '')
    $data = $data.Replace('/114.114.114.114', '')
    $data = $data.Trim()

    if ( $data.Length -gt 0 ) {
        Add-Content -Path 'newsite.txt' -Value $data
    }
}
mv -Force 'newsite.txt' 'chnsite.txt'

mv -Force 'chnsite.txt' $OutputPath
exit 0