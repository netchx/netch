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
	$SelfContained = $False,

	[Parameter()]
	[bool]
	$PublishReadyToRun = $False,

	[Parameter()]
	[bool]
	$PublishSingleFile = $True
)

Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

.\Other\build.ps1
if ( -Not $? ) { exit $lastExitCode }

.\scripts\download.ps1 $OutputPath
if ( -Not $? ) { exit $lastExitCode }

Write-Host "Building $Configuration to $OutputPath"
dotnet publish `
	-c $Configuration `
	-r "win-x64" `
	-p:Platform="x64" `
	-p:PublishSingleFile=$PublishSingleFile `
	-p:SelfContained=$SelfContained `
	-p:PublishTrimmed=$SelfContained `
	-p:PublishReadyToRun=$PublishReadyToRun `
	-o $OutputPath `
	Netch\Netch.csproj

exit $lastExitCode
