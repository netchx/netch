param([string]$OutputPath)
$name="v2ray-plugin.tar.gz"
$address="https://github.com/teddysun/v2ray-plugin/releases/download/v4.40.1/v2ray-plugin-windows-amd64-v4.40.1.tar.gz"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

7z x $name
if (-Not $?) { exit $lastExitCode }

7z x "v2ray-plugin.tar"
if (-Not $?) { exit $lastExitCode }

Move-Item -Force "v2ray-plugin_windows_amd64.exe" "$OutputPath\v2ray-plugin.exe"

Remove-Item -Recurse -Force $name
Remove-Item -Recurse -Force v2ray-plugin.tar
exit 0