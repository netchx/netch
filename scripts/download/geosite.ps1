param([string]$OutputPath)
$address="https://github.com/v2fly/domain-list-community/releases/download/20210709152606/dlc.dat"

Invoke-WebRequest -Uri $address -OutFile geosite.dat

Move-Item -Force geosite.dat $OutputPath
exit 0