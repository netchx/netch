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

	# Reuse existing publish and native outputs
	# instead of building them. CI restores those folders from a cache keyed by
	# the source hash; local builds should always compile so a stale binary is
	# never packaged.
	[Parameter()]
	[switch]
	$UseCache,

	[Parameter()]
	[string]
	$Version = ''
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

	throw 'MSBuild not found. Install Visual Studio 2026 with "Desktop development with C++", or run from Developer PowerShell for VS.'
}

function Test-OtherCoresCurrent {
	$expectedHashes = @{
		'.\Other\release\aiodns.bin'                      = $null
		'.\Other\release\pcap2socks.exe'                  = 'de84f4a32c8c9d4888197345d52ba793e4bdf510614852e76f336f627e43def3'
		'.\Other\release\tun2socks-windows-amd64-v3.exe'  = 'f08d327819f722da3ee3f591cb7e9e36b06640a92b3f63f5073a454be29be35f'
		'.\Other\release\wintun.dll'                      = 'e5da8447dc2c320edc0fc52fa01885c103de8c118481f683643cacc3220dafce'
		'.\Other\release\xray.exe'                        = '1ca7a2cf61ba2c5d20b21a80d405b57933939751081cb289f07bd8ddf7cc3185'
		'.\Other\release\mihomo.exe'                      = $null
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
	param ([string[]] $Files)

	return (-Not $UseCache) -or ((Test-Path -LiteralPath $Files -PathType Leaf) -contains $false)
}

Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)
try {
	if (-not $PublishSingleFile) {
		throw 'WinUI portable builds require PublishSingleFile.'
	}

	if ( Test-Path -Path $OutputPath ) {
		Remove-Item -Recurse -Force $OutputPath
	}
	New-Item -ItemType Directory -Name $OutputPath | Out-Null

	Push-Location $OutputPath
	try {
		New-Item -ItemType Directory -Name 'bin' | Out-Null
		Copy-Item -Recurse -Force '..\Storage\mode' '.'
		Copy-Item -Force '..\Storage\stun.txt' 'bin'
		Copy-Item -Force '..\Storage\nfdriver.sys' 'bin'
		Copy-Item -Recurse -Force '..\Storage\sing_box' 'bin'
		Copy-Item -Force '..\Storage\aiodns.conf' 'bin'

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

		New-Item -ItemType Directory -Name 'pac' -Path 'bin' -Force | Out-Null
		foreach ($pacName in @('proxy-list.txt', 'gfw.txt', 'direct-list.txt')) {
			$pacTarget = "bin\pac\$pacName"
			$pacFallback = "..\Storage\pac\$pacName"
			$downloaded = $false
			foreach ($pacUrl in @(
				"https://raw.githubusercontent.com/Loyalsoldier/v2ray-rules-dat/release/$pacName",
				"https://cdn.jsdelivr.net/gh/Loyalsoldier/v2ray-rules-dat@release/$pacName"
			)) {
				try {
					Invoke-WebRequest -Uri $pacUrl -OutFile $pacTarget
					$downloaded = $true
					break
				}
				catch {
					Write-Warning "Download PAC rule $pacName from $pacUrl failed: $_"
				}
			}
			if (-not $downloaded) {
				if (Test-Path $pacFallback) {
					Write-Warning "Using local fallback $pacFallback for PAC rule $pacName"
					Copy-Item -Force $pacFallback $pacTarget
				}
				else {
					Write-Warning "Download PAC rule $pacName failed and no local copy exists."
				}
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
	elseif (-not $UseCache) {
		$aiodnsBinary = Get-Item -LiteralPath '.\Other\release\aiodns.bin'
		$aiodnsSources = Get-ChildItem -Path '.\Other\aiodns\*' -File -Recurse -Include '*.go', 'go.mod', 'go.sum', 'build.ps1'
		if ($aiodnsSources | Where-Object LastWriteTimeUtc -gt $aiodnsBinary.LastWriteTimeUtc) {
			Invoke-Native -Name 'Build AioDNS' -Command { & .\Other\aiodns\build.ps1 }
		}
	}
	Copy-Item -Force '.\Other\release\*.bin' "$OutputPath\bin"
	Copy-Item -Force '.\Other\release\*.dll' "$OutputPath\bin"
	Copy-Item -Force '.\Other\release\*.exe' "$OutputPath\bin" -Exclude 'v2ray-sn.exe'

	$executable = 'Netch.App.exe'
	$project = '.\Netch.App\Netch.App.csproj'
	$deploymentMode = if ($SelfContained) { 'self-contained' } else { 'framework-dependent' }
	$selfContainedProp = if ($SelfContained) { 'true' } else { 'false' }
	$netchPublishPath = ".\Netch.App\bin\publish-single\$Configuration\$deploymentMode"
	if (Test-Path -LiteralPath $netchPublishPath) {
		Remove-Item -Recurse -Force -LiteralPath $netchPublishPath
	}
	Write-Host
	Write-Host "Building Netch (WinUI, $deploymentMode)"

	$publishArgs = @(
		'-c', $Configuration
		'-r', 'win-x64'
		"-p:Platform=x64"
		"-p:SelfContained=$selfContainedProp"
		"-p:PublishSingleFile=true"
		"-p:PublishReadyToRun=$PublishReadyToRun"
		"-p:PublishReadyToRunShowWarnings=$PublishReadyToRun"
		"-p:IncludeNativeLibrariesForSelfExtract=true"
		"-p:IncludeAllContentForSelfExtract=true"
		"-p:EnableCompressionInSingleFile=$selfContainedProp"
		'-o', $netchPublishPath
	)
	if (-not [string]::IsNullOrWhiteSpace($Version)) {
		$normalizedVersion = $Version.Trim().TrimStart('v', 'V')
		if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:\.[0-9A-Za-z]+)*)?$') {
			throw "Invalid Version '$Version'. Expected SemVer (for example: 2.0.0 or 2.0.0-beta1)."
		}
		$publishArgs += "-p:Version=$normalizedVersion"
	}
	$publishArgs += $project

	Invoke-Native -Name 'dotnet publish' -Command {
		dotnet publish @publishArgs
	}
	Copy-Item -Force "$netchPublishPath\$executable" $OutputPath
	$looseFiles = Get-ChildItem -LiteralPath $netchPublishPath -Recurse -File |
		Where-Object { $_.Name -ne $executable -and $_.Extension -ne '.pdb' }
	if ($looseFiles) {
		throw "WinUI publish left files outside the EXE: $($looseFiles.Name -join ', ')"
	}
	foreach ($directory in 'data', 'logging', 'licenses') {
		New-Item -ItemType Directory -Path (Join-Path $OutputPath $directory) | Out-Null
	}
	$licensePath = Join-Path $OutputPath 'licenses'
	Copy-Item -Force '.\LICENSE', '.\NOTICE.txt' $licensePath
	Copy-Item -Force '.\Storage\README.md' (Join-Path $licensePath 'Components.md')
	if (-not (Test-Path -LiteralPath (Join-Path $OutputPath 'bin\sing_box\libcronet.dll') -PathType Leaf)) {
		throw 'Missing bin\sing_box\libcronet.dll in build output.'
	}

	$redirectorPath = ".\Redirector\bin\$Configuration"
	$buildRedirector = Test-ShouldBuild "$redirectorPath\Redirector.bin", "$redirectorPath\nfapi.dll", "$redirectorPath\Redirector.pdb"
	$buildRouteHelper = Test-ShouldBuild ".\RouteHelper\bin\$Configuration\RouteHelper.bin"
	if ($buildRedirector -or $buildRouteHelper) {
		$msbuild = Resolve-MSBuild
		Write-Host "Using MSBuild: $msbuild"
	}

	if ($buildRedirector) {
		Write-Host
		Write-Host 'Building Redirector'

		Invoke-Native -Name 'msbuild Redirector' -Command {
			& $msbuild `
				-nologo `
				-property:Configuration=$Configuration `
				-property:Platform=x64 `
				'.\Redirector\Redirector.vcxproj'
		}
	}
	Copy-Item -Force "$redirectorPath\nfapi.dll"      "$OutputPath\bin"
	Copy-Item -Force "$redirectorPath\Redirector.bin" "$OutputPath\bin"

	if ($buildRouteHelper) {
		Write-Host
		Write-Host 'Building RouteHelper'

		Invoke-Native -Name 'msbuild RouteHelper' -Command {
			& $msbuild `
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
