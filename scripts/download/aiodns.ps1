param([string]$OutputPath)
$address="https://github.com/aiocloud/aiodns/releases/download/1.0.4/aiodns.bin"
$domains="https://raw.githubusercontent.com/aiocloud/aiodns/master/aiodns.conf"

Invoke-WebRequest -Uri $address -OutFile aiodns.bin
Invoke-WebRequest -Uri $domains -OutFile aiodns.conf

Move-Item -Force aiodns.bin  $OutputPath
Move-Item -Force aiodns.conf $OutputPath
exit 0
