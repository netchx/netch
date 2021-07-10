Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

function Delete {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Recurse -Force $Path | Out-Null
    }
}

Delete ".vs"
Delete "release"
Delete "Netch\bin"
Delete "Netch\obj"
Delete "Tests\bin"
Delete "Tests\obj"
Delete "TestResults"

.\Other\clean.ps1
exit $lastExitCode
