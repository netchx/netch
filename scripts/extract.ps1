param (
    [string]
    $name,

    [string]
    $address
)

try
{
    Expand-Archive -Force -Path $name -DestinationPath $address
}
catch
{
    Write-Host "Extract $name failed"
    exit 1
}

exit 0