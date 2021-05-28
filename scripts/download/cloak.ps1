param([string]$OutputPath)
$address="https://github.com/cbeuw/Cloak/releases/download/v2.5.4/ck-client-windows-amd64-v2.5.4.exe"

Invoke-WebRequest -Uri $address -OutFile ck-client.exe

Move-Item -Force ck-client.exe $OutputPath
exit 0
