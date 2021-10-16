Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

$ms = [System.IO.MemoryStream]::new();
$wr = [System.IO.StreamWriter]::new($ms);
Get-ChildItem -Path '.' -Directory | ForEach-Object {
    $dirName=$_.Name

    Get-ChildItem -Path ".\$dirName" -File | ForEach-Object {
        $fileName=$_.Name

        $wr.Write((Get-FileHash -Path ".\$dirName\$fileName" -Algorithm SHA256).Hash.ToLower())
    }
}
$ms.Position = 0

Write-Output (Get-FileHash -InputStream $ms).Hash.ToLower()
Pop-Location