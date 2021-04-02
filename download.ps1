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

    if ( -Not (Test-Path $filePath) ) {
        Remove-Item -Recurse -Force $CachePath\$Repo*
        Invoke-WebRequest -Uri $archiveUrl -OutFile $filePath
    }

    $cacheExtractPath = "$CachePath\$fileName"
    if ( -Not (Test-Path $cacheExtractPath) ) {
        Expand-Archive -Force -Path $filePath -DestinationPath $CachePath
    }

    Copy-Item -Recurse -Path "$cacheExtractPath\$ReleativePath" $TargetPath
}

DownloadAndExtract -Owner "NetchX" -Repo "NetchData" -CachePath $CachePath -ReleativePath "\" -TargetPath $OutputPath\bin\
DownloadAndExtract -Owner "NetchX" -Repo "NetchMode" -CachePath $CachePath -ReleativePath "mode" -TargetPath $OutputPath
DownloadAndExtract -Owner "NetchX" -Repo "NetchI18N" -CachePath $CachePath -ReleativePath "i18n" -TargetPath $OutputPath

Get-Item $OutputPath

exit 0;