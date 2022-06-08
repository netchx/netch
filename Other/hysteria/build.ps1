Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/HyNetwork/hysteria -b 'v1.0.4' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src/cmd

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w -buildid=' -o '..\..\..\release\hysteria.exe'
exit $lastExitCode