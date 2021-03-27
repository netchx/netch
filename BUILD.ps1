Write-Host 'Building'

dotnet build -p:Configuration="Release" `
	-restore `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
