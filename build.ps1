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

Remove-Item -Recurse -Force $OutputPath -Confirm:$false -ErrorAction Ignore

.\scripts\download.ps1 $OutputPath

Write-Host "Building $Configuration to $OutputPath"
dotnet publish `
	-c $Configuration `
	-r 'win-x64' `
	-p:Platform='x64' `
	-p:SelfContained=$SelfContained `
	$('--self-contained', '--no-self-contained')[$SelfContained] `
	-p:PublishTrimmed=$False `
	-p:PublishReadyToRun=$PublishReadyToRun `
	-p:PublishSingleFile=$PublishSingleFile `
	-p:IncludeNativeLibrariesForSelfExtract=$SelfContained `
	-o $OutputPath `
	'.\Netch\Netch.csproj'
exit $lastExitCode