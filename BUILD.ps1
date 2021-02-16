Write-Host 'Building'

msbuild -v:n /p:Configuration=Release `
/p:Platform=x64 `
/p:TargetFramework=net48 `
/p:SolutionDir=$pwd\ `
/restore `
Netch\Netch.csproj

msbuild -v:n /t:Publish /p:Configuration=Release `
/p:Platform=x64 `
/p:PublishSingleFile=true `
/p:RuntimeIdentifier=win-x64 `
/p:SolutionDir=$pwd\ `
/p:SelfContained=false `
/p:PublishReadyToRun=true `
/p:TargetFramework=net5.0-windows `
/restore `
Netch\Netch.csproj

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
