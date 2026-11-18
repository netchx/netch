Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

go mod tidy

Pop-Location
exit $lastExitCode
