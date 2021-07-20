$name="data.zip"
$address="https://github.com/netchx/netch-data/archive/refs/heads/main.zip"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

..\scripts\extract.ps1 $name "bin"
if (-Not $?) { exit $lastExitCode }

Copy-Item -Recurse -Force .\netch-data-main\* .\bin

Remove-Item -Force $name
Remove-Item -Recurse -Force netch-data-main
exit 0