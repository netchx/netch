function Delete {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Recurse -Force $Path | Out-Null
    }
}

Delete ".vs"
Delete "Netch\bin"
Delete "Netch\obj"
Delete "Tests\bin"
Delete "Tests\obj"
Delete "TestResults"
exit 0
