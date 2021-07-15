$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

rm -Force go.*
go mod init tun2socks
go mod tidy

Pop-Location
exit $lastExitCode
