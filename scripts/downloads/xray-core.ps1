param([string]$OutputPath)
$address="https://github.com/XTLS/Xray-core/releases/download/v1.4.2/Xray-windows-64.zip"

Invoke-WebRequest -Uri $address -OutFile xray-core.zip
Expand-Archive -Force -Path xray-core.zip -DestinationPath xray-core

Move-Item -Force xray-core\xray.exe    $OutputPath
Move-Item -Force xray-core\geoip.dat   $OutputPath
Move-Item -Force xray-core\geosite.dat $OutputPath

Remove-Item -Recurse -Force xray-core
Remove-Item -Recurse -Force xray-core.zip
exit 0
