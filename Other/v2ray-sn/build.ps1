Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

git clone https://github.com/SagerNet/v2ray-core.git -b 'v5.0.16' src
if ( -Not $? ) {
    exit $lastExitCode
}
Set-Location src

# Download SSR plugin
Invoke-WebRequest -Uri 'https://gist.githubusercontent.com/H1JK/b3165a99b635dcc06101690e4c43b5fd/raw/691b471f3b395a949d03a3d064d93d319d4997b7/ssr.go' -OutFile '.\proxy\shadowsocks\plugin\self\ssr.go'

# Download Simple-Obfs plugin
Invoke-WebRequest -Uri 'https://gist.githubusercontent.com/H1JK/b3165a99b635dcc06101690e4c43b5fd/raw/691b471f3b395a949d03a3d064d93d319d4997b7/obfs.go' -OutFile '.\proxy\shadowsocks\plugin\self\obfs.go'

# Enable ReadV (Use old ReadV code)
Remove-Item '.\common\buf\io.go'
Remove-Item '.\common\buf\readv_reader.go'
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/SagerNet/v2ray-core/2711fd1/common/buf/io.go' -OutFile '.\common\buf\io.go'
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/SagerNet/v2ray-core/2711fd1/common/buf/readv_reader.go' -OutFile '.\common\buf\readv_reader.go'

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
go get -u ./...
go mod tidy
go build -a -trimpath -asmflags '-s -w' -ldflags '-s -w -buildid=' -o '..\..\release\v2ray-sn.exe' '.\main'
exit $lastExitCode
