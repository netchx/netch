param (
	[Parameter()]
	[ValidateSet('Debug', 'Release')]
	[string]
	$Configuration = 'Release',

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]
	$OutputPath = 'release',

	[Parameter()]
	[bool]
	$SelfContained = $True,

	[Parameter()]
	[bool]
	$PublishReadyToRun = $False,

	[Parameter()]
	[bool]
	$PublishSingleFile = $True
)

Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

if ( Test-Path -Path "$OutputPath" ) {
    rm -Recurse -Force "$OutputPath"
}
New-Item -ItemType Directory -Name "$OutputPath" | Out-Null

.\deps.ps1 "$OutputPath"
if ( -Not $? ) { exit $lastExitCode }

.\other\build.ps1
if ( -Not $? ) { exit $lastExitCode }

Write-Host
Write-Host 'Building Netch'
dotnet publish `
	-c "$Configuration" `
	-r 'win-x64' `
	-p:Platform='x64' `
	-p:SelfContained="$SelfContained" `
	-p:PublishTrimmed="$SelfContained" `
	-p:PublishReadyToRun="$PublishReadyToRun" `
	-p:PublishSingleFile="$PublishSingleFile" `
	-p:IncludeNativeLibrariesForSelfExtract="$SelfContained" `
	-o "$OutputPath" `
	'.\Netch\Netch.csproj'
if ( -Not $? ) { exit $lastExitCode }

Write-Host
Write-Host 'Building Redirector'
msbuild `
	-property:Configuration="$Configuration" `
	-property:Platform=x64 `
	'.\Redirector\Redirector.vcxproj'
if ( -Not $? ) { exit $lastExitCode }

Write-Host
Write-Host 'Building RouteHelper'
msbuild `
	-property:Configuration="$Configuration" `
	-property:Platform=x64 `
	'.\RouteHelper\RouteHelper.vcxproj'
if ( -Not $? ) { exit $lastExitCode }

cp -Force '.\other\release\*.bin'             "$OutputPath\bin"
cp -Force '.\other\release\*.exe'             "$OutputPath\bin"
cp -Force '.\Redirector\bin\Redirector.bin'   "$OutputPath\bin"
cp -Force '.\RouteHelper\bin\RouteHelper.bin' "$OutputPath\bin"

if ( $Configuration.Equals('Release') ) {
	rm -Force "$OutputPath\*.pdb"
	rm -Force "$OutputPath\*.xml"
}

Pop-Location
exit 0