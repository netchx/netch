$name="i18n.zip"
$address="https://github.com/netchx/netch-i18n/archive/refs/heads/main.zip"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

..\scripts\extract.ps1 $name "i18n"
if (-Not $?) { exit $lastExitCode }

Copy-Item -Recurse -Force .\netch-i18n-main\* .\i18n

Remove-Item -Force $name
Remove-Item -Recurse -Force netch-i18n-main
exit 0