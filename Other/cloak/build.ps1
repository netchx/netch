Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/cbeuw/Cloak -b 'v2.5.5' src
if ( -Not $? ) {
    Pop-Location
    exit $lastExitCode
}

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w' -o '..\release\ck-client.exe' '.\src\cmd\ck-client'

Pop-Location
exit $lastExitCode