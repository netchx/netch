param (
    [string]
    $OutputPath = 'release'
)

if ( Test-Path -Path "$OutputPath" -IsValid ) {
    New-Item -ItemType Directory -Name "$OutputPath" | Out-Null
}

Push-Location "$OutputPath"
New-Item -ItemType Directory -Name 'bin'  | Out-Null
New-Item -ItemType Directory -Name 'mode' | Out-Null
New-Item -ItemType Directory -Name 'i18n' | Out-Null

# Get-ChildItem -Path '..\scripts\deps\main' -File | ForEach-Object {
#     $name=$_.Name

#     Write-Host "Executing $name"
#     & "..\scripts\deps\base\$name"
#     if ( -Not $? ) {
#         Pop-Location
#         exit $lastExitCode
#     }
# }

Get-ChildItem -Path '..\scripts\deps' -File | ForEach-Object {
    $name=$_.Name

    Write-Host "Executing $name"
    & "..\scripts\deps\$name" -OutputPath 'bin'
    if ( -Not $? ) {
        Pop-Location
        exit $lastExitCode
    }
}

Pop-Location
exit 0