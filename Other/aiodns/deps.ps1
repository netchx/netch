$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

rm -Force go.*
go mod init aiodns
go mod tidy

Pop-Location
exit $lastExitCode
