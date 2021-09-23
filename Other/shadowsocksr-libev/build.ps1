Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocksrr/shadowsocksr-libev -b 'Akkariiin/develop' src
if ( -Not $? ) {
    Pop-Location
    exit $lastExitCode
}
Push-Location src

msys2 ..\build.sh
if ( -Not $? ) {
    Pop-Location

    rm -Recurse -Force src
    exit $lastExitCode
}

cp -Force '.\ssr-local.exe' '..\release\ssr-local.exe'

Pop-Location
rm -Recurse -Force src

Pop-Location
exit $lastExitCode