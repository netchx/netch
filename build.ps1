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

.\download.ps1 $OutputPath

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

if ($lastExitCode) { exit $lastExitCode } 
exit 0
