$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)
$last=(Get-Location).Path

Set-Location $exec

.\clean.ps1

gcc -v
go  version

Get-ChildItem -Path . -Directory | ForEach-Object {
    $name=$_.Name

    Write-Host "Building $name"
    & ".\$name\build.ps1"

    if ( -Not $? ) {
        Write-Host "Build $name failed"
        exit $lastExitCode
    }
}

Set-Location $last
exit 0
