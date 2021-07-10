param([string]$OutputPath)

$NetchDataURL="https://github.com/netchx/netch-data/archive/refs/heads/main.zip"
$NetchModeURL="https://github.com/netchx/netch-mode/archive/refs/heads/main.zip"
$NetchI18NURL="https://github.com/netchx/netch-i18n/archive/refs/heads/main.zip"

$last=(Get-Location).Path
New-Item -ItemType Directory -Name $OutputPath | Out-Null
Set-Location $OutputPath

Invoke-WebRequest -Uri $NetchDataURL -OutFile data.zip
Invoke-WebRequest -Uri $NetchModeURL -OutFile mode.zip
Invoke-WebRequest -Uri $NetchI18NURL -OutFile i18n.zip

Expand-Archive -Force -Path data.zip -DestinationPath .
Expand-Archive -Force -Path mode.zip -DestinationPath .
Expand-Archive -Force -Path i18n.zip -DestinationPath .

New-Item -ItemType Directory -Name Bin  | Out-Null
New-Item -ItemType Directory -Name Mode | Out-Null
New-Item -ItemType Directory -Name I18N | Out-Null

Copy-Item -Recurse -Force .\netch-data-main\*             .\Bin
Copy-Item -Recurse -Force .\netch-mode-main\mode\*        .\Mode
Copy-Item -Recurse -Force .\netch-i18n-main\i18n\*        .\I18N

Remove-Item -Recurse -Force netch-data-main
Remove-Item -Recurse -Force netch-mode-main
Remove-Item -Recurse -Force netch-i18n-main
Remove-Item -Force data.zip
Remove-Item -Force mode.zip
Remove-Item -Force i18n.zip

..\scripts\download\cloak.ps1     -OutputPath Bin
..\scripts\download\xray-core.ps1 -OutputPath Bin

Get-Item *
Set-Location $last
exit 0
