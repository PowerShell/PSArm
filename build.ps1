
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$netTarget = 'netstandard2.0'
$moduleName = "PSArm"
$dotnetLibName = $moduleName
$outDir = "$PSScriptRoot/out/$moduleName"
$srcDir = "$PSScriptRoot/src"
$dotnetSrcDir = $srcDir
$binDir = "$srcDir/bin/$Configuration/$netTarget/publish"

Push-Location $dotnetSrcDir
try
{
    dotnet restore
    dotnet publish -f $netTarget
}
finally
{
    Pop-Location
}

if (Test-Path $outDir)
{
    Remove-Item -Path $outDir -Recurse -Force
}

$assets = @(
    "$binDir/*.dll",
    "$binDir/*.pdb",
    "$srcDir/$moduleName.psd1",
    "$srcDir/schemas",
    "$srcDir/OnImport.ps1"
)

New-Item -ItemType Directory -Path $outDir
foreach ($path in $assets)
{
    Copy-Item -Recurse -Path $path -Destination $outDir
}