Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

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

exit 0
