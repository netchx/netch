Write-Host 'Building'

dotnet publish `
	-c "Release" `
	-r "win-x64" `
	-p:Platform="x64" `
	-p:PublishSingleFile=true `
	-p:SelfContained=false `
	-p:PublishTrimmed=false `
	-p:PublishReadyToRun=false `
	-o Netch\bin\Publish\ `
	Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
