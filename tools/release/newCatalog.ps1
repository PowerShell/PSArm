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

$tempDir = [System.IO.Path]::GetTempPath()
if (-not (Test-Path $tempDir))
{
    New-Item -Path $tempDir -Force -ItemType Directory
}

$tempPath = Join-Path -Path $tempDir -ChildPath "temp.cat"
New-FileCatalog -Path $OutputPath -CatalogFilePath $tempPath
Copy-Item -LiteralPath $tempPath -Destination $OutputPath -Force
