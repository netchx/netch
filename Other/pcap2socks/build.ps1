Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

try {
    Invoke-WebRequest `
        -Uri 'https://github.com/zhxie/pcap2socks/releases/download/v0.6.2/pcap2socks-v0.6.2-windows-amd64.zip' `
        -OutFile 'pcap2socks.zip'
    Expand-Archive -Force -Path pcap2socks.zip -DestinationPath pcap2socks
}
catch {
    exit 1
}

mv -Force 'pcap2socks\pcap2socks.exe' '..\release\pcap2socks.exe'
exit 0