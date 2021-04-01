param (
	[Alias('c')]
	[Parameter()]
	[ValidateSet('Debug', 'Release')]
	[string]
	$Configuration = 'Release',

	[Alias('o')]
	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]
	$OutputPath = 'Netch\bin\Publish\',

	[Parameter()]
	[bool]
	$SelfContained = $False,

	[Parameter()]
	[bool]
	$PublishReadyToRun = $False
)

$PublishSingleFile = $True

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

if ($LASTEXITCODE) { exit $LASTEXITCODE } 

Write-Host 'Build done'
