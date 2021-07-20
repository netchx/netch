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

Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

if (Test-Path -Path "$OutputPath") {
    Remove-Item -Recurse -Force "$OutputPath"
}

if (Test-Path -Path "$OutputPath" -IsValid) {
    New-Item -ItemType Directory -Name "$OutputPath" | Out-Null
}

.\deps.ps1 "$OutputPath"
if ( -Not $? ) { exit $lastExitCode }

.\other\build.ps1
if ( -Not $? ) { exit $lastExitCode }

if (Test-Path '.\other\release') {
	Copy-Item -Recurse -Force '.\other\release\*' "$OutputPath\bin"
}

Write-Host
Write-Host 'Building Netch'
dotnet publish `
	-c $Configuration `
	-r 'win-x64' `
	-p:Platform='x64' `
	-p:PublishSingleFile="$PublishSingleFile" `
	-p:SelfContained="$SelfContained" `
	-p:PublishTrimmed="$SelfContained" `
	-p:PublishReadyToRun="$PublishReadyToRun" `
	-o "$OutputPath" `
	'.\Netch\Netch.csproj'
if ( -Not $? ) { exit $lastExitCode }

Write-Host
Write-Host 'Building Redirector'
msbuild `
	-property:Configuration=Release `
	-property:Platform=x64 `
	'.\Redirector\Redirector.vcxproj'
if ( -Not $? ) { exit $lastExitCode }

Copy-Item -Force 'Redirector\bin\Redirector.bin' "$OutputPath\bin"

if ( $Configuration.Equals('Release') ) {
	Remove-Item -Force "$OutputPath\*.pdb"
	Remove-Item -Force "$OutputPath\*.xml"
}

Pop-Location
exit 0