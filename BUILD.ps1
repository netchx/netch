Write-Host 'Building'

msbuild -v:n /p:Configuration="Release" `
	/p:Platform="x64" `
	/p:TargetFramework=net48 `
	/p:SolutionDir="$pwd\" `
	/restore `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
