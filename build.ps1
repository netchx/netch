# REM The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/microsoft/msbuild/issues/3986
param([string]$buildtfm = 'all')

Write-Host 'dotnet SDK version'
dotnet --version

$exe = 'Netch.exe'
$mainDir = (Get-Item -Path ".\").FullName
$net_baseoutput = "$mainDir\Netch\bin\$configuration"

Write-Host $mainDir
Write-Host $net_baseoutput

function Build-NetFrameworkx64
{
	Write-Host 'Building .NET Framework x64'
	
	$outdir = "$net_baseoutput\x64"
	
	msbuild -v:m -m -t:Build /p:Configuration="Release" /p:Platform="x64" /p:TargetFramework=net48 /p:Runtimeidentifier=win-x64 /restore
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE } 

    Write-Host 'Build x64 Complete ,Started Copy bin,mode,i18n file'
    
Copy-Item "$mainDir\translations\i18n\*" "$net_baseoutput\x64\Release\win-x64\i18n" -recurse

Copy-Item "$mainDir\modes\mode\*" "$net_baseoutput\x64\Release\win-x64\mode" -recurse

Remove-Item -path "$net_baseoutput\x64\Release\win-x64\bin\tap-driver"

Copy-Item "$mainDir\binaries\*" -destination "$net_baseoutput\x64\Release\win-x64\bin" -recurse

    Write-Host 'Netch Build ALL DONE'

}

cd $mainDir\Netch

Build-NetFrameworkx64

cd $mainDir
