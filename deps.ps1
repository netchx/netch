param (
    [string]
    $OutputPath = 'release'
)

if ( -Not ( Test-Path -Path "$OutputPath" ) ) {
    New-Item -ItemType Directory -Name "$OutputPath" | Out-Null
}

Push-Location "$OutputPath"
New-Item -ItemType Directory -Name 'bin'  | Out-Null
New-Item -ItemType Directory -Name 'mode' | Out-Null
New-Item -ItemType Directory -Name 'i18n' | Out-Null
Pop-Location
exit 0