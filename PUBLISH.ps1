Write-Host 'Building'

dotnet publish -c "Release" `
	-p:Platform="x64" `
	-p:PublishSingleFile=true `
	-p:RuntimeIdentifier=win-x64 `
	-p:SolutionDir="$pwd\" `
	-o Netch\bin\Publish\ `
	--no-self-contained `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
