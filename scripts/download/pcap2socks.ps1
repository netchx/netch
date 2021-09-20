param([string]$OutputPath)
$address="https://github.com/zhxie/pcap2socks/releases/download/v0.6.2/pcap2socks-v0.6.2-windows-amd64.zip"

Invoke-WebRequest -Uri $address -OutFile pcap2socks.zip
Expand-Archive -Force -Path pcap2socks.zip -DestinationPath pcap2socks

Move-Item -Force pcap2socks\pcap2socks.exe    $OutputPath

Remove-Item -Recurse -Force pcap2socks
Remove-Item -Recurse -Force pcap2socks.zip
exit 0
