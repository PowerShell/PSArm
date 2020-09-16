param(
    [Parameter()]
    [string]
    $Path,

    [Parameter()]
    [string]
    $OutputPath
)

if (-not (Get-Command New-FileCatalog -ErrorAction SilentlyContinue))
{
    throw "New-FileCatalog command not found -- required for catalog creation"
}

$tempPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "temp.cat"
New-FileCatalog -CatalogFilePath $OutputPath -Path $tempPath
Copy-Item -LiteralPath $tempPath -Destination $OutputPath -Force
