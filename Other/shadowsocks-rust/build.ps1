Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/shadowsocks/shadowsocks-rust -b 'v1.15.0-alpha.4' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

cargo build --features logging,trust-dns,local,utility,local-http,local-tunnel,local-socks4,multi-threaded,stream-cipher,aead-cipher-2022 --release
if ( -Not $? ) {
    exit $lastExitCode
}

cp -Force '.\target\release\sslocal.exe' '..\..\release\Shadowsocks.exe'
exit 0