$exec=(Split-Path $MyInvocation.MyCommand.Path -Parent)
$last=(Get-Location).Path

Set-Location $exec

if (Test-Path release) {
    Remove-Item -Recurse -Force release
}

Set-Location $last
exit 0
