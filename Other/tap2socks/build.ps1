$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)

Push-Location $exec

$Env:CGO_ENABLED="0"
$Env:GOROOT_FINAL="/usr"

$Env:GOOS="windows"
$Env:GOARCH="amd64"
go build -a -trimpath -asmflags "-s -w" -ldflags "-s -w" -o "..\release\tap2socks.exe"

Pop-Location
exit $lastExitCode
