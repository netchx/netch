$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)
$last=(Get-Location).Path

Set-Location $exec

.\clean.ps1

Get-ChildItem -Path . -Directory | ForEach-Object {
    $name=$_.Name
    & ".\$name\deps.ps1"

    if (-Not $?) {
        exit $lastExitCode
    }
}

Set-Location $last
exit 0
