Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

rm -Force go.*
go mod init tap2socks
go mod tidy

Pop-Location
exit $lastExitCode
