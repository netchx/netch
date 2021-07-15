param([string]$OutputPath)
$name="v2ray-core.zip"
$address="https://github.com/v2fly/v2ray-core/releases/download/v4.40.1/v2ray-windows-64.zip"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

..\scripts\extract.ps1 $name "v2ray-core"
if (-Not $?) { exit $lastExitCode }

Move-Item -Force v2ray-core\v2ctl.exe  $OutputPath
Move-Item -Force v2ray-core\v2ray.exe  $OutputPath
Move-Item -Force v2ray-core\wv2ray.exe $OutputPath

Remove-Item -Recurse -Force v2ray-core
Remove-Item -Recurse -Force v2ray-core.zip
exit 0