param([string]$OutputPath)
$name="xray-core.zip"
$address="https://github.com/XTLS/Xray-core/releases/download/v1.4.2/Xray-windows-64.zip"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

..\scripts\extract.ps1 $name "xray-core"
if (-Not $?) { exit $lastExitCode }

Move-Item -Force "xray-core\xray.exe" $OutputPath

Remove-Item -Recurse -Force xray-core
Remove-Item -Recurse -Force xray-core.zip
exit 0