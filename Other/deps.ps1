$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

.\clean.ps1

Get-ChildItem -Path . -Directory | ForEach-Object {
    $name=$_.Name
    & ".\$name\deps.ps1"

    if (-Not $?) {
        exit $lastExitCode
    }
}

Pop-Location
exit 0
