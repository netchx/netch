Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

$Env:CGO_ENABLED="0"
$Env:GOROOT_FINAL="/usr"

$Env:GOOS="windows"
$Env:GOARCH="amd64"
go build -a -trimpath -asmflags "-s -w" -ldflags "-s -w" -o "..\release\aiodns.exe"

Pop-Location
exit $lastExitCode
