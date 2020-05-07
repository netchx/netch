if (Test-Path Netch\bin)
{
    Remove-Item -Recurse -Force Netch\bin
}

if (Test-Path Netch\obj)
{
    Remove-Item -Recurse -Force Netch\obj
}

if (Test-Path NetchLib\bin)
{
    Remove-Item -Recurse -Force NetchLib\bin
}

if (Test-Path NetchLib\obj)
{
    Remove-Item -Recurse -Force NetchLib\obj
}

exit 0
