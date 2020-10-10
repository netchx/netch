param([string]$file)
$hash = [Security.Cryptography.HashAlgorithm]::Create( "SHA256" )
$path = (Resolve-Path -Path $file).Path
$stream = ([IO.StreamReader]$path).BaseStream
-join ($hash.ComputeHash($stream) | ForEach-Object { "{0:x2}" -f $_ })
$stream.Close()