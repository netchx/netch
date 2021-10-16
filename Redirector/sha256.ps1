Push-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

$ms = [System.IO.MemoryStream]::new();
$wr = [System.IO.StreamWriter]::new($ms);
Get-ChildItem -Path '.' -File | ForEach-Object {
    $name=$_.Name

    $wr.Write((Get-FileHash -Path ".\$name" -Algorithm SHA256).Hash.ToLower())
}
$ms.Position = 0

Write-Output (Get-FileHash -InputStream $ms).Hash.ToLower()
Pop-Location