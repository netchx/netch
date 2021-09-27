Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

.\clean.ps1

Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
Get-ChildItem -Path '.' -Directory | ForEach-Object {
    $name=$_.Name

    if ( Test-Path ".\$name\build.ps1" ) {
        Write-Host "Building $name"

        & ".\$name\build.ps1"
        if ( -Not $? ) {
            Write-Host "Build $name failed"
            exit $lastExitCode
        }
    }

    Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
}

Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
Get-ChildItem -Path '.' -Directory | ForEach-Object {
    $name=$_.Name

    if ( Test-Path ".\$name\src" ) {
        rm -Recurse -Force ".\$name\src"
    }

    Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
}

Write-Host

Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
Get-ChildItem -Path '.\release' -File | ForEach-Object {
    $name=$_.Name
    $hash=(Get-FileHash ".\release\$name" -Algorithm SHA256).Hash.ToLower()

    Write-Host "$hash $name"
}

Pop-Location
exit 0
