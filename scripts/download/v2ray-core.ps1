param([string]$OutputPath)
$address="https://github.com/v2fly/v2ray-core/releases/download/v4.40.1/v2ray-windows-64.zip"

Invoke-WebRequest -Uri $address -OutFile v2ray-core.zip
Expand-Archive -Force -Path v2ray-core.zip -DestinationPath v2ray-core

Move-Item -Force v2ray-core\v2ctl.exe  $OutputPath
Move-Item -Force v2ray-core\v2ray.exe  $OutputPath
Move-Item -Force v2ray-core\wv2ray.exe $OutputPath

Remove-Item -Recurse -Force v2ray-core
Remove-Item -Recurse -Force v2ray-core.zip
exit 0