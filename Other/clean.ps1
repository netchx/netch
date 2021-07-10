Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

if (Test-Path release) {
    Remove-Item -Recurse -Force release
}

exit 0
