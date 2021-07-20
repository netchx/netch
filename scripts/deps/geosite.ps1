param([string]$OutputPath)
$name="geosite.dat"
$address="https://github.com/v2fly/domain-list-community/releases/download/20210718155822/dlc.dat"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

Move-Item -Force $name $OutputPath
exit 0