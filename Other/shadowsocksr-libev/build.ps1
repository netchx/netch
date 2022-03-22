Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocksrr/shadowsocksr-libev -b 'Akkariiin/develop' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

msys2 ..\build.sh
if ( -Not $? ) {
    exit $lastExitCode
}

cp -Force '.\ss-local.exe' '..\..\release\ShadowsocksR.exe'
exit 0
