Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/simple-obfs src
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

cp -Force '.\src\obfs-local.exe' '..\..\release\simple-obfs.exe'

Pop-Location
rm -Recurse -Force src

Pop-Location
exit $lastExitCode