Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/v2fly/v2ray-core -b 'v5.0.6' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w -buildid=' -o '..\..\release\v2ray.exe' '.\main'
exit $lastExitCode