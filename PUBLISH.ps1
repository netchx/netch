Write-Host 'Building'

dotnet publish -c "Release" `
	-o Netch\bin\Publish\ `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
