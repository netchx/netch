Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

rm -Force go.*
go mod init aiodns
go mod tidy

Pop-Location
exit $lastExitCode
