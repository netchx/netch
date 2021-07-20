$name="mode.zip"
$address="https://github.com/netchx/netch-mode/archive/refs/heads/main.zip"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

..\scripts\extract.ps1 $name "mode"
if (-Not $?) { exit $lastExitCode }

Copy-Item -Recurse -Force .\netch-mode-main\* .\mode

Remove-Item -Force $name
Remove-Item -Recurse -Force netch-mode-main
exit 0