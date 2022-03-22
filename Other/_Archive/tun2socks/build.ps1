Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/xjasonlyu/tun2socks -b 'v2.3.2' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\..\release\tun2socks.exe'
exit $lastExitCode