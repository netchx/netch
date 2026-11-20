Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

function Delete {
    param (
        [string]
        $Path
    )

    if (Test-Path $Path) {
        rm -Recurse -Force $Path | Out-Null
    }
}

Delete '.vs'
Delete 'release'
Delete 'Netch.Core\bin'
Delete 'Netch.Core\obj'
Delete 'Netch.App\bin'
Delete 'Netch.App\obj'
Delete 'Tests\bin'
Delete 'Tests\obj'
Delete 'TestResults'
Delete 'Redirector\bin'
Delete 'Redirector\obj'
Delete 'RedirectorTester\bin'
Delete 'RedirectorTester\obj'
Delete 'RouteHelper\bin'
Delete 'RouteHelper\obj'

Delete 'Netch.Core\*.csproj.user'
Delete 'Netch.App\*.csproj.user'
Delete 'Redirector\*.vcxproj.user'
Delete 'RedirectorTester\*.csproj.user'
Delete 'RouteHelper\*.vcxproj.user'

.\other\clean.ps1

Pop-Location
exit $lastExitCode