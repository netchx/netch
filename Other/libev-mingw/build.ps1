Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/libev -b 'mingw' src
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

Pop-Location
rm -Recurse -Force src

Pop-Location
exit $lastExitCode