param([string]$file)
$hash = [Security.Cryptography.HashAlgorithm]::Create( "SHA256" )
$stream = ([IO.StreamReader]$file).BaseStream
-join ($hash.ComputeHash($stream) | ForEach { "{0:X2}" -f $_ })
$stream.Close()
