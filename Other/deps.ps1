Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

.\clean.ps1

Get-ChildItem -Path '.' -Directory | ForEach-Object {
    Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

    $name=$_.Name

    if ( Test-Path ".\$name\deps.ps1" ) {
        & ".\$name\deps.ps1"

        if ( -Not $? ) {
            exit $lastExitCode
        }
    }
}

Pop-Location
exit 0
