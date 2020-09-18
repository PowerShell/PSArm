Import-Module "$PSScriptRoot/../BuildHelper.psm1"

function New-Catalog
{
    param(
        [Parameter()]
        [string]
        $Path,

        [Parameter()]
        [string]
        $OutputPath
    )

    Write-Log "Creating file catalog for path '$Path' with output location '$OutputPath'"

    if (-not (Get-Command New-FileCatalog -ErrorAction SilentlyContinue))
    {
        throw "New-FileCatalog command not found -- required for catalog creation"
    }

    $outputDir = Split-Path $OutputPath
    if (-not (Test-Path $outputDir))
    {
        Write-Log "Creating catalog output directory '$outputDir'"
        New-Item -Path $outputDir -Force -ItemType Directory
    }

    New-FileCatalog -Path $Path -CatalogFilePath $OutputPath
}

function Publish-Module
{
    param(
        [Parameter()]
        [string]
        $ModulePath
    )

    Write-Host "Publishing module!"
    Get-Item $ModulePath
}

function Copy-SignedFiles
{
    param(
        [Parameter()]
        [string]
        $SignedDirPath,

        [Parameter()]
        [string]
        $Destination
    )

    Write-Log "Copying signed files from path '$SignedDirPath' to path '$Destination'"

    foreach ($file in Get-ChildItem -LiteralPath $SignedDirPath -Recurse)
    {
        $newPath = $file.FullName.Substring($SignedDirPath.Length).TrimStart(@('\', '/'))
        $newPath = Join-Path -Path $Destination  -ChildPath $newPath

        Copy-Item -Force -LiteralPath $file.FullName -Destination $newPath
    }
}
