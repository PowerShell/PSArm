param(
    [Parameter()]
    [string]
    $SignedDirPath,

    [Parameter()]
    [string]
    $Destination
)

foreach ($file in Get-ChildItem -LiteralPath $SignedDirPath -Recurse)
{
    $newPath = $file.FullName.Substring($SignedDirPath.Length).TrimStart(@('\', '/'))
    $newPath = Join-Path -Path $Destination  -ChildPath $newPath

    Copy-Item -Force -LiteralPath $file.FullName -Destination $newPath
}
