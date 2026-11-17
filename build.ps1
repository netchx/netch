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

function Resolve-MSBuild {
	# Prefer msbuild from PATH (Developer PowerShell); otherwise locate the
	# Visual Studio installation with vswhere so a plain PowerShell works too.
	$command = Get-Command 'msbuild' -ErrorAction SilentlyContinue
	if ($command) {
		return $command.Source
	}

	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
	if (Test-Path $vswhere) {
		$found = & $vswhere -latest -prerelease -products * `
			-requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
			-find 'MSBuild\**\Bin\amd64\MSBuild.exe' | Select-Object -First 1
		if (-Not $found) {
			$found = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild `
				-find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
		}
		if ($found) {
			return $found
		}
	}

	throw 'MSBuild not found. Install Visual Studio 2022 with "Desktop development with C++", or run from Developer PowerShell for VS.'
}

function Resolve-PlatformToolset {
	param (
		[Parameter(Mandatory)]
		[string]
		$MSBuildPath
	)

	$dir = Split-Path $MSBuildPath -Parent
	for ($i = 0; $i -lt 5 -and $dir; $i++) {
		$vcRoot = Join-Path $dir 'Microsoft\VC'
		if (Test-Path $vcRoot) {
			$toolsets = Get-ChildItem -Path $vcRoot -Directory -Recurse -Filter 'v14*' -ErrorAction SilentlyContinue |
				Where-Object { $_.FullName -match '\\Platforms\\x64\\PlatformToolsets\\v14\d+$' } |
				Select-Object -ExpandProperty Name -Unique |
				Sort-Object -Descending
			if ($toolsets -contains 'v143') {
				return $null
			}
			if ($toolsets) {
				return $toolsets | Select-Object -First 1
			}
		}
		$dir = Split-Path $dir -Parent
	}

	return $null
}

function Test-OtherCoresCurrent {
	$expectedHashes = @{
		'.\Other\release\aiodns.bin'                      = $null
		'.\Other\release\pcap2socks.exe'                  = 'de84f4a32c8c9d4888197345d52ba793e4bdf510614852e76f336f627e43def3'
		'.\Other\release\tun2socks-windows-amd64-v3.exe'  = 'bc5827ca00c7f312125365e8669794fffdbdf1cb0efa948cddd94a0c800c8c31'
		'.\Other\release\v2ray-sn.exe'                    = '62f1df5d02aa2cfb5d1f15828d3561c415a7687a2efe869ab38e6ce9a0c135d6'
		'.\Other\release\wintun.dll'                      = 'e5da8447dc2c320edc0fc52fa01885c103de8c118481f683643cacc3220dafce'
		'.\Other\release\xray.exe'                        = 'c77a00eb84fb93ae044c2d771a1cabcc033b81e487253fbc35b12df6879c1e15'
	}

	foreach ($entry in $expectedHashes.GetEnumerator()) {
		if (-Not (Test-Path $entry.Key)) {
			return $false
		}
		if ($entry.Value) {
			$actual = (Get-FileHash $entry.Key -Algorithm SHA256).Hash.ToLowerInvariant()
			if ($actual -ne $entry.Value) {
				return $false
			}
		}
	}

	return $true
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

	if (-Not (Test-OtherCoresCurrent)) {
		# Scripts extracted from a downloaded archive carry the "downloaded from
		# the internet" mark, which a RemoteSigned execution policy refuses to
		# run. They are part of this source tree, so clear the mark first.
		Get-ChildItem -Path '.\Other' -Filter '*.ps1' -Recurse | Unblock-File

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

	$needsNative = (Test-ShouldBuild ".\Redirector\bin\$Configuration") -or (Test-ShouldBuild ".\RouteHelper\bin\$Configuration")
	$msbuildArgs = @()
	if ($needsNative) {
		$msbuild = Resolve-MSBuild
		Write-Host "Using MSBuild: $msbuild"
		$toolset = Resolve-PlatformToolset -MSBuildPath $msbuild
		if ($toolset) {
			Write-Host "Using detected PlatformToolset override: $toolset"
			$msbuildArgs += "-property:PlatformToolset=$toolset"
		}
	}

	if ( Test-ShouldBuild ".\Redirector\bin\$Configuration" ) {
		Write-Host
		Write-Host 'Building Redirector'

		Invoke-Native -Name 'msbuild Redirector' -Command {
			& $msbuild `
				-nologo `
				-property:Configuration=$Configuration `
				-property:Platform=x64 `
				@msbuildArgs `
				'.\Redirector\Redirector.vcxproj'
		}
	}
	Copy-Item -Force ".\Redirector\bin\$Configuration\nfapi.dll"      "$OutputPath\bin"
	Copy-Item -Force ".\Redirector\bin\$Configuration\Redirector.bin" "$OutputPath\bin"

	if ( Test-ShouldBuild ".\RouteHelper\bin\$Configuration" ) {
		Write-Host
		Write-Host 'Building RouteHelper'

		Invoke-Native -Name 'msbuild RouteHelper' -Command {
			& $msbuild `
				-nologo `
				-property:Configuration=$Configuration `
				-property:Platform=x64 `
				@msbuildArgs `
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
