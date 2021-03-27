Write-Host 'Building'

dotnet build -p:Configuration="Release" `
	-p:Platform="x64" `
	-p:SolutionDir="$pwd\" `
	-restore `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
