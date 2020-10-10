Write-Host 'Building'

msbuild -v:n -m:1 /p:Configuration="Release" `
	/p:Platform="x64" `
	/p:TargetFramework=net48 `
	/p:SolutionDir="..\" `
	/restore `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
