param([string]$OutputPath)
$name="geoip.dat"
$address="https://github.com/v2fly/geoip/releases/download/202107150023/geoip.dat"

..\scripts\download.ps1 $name $address
if (-Not $?) { exit $lastExitCode }

Move-Item -Force $name $OutputPath
exit 0