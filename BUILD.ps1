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

	msbuild -v:n -m:1 /p:Configuration="Release" `
		/p:Platform="x64" `
		/p:TargetFramework=net48 `
		/p:Runtimeidentifier=win-x64 `
		/restore
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE } 

    Write-Host 'Build done'
}

cd $mainDir
Build-NetFrameworkx64
cd $mainDir

exit 0
