param (
	[string]
	$Location = ''
)

Push-Location $Location

$global:data = ''
function Scan {
    param (
        [string]
        $path = ''
    )

    foreach ( $item in ( Get-ChildItem -Path $path -File ) ) {
        $name = $item.Name

        $global:data += (Get-FileHash -Path ".\$path\$name" -Algorithm SHA256).Hash.ToLower()
    }

    foreach ( $item in ( Get-ChildItem -Path $path -Directory ) ) {
        $name = $item.Name

        Scan -Path ".\$path\$name"
    }
}

Scan -Path '.'
Write-Output (Get-FileHash -InputStream ([System.IO.MemoryStream]::New([System.Text.Encoding]::UTF8.GetBytes($data)))).Hash.ToLower()

Pop-Location
exit 0