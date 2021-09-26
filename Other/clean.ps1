Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

if ( Test-Path 'build' ) {
    rm -Recurse -Force 'build'
}

if ( Test-Path 'release' ) {
    rm -Recurse -Force 'release'
}

Get-ChildItem -Path '.' -Directory | ForEach-Object {
    $name=$_.Name

    if ( Test-Path "$name\src" ) {
        rm -Recurse -Force "$name\src"
    }
}

Pop-Location
exit 0
