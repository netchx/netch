param (
	[string]
	$Location = ''
)

Push-Location $Location

$ms = [System.IO.MemoryStream]::new();
$wr = [System.IO.StreamWriter]::new($ms);

function Scan {
    param (
        [string]
        $path = ''
    )

    foreach ( $item in ( Get-ChildItem -Path $path ) ) {
        $name = $item.Name

        if ( Test-Path -Path ".\$path\$name" -PathType Container ) {
            Scan -Path ".\$path\$name"
            continue
        }

        if ( Test-Path -Path ".\$path\$name" -PathType Leaf ) {
            $wr.Write((Get-FileHash -Path ".\$path\$name" -Algorithm SHA256).Hash.ToLower())
        }
    }
}

Scan -Path '.'
Write-Output (Get-FileHash -InputStream $ms).Hash.ToLower()

Pop-Location
exit 0