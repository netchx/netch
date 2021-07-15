param([string]$OutputPath)
$name="ck-client.exe"
$address="https://github.com/cbeuw/Cloak/releases/download/v2.5.4/ck-client-windows-amd64-v2.5.4.exe"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

Move-Item -Force $name $OutputPath
exit 0