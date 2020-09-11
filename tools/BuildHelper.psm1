function Write-Log
{
    param([string]$Message)

    Write-Host $Message
}

function Unsplat
{
    param([hashtable]$SplatParams)

    ($SplatParams.GetEnumerator() | ForEach-Object { $name = $_.Key; $value = $_.Value; "-$name $value" }) -join " "
}
