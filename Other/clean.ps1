$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

if (Test-Path release) {
    Remove-Item -Recurse -Force release
}

Pop-Location
exit 0
