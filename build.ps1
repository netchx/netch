param([string]$buildtfm = 'all')

Write-Host 'DotNet SDK Version'
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

    Write-Host 'Build x64 Completed, start copy bin, mode, i18n file'
	Remove-Item -Recurse -Force "$net_baseoutput\x64\Release\win-x64\bin\tap-driver"
	Copy-Item -Recurse "$mainDir\binaries\*" "$net_baseoutput\x64\Release\win-x64\bin"
	Copy-Item -Recurse "$mainDir\modes\mode\*" "$net_baseoutput\x64\Release\win-x64\mode"
	Copy-Item -Recurse "$mainDir\translations\i18n\*" "$net_baseoutput\x64\Release\win-x64\i18n"

    Write-Host 'Build done'
}

cd $mainDir\Netch
Build-NetFrameworkx64
cd $mainDir

exit 0
