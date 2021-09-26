Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/libev -b 'mingw' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

msys2 ..\build.sh
exit $lastExitCode