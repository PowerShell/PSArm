
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

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
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        $OriginalDirPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $SignedDirPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Destination
    )

    Write-Log "Files in OriginalDir '$OriginalDirPath':"
    tree /f /a $OriginalDirPath | Write-Log
    Write-Log "Files in SignedDir '$SignedDirPath':"
    tree /f /a $SignedDirPath | Write-Log

    Write-Log "Copying signed files from path '$SignedDirPath' to path '$Destination'"

    foreach ($file in Get-ChildItem -LiteralPath $SignedDirPath -Recurse)
    {
        $newPath = $file.FullName.Substring($SignedDirPath.Length).TrimStart(@('\', '/'))
        $newPath = Join-Path -Path $Destination -ChildPath $newPath
        Write-Log "Copying '$($file.FullName)' to '$newPath'"
        Copy-Item -Force -LiteralPath $file.FullName -Destination $newPath
    }

    Write-Log "Copying remaning files from path '$OriginalDirPath' to '$Destination'"
    
    foreach ($file in Get-ChildItem -LiteralPath $OriginalDirPath -Recurse)
    {
        $newPath = $file.FullName.Substring($OriginalDirPath.Length).TrimStart(@('\','/'))
        $newPath = Join-Path -Path $Destination -ChildPath $newPath

        if (Test-Path -LiteralPath $newPath)
        {
            continue
        }

        Write-Log "Copying '$($file.FullName)' to '$newPath'"
        Copy-Item -LiteralPath $file.FullName -Destination $newPath
    }
}

function Assert-FilesAreSigned
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    $allSigned = $true
    $count = 0
    $signableCount = 0
    foreach ($file in Get-ChildItem -LiteralPath $Path -Recurse)
    {
        $count++

        if ([System.IO.Path]::GetExtension($file.Name) -in '.dll','.ps1','.psd1','.psm1')
        {
            $signableCount++
            Write-Log "Validating signature on '$($file.FullName)'"
            $sig = Get-AuthenticodeSignature -FilePath $file.FullName

            if ($sig.Status -ne 'Valid')
            {
                $allSigned = $false
                Write-Error "File '$($file.FullName)' is not signed"
            }
        }
    }

    Write-Log "Found $count files and $signableCount signable files"

    if ($signableCount -eq 0)
    {
        throw "No files were signed"
    }

    if (-not $allSigned)
    {
        throw "Some files were not signed. See above errors for details"
    }
    else
    {
        Write-Log "All file signatures are valid"
    }
}
