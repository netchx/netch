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

Copy-Item "$mainDir\binaries\x64\*" "$net_baseoutput\x64\Release\win-x64\bin"
Copy-Item "$mainDir\binaries\x64\tap-driver" "$net_baseoutput\x64\Release\win-x64\bin\tap-driver" -recurse
Copy-Item "$mainDir\binaries\*.acl" "$net_baseoutput\x64\Release\win-x64\bin"
Copy-Item "$mainDir\binaries\*.conf" "$net_baseoutput\x64\Release\win-x64\bin"
Copy-Item "$mainDir\binaries\*.dat" "$net_baseoutput\x64\Release\win-x64\bin"
Copy-Item "$mainDir\binaries\*.exe" "$net_baseoutput\x64\Release\win-x64\bin"
Move-Item "$net_baseoutput\x64\Release\win-x64\bin\nfapinet.dll" "$net_baseoutput\x64\Release\win-x64\nfapinet.dll"
Copy-Item "$mainDir\translations\i18n" "$net_baseoutput\x64\Release\win-x64\i18n" -recurse
mkdir "$net_baseoutput\x64\Release\win-x64\mode"
Copy-Item "$mainDir\modes\mode\*.txt" "$net_baseoutput\x64\Release\win-x64\mode"

    Write-Host 'x64 ALL DONE'

}
function Build-NetFrameworkx86
{
	Write-Host 'Building .NET Framework x86'
	
	$outdir = "$net_baseoutput\x86"
	
	msbuild -v:m -m -t:Build /p:Configuration="Release" /p:Platform="x86" /p:TargetFramework=net48 /p:Runtimeidentifier=win-x86 /restore
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE } 

    Write-Host 'Build x86 Complete ,Started Copy bin,mode,i18n file'

Copy-Item "$mainDir\binaries\x86\*" "$net_baseoutput\x86\Release\win-x86\bin"
Copy-Item "$mainDir\binaries\x86\tap-driver" "$net_baseoutput\x86\Release\win-x86\bin\tap-driver" -recurse
Copy-Item "$mainDir\binaries\*.acl" "$net_baseoutput\x86\Release\win-x86\bin"
Copy-Item "$mainDir\binaries\*.conf" "$net_baseoutput\x86\Release\win-x86\bin"
Copy-Item "$mainDir\binaries\*.dat" "$net_baseoutput\x86\Release\win-x86\bin"
Copy-Item "$mainDir\binaries\*.exe" "$net_baseoutput\x86\Release\win-x86\bin"
Move-Item "$net_baseoutput\x86\Release\win-x86\bin\nfapinet.dll" "$net_baseoutput\x86\Release\win-x86\nfapinet.dll"
Copy-Item "$mainDir\translations\i18n" "$net_baseoutput\x86\Release\win-x86\i18n" -recurse
mkdir "$net_baseoutput\x86\Release\win-x86\mode"
Copy-Item "$mainDir\modes\mode\*.txt" "$net_baseoutput\x86\Release\win-x86\mode"

    Write-Host 'x86 ALL DONE'
}

cd $mainDir\Netch

Build-NetFrameworkx64
Build-NetFrameworkx86

cd $mainDir
