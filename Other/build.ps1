$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

.\clean.ps1

Get-ChildItem -Path . -Directory | ForEach-Object {
    $name=$_.Name

    Write-Host "Building $name"
    & ".\$name\build.ps1"

    if ( -Not $? ) {
        Write-Host "Build $name failed"
        exit $lastExitCode
    }
}

Write-Host

Get-ChildItem -Path .\release -File | ForEach-Object {
    $name=$_.Name
    $hash=(Get-FileHash ".\release\$name" -Algorithm SHA256).Hash.ToLower()

    Write-Host "$hash $name"
}

Pop-Location
exit 0
