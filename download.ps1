$NetchDataURL="https://github.com/NetchX/NetchData/archive/refs/heads/master.zip"
$NetchModeURL="https://github.com/NetchX/NetchMode/archive/refs/heads/master.zip"
$NetchI18NURL="https://github.com/NetchX/NetchTranslation/archive/refs/heads/master.zip"

New-Item -ItemType Directory -Name release | Out-Null
Set-Location release

Invoke-WebRequest -Uri $NetchDataURL -OutFile data.zip
Invoke-WebRequest -Uri $NetchModeURL -OutFile mode.zip
Invoke-WebRequest -Uri $NetchI18NURL -OutFile i18n.zip

Expand-Archive -Force -Path data.zip -DestinationPath .
Expand-Archive -Force -Path mode.zip -DestinationPath .
Expand-Archive -Force -Path i18n.zip -DestinationPath .

New-Item -ItemType Directory -Name bin  | Out-Null
New-Item -ItemType Directory -Name mode | Out-Null
New-Item -ItemType Directory -Name i18n | Out-Null

Copy-Item -Recurse -Force .\NetchData-master\*             .\bin
Copy-Item -Recurse -Force .\NetchMode-master\mode\*        .\mode
Copy-Item -Recurse -Force .\NetchTranslation-master\i18n\* .\i18n

Remove-Item -Recurse -Force NetchData-master
Remove-Item -Recurse -Force NetchMode-master
Remove-Item -Recurse -Force NetchTranslation-master
Remove-Item -Force data.zip
Remove-Item -Force mode.zip
Remove-Item -Force i18n.zip

Get-Item *
exit 0
