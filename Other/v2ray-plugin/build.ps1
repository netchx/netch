Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/teddysun/v2ray-plugin -b 'v4.42.1' src
if ( -Not $? ) {
    Pop-Location
    exit $lastExitCode
}
Push-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\..\release\v2ray-plugin.exe'

Pop-Location
rm -Recurse -Force src

Pop-Location
exit $lastExitCode