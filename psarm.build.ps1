
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug',

    [switch]
    $RunTestsInProcess,

    [switch]
    $RunTestsInCIMode,

    [string]
    $TestPSArmPath
)

Import-Module "$PSScriptRoot/tools/BuildHelper.psm1"

$ErrorActionPreference = 'Stop'

$RequiredTestModules = @(
    @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
)
$TargetFrameworks = 'net452','netstandard2.0'
$NetTarget = 'netstandard2.0'
$ModuleName = "PSArm"
$DotnetLibName = $moduleName
$OutDir = "$PSScriptRoot/out/$moduleName"
$SrcDir = "$PSScriptRoot/src"
$DotnetSrcDir = $srcDir
$BinDir = "$srcDir/bin/$Configuration/$netTarget/publish"
$TempDependenciesLocation = Join-Path ([System.IO.Path]::GetTempPath()) 'PSArmDeps'
$TempModulesLocation = Join-Path $TempDependenciesLocation 'Modules'

Write-Log "TempDepsDir: '$TempDependenciesLocation'"
Write-Log "TempModDir: '$TempModulesLocation'"

function Get-PwshPath
{
    return (Get-Process -Id $PID).PAth
}

function Remove-ModuleDependencies
{
    if (Test-Path -Path $TempDependenciesLocation)
    {
        Remove-Item -Force -Recurse -LiteralPath $TempDependenciesLocation
    }
}

task InstallTestDependencies InstallRequiredTestModules

task InstallRequiredTestModules {
    Remove-ModuleDependencies

    $alreadyInstalled = Get-Module -ListAvailable -FullyQualifiedName $RequiredTestModules
    $needToInstall = $RequiredTestModules | Where-Object { $_.ModuleName -notin $alreadyInstalled.Name }

    foreach ($module in $needToInstall)
    {
        if (-not (Test-Path $TempModulesLocation))
        {
            New-Item -Path $TempModulesLocation -ItemType Directory
            Write-Log "Created directory '$TempModulesLocation'"
        }

        Write-Log "Installing module '$($module.ModuleName)' to '$TempModulesLocation'"
        Save-Module -LiteralPath $TempModulesLocation -Name $module.ModuleName -MinimumVersion $module.ModuleVersion
    }
}

task Build {
    Push-Location $DotnetSrcDir
    try
    {
        dotnet restore
        dotnet publish -f $NetTarget
    }
    finally
    {
        Pop-Location
    }

    if (Test-Path $OutDir)
    {
        Remove-Item -Path $OutDir -Recurse -Force
    }

    $assets = @(
        "$BinDir/*.dll",
        "$BinDir/*.pdb",
        "$SrcDir/$ModuleName.psd1",
        "$SrcDir/schemas",
        "$SrcDir/OnImport.ps1"
    )

    New-Item -ItemType Directory -Path $OutDir
    foreach ($path in $assets)
    {
        Copy-Item -Recurse -Path $path -Destination $OutDir
    }
}

task Test TestPester

task TestPester InstallRequiredTestModules,{
    # Run tests in a new process so that the built module isn't stuck in the calling process
    $testScriptPath = "$PSScriptRoot/test/tools/runPesterTests.ps1"

    $oldPSModulePath = $env:PSModulePath
    $sep = [System.IO.Path]::PathSeparator
    $env:PSModulePath = "${TempModulesLocation}${sep}${env:PSModulePath}"
    try
    {
        if ($RunTestsInProcess)
        {
            $testParams = @{}
            if ($RunTestsInCIMode) { $testParams['CI'] = $true }
            if ($TestPSArmPath) { $testParams['PSArmPath'] = $TestPSArmPath }

            Write-Log "Invoking in process: '$testScriptPath $(Unsplat $testParams)'"

            & $testScriptPath @testParams
        }
        else
        {
            $pwshArgs = @('-File', $testScriptPath)
            if ($RunTestsInCIMode)
            {
                $pwshArgs += @('-CI')
            }
            if ($TestPSArmPath)
            {
                $pwshArgs += @('-PSArmPath', $TestPSArmPath)
            }

            Write-Log "Invoking in subprocess: 'pwsh $pwshArgs'"

            exec { & (Get-PwshPath) @pwshArgs }
        }
    }
    finally
    {
        $env:PSModulePath = $oldPSModulePath
    }
}

task . Build,Test