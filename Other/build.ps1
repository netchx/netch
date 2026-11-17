Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
try {
    .\clean.ps1

    $root = Split-Path $MyInvocation.MyCommand.Path -Parent
    Set-Location $root
    Get-ChildItem -Path '.' -Directory | ForEach-Object {
        $name = $_.Name

        if ( Test-Path ".\$name\build.ps1" ) {
            Write-Host "Building $name"

            $global:LASTEXITCODE = 0
            try {
                & ".\$name\build.ps1"
                if ( -Not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) ) {
                    throw "Script exited with code $LASTEXITCODE"
                }
            }
            catch {
                Write-Host "Build $name failed: $_"
                exit 1
            }
        }

        Set-Location $root
    }

    Set-Location $root
    Get-ChildItem -Path '.' -Directory | ForEach-Object {
        $name = $_.Name

        if ( Test-Path ".\$name\src" ) {
            Remove-Item -Recurse -Force ".\$name\src"
        }

        Set-Location $root
    }

    Write-Host

    Set-Location $root
    Get-ChildItem -Path '.\release' -File | ForEach-Object {
        $name = $_.Name
        $hash = (Get-FileHash ".\release\$name" -Algorithm SHA256).Hash.ToLowerInvariant()

        Write-Host "$hash $name"
    }
}
finally {
    Pop-Location
}
exit 0
