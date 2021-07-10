param([string]$OutputPath)
$name="geosite.dat"
$address="https://github.com/v2fly/domain-list-community/releases/download/20210709152606/dlc.dat"

Invoke-WebRequest -Uri $address -OutFile $name

Move-Item -Force $name $OutputPath
exit 0