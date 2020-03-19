# REM The reason we don't use dotnet build is that dotnet build doesn't support COM references yet https://github.com/microsoft/msbuild/issues/3986
param([string]$buildtfm = 'all')

Write-Host 'dotnet SDK version'
dotnet --version

$exe = 'ShadowsocksR.exe'
$net_tfm = 'net48'
$netcore_tfm = 'netcoreapp3.1'
$configuration = 'Release'
$mainDir = (Get-Item -Path ".\").FullName
$net_baseoutput = "$mainDir\shadowsocks-csharp\bin\$configuration"
$apphostpatcher_dir = "$mainDir\AppHostPatcher"

Write-Host $mainDir
Write-Host $net_baseoutput
Write-Host $apphostpatcher_dir

$buildNet     = $buildtfm -eq 'all' -or $buildtfm -eq 'net'

function Build-AppHostPatcher
{
	Write-Host Building AppHostPatcher
	
	$outdir = "$apphostpatcher_dir\bin\$configuration\$netcore_tfm"
	$publishDir = "$outdir\publish"
	
	Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore
	
	msbuild -v:m -m -r -t:Publish -p:Configuration=$configuration -p:TargetFramework=$netcore_tfm
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }
}

function Build-NetFramework
{
	Write-Host 'Building .NET Framework x86 and x64'
	
	$outdir = "$net_baseoutput\$net_tfm"
	
	msbuild -v:m -m -r -t:Build -p:Configuration=$configuration -p:TargetFramework=$net_tfm
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }
}

function Build-NetCore
{
	Write-Host 'Building .NET Core'
	
	$outdir = "$net_baseoutput\$netcore_tfm"
	$publishDir = "$outdir\publish"

	Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore
	
	msbuild -v:m -m -r -t:Publish -p:Configuration=$configuration -p:TargetFramework=$netcore_tfm
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }

	$tmpbin = 'tmpbin'
	Rename-Item $publishDir $tmpbin
	New-Item -ItemType Directory $publishDir > $null
	Move-Item $outdir\$tmpbin $publishDir
	Rename-Item $publishDir\$tmpbin bin

	Move-Item $publishDir\bin\$exe $publishDir
	& $apphostpatcher_dir\bin\$configuration\$netcore_tfm\AppHostPatcher.exe $publishDir\$exe -d bin
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }
}

function Build-NetCoreSelfContained
{
	param([string]$arch)
	Write-Host "Building .NET Core $arch"

	$rid = "win-$arch"
	$outdir = "$net_baseoutput\$netcore_tfm\$rid"
	$publishDir = "$outdir\publish"

	Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore
	
	msbuild -v:m -m -r -t:Publish -p:Configuration=$configuration -p:TargetFramework=$netcore_tfm -p:RuntimeIdentifier=$rid -p:SelfContained=True -p:PublishReadyToRun=True
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }
	
	$tmpbin = 'tmpbin'
	Rename-Item $publishDir $tmpbin
	New-Item -ItemType Directory $publishDir > $null
	Move-Item $outdir\$tmpbin $publishDir
	Rename-Item $publishDir\$tmpbin bin
	
	Move-Item $publishDir\bin\$exe $publishDir
	& $apphostpatcher_dir\bin\$configuration\$netcore_tfm\AppHostPatcher.exe $publishDir\$exe -d bin
	if ($LASTEXITCODE) { cd $mainDir ; exit $LASTEXITCODE }
}

if ($buildCore -or $buildCoreX86 -or $buildCoreX64)
{
	cd $apphostpatcher_dir
	Build-AppHostPatcher
}

cd $mainDir\shadowsocks-csharp

if ($buildNet) {
	Build-NetFramework
}

if ($buildCore) {
	Build-NetCore
}

if ($buildCoreX86) {
	Build-NetCoreSelfContained x86
}

if ($buildCoreX64) {
	Build-NetCoreSelfContained x64
}

cd $mainDir