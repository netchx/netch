Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/p4gefau1t/trojan-go -b 'v0.10.5' src
if ( -Not $? ) {
    Pop-Location
    exit $lastExitCode
}
Push-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\..\release\trojan-go.exe'

Pop-Location
Pop-Location
exit $lastExitCode