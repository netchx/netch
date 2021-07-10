$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)
$last=(Get-Location).Path

Set-Location $exec

$Env:CGO_ENABLED="0"
$Env:GOROOT_FINAL="/usr"

$Env:GOOS="windows"
$Env:GOARCH="amd64"
go build -a -trimpath -asmflags "-s -w" -ldflags "-s -w" -o "..\release\tap2socks.exe"

Set-Location $last
exit $lastExitCode
