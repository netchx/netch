param([string]$OutputPath)

$NetchDataURL="https://github.com/netchx/netchdata/archive/refs/heads/master.zip"
$NetchModeURL="https://github.com/netchx/netchmode/archive/refs/heads/master.zip"
$NetchI18NURL="https://github.com/netchx/netchi18n/archive/refs/heads/master.zip"

$last=$(Get-Location)
New-Item -ItemType Directory -Name $OutputPath | Out-Null
Set-Location $OutputPath

Invoke-WebRequest -Uri $NetchDataURL -OutFile data.zip
Invoke-WebRequest -Uri $NetchModeURL -OutFile mode.zip
Invoke-WebRequest -Uri $NetchI18NURL -OutFile i18n.zip

Expand-Archive -Force -Path data.zip -DestinationPath .
Expand-Archive -Force -Path mode.zip -DestinationPath .
Expand-Archive -Force -Path i18n.zip -DestinationPath .

New-Item -ItemType Directory -Name bin  | Out-Null
New-Item -ItemType Directory -Name mode | Out-Null
New-Item -ItemType Directory -Name i18n | Out-Null

Copy-Item -Recurse -Force .\netchdata-master\*             .\bin
Copy-Item -Recurse -Force .\netchmode-master\mode\*        .\mode
Copy-Item -Recurse -Force .\netchi18n-master\i18n\* .\i18n

Remove-Item -Recurse -Force netchdata-master
Remove-Item -Recurse -Force netchmode-master
Remove-Item -Recurse -Force netchi18n-master
Remove-Item -Force data.zip
Remove-Item -Force mode.zip
Remove-Item -Force i18n.zip

..\scripts\download\aiodns.ps1    -OutputPath bin
..\scripts\download\cloak.ps1     -OutputPath bin
..\scripts\download\xray-core.ps1 -OutputPath bin

Get-Item *
Set-Location $last
exit 0
