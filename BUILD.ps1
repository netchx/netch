Write-Host 'Building'

dotnet build `
	-c "Release" `
	-p:Platform="x64" `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
