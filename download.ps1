param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $OutputPath = "release",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $CachePath = "DataCache"
)

function DownloadAndExtract {
    param (

        # NetchX
        [Parameter(Mandatory = $True)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Owner,

        # NetchMode
        [Parameter(Mandatory = $True)]
        [string]
        [ValidateNotNullOrEmpty()]
        $Repo,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $ref = "heads/master",

        [Parameter(Mandatory = $True)]
        [ValidateNotNullOrEmpty()]
        [string]
        $CachePath,
        
        [Parameter()]
        [ValidateNotNull()]
        [string]
        $ReleativePath = "",

        [Parameter(Mandatory = $True)]
        [ValidateNotNullOrEmpty()]
        [string]
        $TargetPath
    )
    $json = Invoke-RestMethod "https://api.github.com/repos/$Owner/$Repo/git/refs/$ref"
    $sha = $json.object.sha
    $archiveUrl = "https://github.com/$Owner/$Repo/archive/$sha.zip"

    $fileName = "$repo-$sha"
    $filePath = "$CachePath\$fileName.zip"
    $cacheExtractPath = "$CachePath\$fileName"
    
    if ( -Not (Test-Path $filePath) ) {
        Remove-Item -Recurse -Force $CachePath\$Repo* | Out-Null
        Invoke-WebRequest -Uri $archiveUrl -OutFile $filePath
    }

    if ( -Not (Test-Path $cacheExtractPath) ) {
        Expand-Archive -Force -Path $filePath -DestinationPath $CachePath
    }

    New-Item -Force -ItemType Directory -Path $TargetPath | Out-Null
    Copy-Item -Recurse -Force "$cacheExtractPath\$ReleativePath\*" $TargetPath
}

New-Item -Force -ItemType Directory -Path $OutputPath, $CachePath | Out-Null

DownloadAndExtract -Owner "NetchX" -Repo "NetchData" -CachePath $CachePath -TargetPath $OutputPath\bin
DownloadAndExtract -Owner "NetchX" -Repo "NetchMode" -CachePath $CachePath -ReleativePath "mode" -TargetPath $OutputPath\mode
DownloadAndExtract -Owner "NetchX" -Repo "NetchI18N" -CachePath $CachePath -ReleativePath "i18n" -TargetPath $OutputPath\i18n

Get-Item $OutputPath

exit 0;