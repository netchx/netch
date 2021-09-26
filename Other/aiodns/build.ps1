Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

$Env:CGO_ENABLED='1'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -buildmode=c-shared -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\release\aiodns.bin'
exit $lastExitCode
