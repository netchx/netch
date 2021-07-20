Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

if (Test-Path 'release') {
    rm -Recurse -Force 'release'
}

Pop-Location
exit 0
