Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/AdguardTeam/dnsproxy -b 'v0.39.9' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\..\release\dnsproxy.exe'
exit $lastExitCode