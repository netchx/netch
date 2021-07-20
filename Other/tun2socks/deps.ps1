Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

rm -Force go.*
go mod init tun2socks
go mod tidy

Pop-Location
exit $lastExitCode
