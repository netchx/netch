Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/shadowsocks-rust -b 'v1.11.2' src
if ( -Not $? ) {
    Pop-Location
    exit $lastExitCode
}
Push-Location src

cargo build --release
if ( -Not $? ) {
    Pop-Location

    rm -Recurse -Force src
    exit $lastExitCode
}

cp -Force '.\target\release\sslocal.exe' '..\..\release\ss-local.exe'

Pop-Location
rm -Recurse -Force src

Pop-Location
exit $lastExitCode