Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

.\clean.ps1

Get-ChildItem -Path . -Directory | ForEach-Object {
    $name=$_.Name
    & ".\$name\deps.ps1"

    if (-Not $?) {
        exit $lastExitCode
    }
}

exit 0
