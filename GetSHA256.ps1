param([string]$file)
$hash = [Security.Cryptography.HashAlgorithm]::Create( "SHA256" )
$stream = ([IO.StreamReader]$file).BaseStream
-join ($hash.ComputeHash($stream) | ForEach { "{0:x2}" -f $_ })
$stream.Close()
