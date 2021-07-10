param([string]$OutputPath)
$address="https://github.com/v2fly/geoip/releases/download/202107080024/geoip.dat"

Invoke-WebRequest -Uri $address -OutFile geoip.dat

Move-Item -Force geoip.dat $OutputPath
exit 0