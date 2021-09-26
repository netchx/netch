Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/simple-obfs src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

msys2 ..\build.sh
if ( -Not $? ) {
    exit $lastExitCode
}

cp -Force '.\simple-obfs.exe' '..\..\release\simple-obfs.exe'
exit 0