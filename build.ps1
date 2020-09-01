
# Copyright (c) Microsoft Corporation.
# All rights reserved.

[CmdletBinding(DefaultParameterSetName = "Build")]
param(
    [Parameter(ParameterSetName = "Build")]
    [Parameter(ParameterSetName = "Test")]
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug',

    [Parameter(Mandatory, ParameterSetName="Test")]
    [switch]
    $Test,

    [Parameter(ParameterSetName="Test")]
    [switch]
    $SkipBuild
)

$ErrorActionPreference = 'Stop'

$netTarget = 'netstandard2.0'
$moduleName = "PSArm"
$dotnetLibName = $moduleName
$outDir = "$PSScriptRoot/out/$moduleName"
$srcDir = "$PSScriptRoot/src"
$dotnetSrcDir = $srcDir
$binDir = "$srcDir/bin/$Configuration/$netTarget/publish"

if (-not $SkipBuild)
{

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
}

if ($Test)
{
    $pwsh = (Get-Process -Id $PID).Path
    & $pwsh -Command "Import-Module '$outDir'; Import-Module '$PSScriptRoot/test/pester/TestHelper.psm1'; Invoke-Pester -Path '$PSScriptRoot/test/pester'"
}
