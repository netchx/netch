param([string]$OutputPath)
$name="xray-plugin.tar.gz"
$address="https://github.com/teddysun/xray-plugin/releases/download/v1.4.2/xray-plugin-windows-amd64-v1.4.2.tar.gz"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

7z x $name
if (-Not $?) { exit $lastExitCode }

7z x "xray-plugin.tar"
if (-Not $?) { exit $lastExitCode }

Move-Item -Force "xray-plugin_windows_amd64.exe" "$OutputPath\xray-plugin.exe"

Remove-Item -Recurse -Force $name
Remove-Item -Recurse -Force xray-plugin.tar
exit 0