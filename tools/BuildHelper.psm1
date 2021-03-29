
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

filter Write-Log
{
    param(
        [Parameter(ValueFromPipeline)]
        [string[]]
        $Message
    )

    $Message | Write-Host
}

function Unsplat
{
    param([hashtable]$SplatParams)

    ($SplatParams.GetEnumerator() | ForEach-Object { $name = $_.Key; $value = $_.Value; "-$name $value" }) -join " "
}
