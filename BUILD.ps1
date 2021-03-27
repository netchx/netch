Write-Host 'Building'

dotnet build -p:Configuration="Release" `
	-p:SolutionDir="$pwd\" `
	-restore `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
