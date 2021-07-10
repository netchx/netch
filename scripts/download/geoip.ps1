param([string]$OutputPath)
$name="geoip.dat"
$address="https://github.com/v2fly/geoip/releases/download/202107080024/geoip.dat"

Invoke-WebRequest -Uri $address -OutFile $name

Move-Item -Force $name $OutputPath
exit 0