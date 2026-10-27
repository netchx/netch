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
	$PublishSingleFile = $True,

	[Parameter()]
	[bool]
	$PublishReadyToRun = $False,

	# Reuse existing component outputs (Netch\bin, Redirector\bin, RouteHelper\bin)
	# instead of building them. CI restores those folders from a cache keyed by
	# the source hash; local builds should always compile so a stale binary is
	# never packaged.
	[Parameter()]
	[switch]
	$UseCache
)

$ErrorActionPreference = 'Stop'

function Invoke-Native {
	param (
		[Parameter(Mandatory)]
		[scriptblock]
		$Command,

		[Parameter(Mandatory)]
		[string]
		$Name
	)

	& $Command
	if ($LASTEXITCODE -ne 0) {
		throw "$Name failed with exit code $LASTEXITCODE"
	}
}

function Test-ShouldBuild {
	param ([string] $Path)

	return (-Not $UseCache) -or (-Not (Test-Path $Path))
}

Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
try {
	if ( Test-Path -Path $OutputPath ) {
		Remove-Item -Recurse -Force $OutputPath
	}
	New-Item -ItemType Directory -Name $OutputPath | Out-Null

	Push-Location $OutputPath
	try {
		New-Item -ItemType Directory -Name 'bin' | Out-Null
		Copy-Item -Recurse -Force '..\Storage\i18n' '.'
		Copy-Item -Recurse -Force '..\Storage\mode' '.'
		Copy-Item -Force '..\Storage\stun.txt' 'bin'
		Copy-Item -Force '..\Storage\nfdriver.sys' 'bin'
		Copy-Item -Force '..\Storage\aiodns.conf' 'bin'
		Copy-Item -Force '..\Storage\tun2socks-windows-amd64-v3.exe' 'bin'
		Copy-Item -Force '..\Storage\README.md' 'bin'

		# The GeoIP database is only used to show the exit country. A network
		# hiccup should not fail the whole build when a local copy is available.
		$geoIpTarget = 'bin\GeoLite2-Country.mmdb'
		$geoIpFallback = '..\Storage\GeoLite2-Country.mmdb'
		try {
			Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/Loyalsoldier/geoip/release/Country.mmdb' -OutFile $geoIpTarget
		}
		catch {
			if (Test-Path $geoIpFallback) {
				Write-Warning "Download GeoIP database failed, using $geoIpFallback"
				Copy-Item -Force $geoIpFallback $geoIpTarget
			}
			else {
				Write-Warning "Download GeoIP database failed and no local copy exists: $_"
			}
		}
	}
	finally {
		Pop-Location
	}

	$xrayBinary = '.\Other\release\xray.exe'
	$xrayCurrent = (Test-Path $xrayBinary) -and ((Get-FileHash $xrayBinary -Algorithm SHA256).Hash.ToLowerInvariant() -eq 'c77a00eb84fb93ae044c2d771a1cabcc033b81e487253fbc35b12df6879c1e15')
	if ( -Not $xrayCurrent -or -Not ( Test-Path '.\Other\release\v2ray-sn.exe' ) ) {
		# The core build scripts handle their own errors; do not let the strict
		# preference of this script turn their non-fatal messages into failures.
		$previousPreference = $ErrorActionPreference
		$ErrorActionPreference = 'Continue'
		try {
			& .\Other\build.ps1
		}
		finally {
			$ErrorActionPreference = $previousPreference
		}
		if ($LASTEXITCODE -ne 0) {
			throw "Building proxy cores failed with exit code $LASTEXITCODE"
		}
	}
	Copy-Item -Force '.\Other\release\*.bin' "$OutputPath\bin"
	Copy-Item -Force '.\Other\release\*.dll' "$OutputPath\bin"
	Copy-Item -Force '.\Other\release\*.exe' "$OutputPath\bin"

	# Copy the bundled wintun.dll last so the version kept in Storage is not
	# replaced by the one downloaded by Other\wintun.
	Copy-Item -Force '.\Storage\wintun.dll' "$OutputPath\bin"

	if ( Test-ShouldBuild ".\Netch\bin\$Configuration" ) {
		Write-Host
		Write-Host 'Building Netch'

		Invoke-Native -Name 'dotnet publish' -Command {
			dotnet publish `
				-c $Configuration `
				-r 'win-x64' `
				-p:Platform='x64' `
				-p:SelfContained=$SelfContained `
				-p:PublishSingleFile=$PublishSingleFile `
				-p:PublishReadyToRun=$PublishReadyToRun `
				-p:PublishReadyToRunShowWarnings=$PublishReadyToRun `
				-p:IncludeNativeLibrariesForSelfExtract=$SelfContained `
				-o ".\Netch\bin\$Configuration" `
				'.\Netch\Netch.csproj'
		}
	}
	Copy-Item -Force ".\Netch\bin\$Configuration\Netch.exe" $OutputPath

	if ( Test-ShouldBuild ".\Redirector\bin\$Configuration" ) {
		Write-Host
		Write-Host 'Building Redirector'

		Invoke-Native -Name 'msbuild Redirector' -Command {
			msbuild `
				-nologo `
				-property:Configuration=$Configuration `
				-property:Platform=x64 `
				'.\Redirector\Redirector.vcxproj'
		}
	}
	Copy-Item -Force ".\Redirector\bin\$Configuration\nfapi.dll"      "$OutputPath\bin"
	Copy-Item -Force ".\Redirector\bin\$Configuration\Redirector.bin" "$OutputPath\bin"

	if ( Test-ShouldBuild ".\RouteHelper\bin\$Configuration" ) {
		Write-Host
		Write-Host 'Building RouteHelper'

		Invoke-Native -Name 'msbuild RouteHelper' -Command {
			msbuild `
				-nologo `
				-property:Configuration=$Configuration `
				-property:Platform=x64 `
				'.\RouteHelper\RouteHelper.vcxproj'
		}
	}
	Copy-Item -Force ".\RouteHelper\bin\$Configuration\RouteHelper.bin" "$OutputPath\bin"

	if ( $Configuration.Equals('Release') ) {
		Remove-Item -Force "$OutputPath\*.pdb" -ErrorAction SilentlyContinue
		Remove-Item -Force "$OutputPath\*.xml" -ErrorAction SilentlyContinue
	}
}
catch {
	Write-Host "Build failed: $_" -ForegroundColor Red
	exit 1
}
finally {
	Pop-Location
}

exit 0
