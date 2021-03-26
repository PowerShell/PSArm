# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug',

    [ValidateSet('netcoreapp3.1','net471')]
    [string[]]
    $TargetFrameworks = ($(if($false -eq $IsWindows){'netcoreapp3.1'}else{'netcoreapp3.1','net471'})),

    [switch]
    $RunTestsInProcess,

    [switch]
    $RunTestsInCIMode,

    [string]
    $TestPSArmPath,

    [string[]]
    $TestPowerShell = ($(if($false -eq $IsWindows){'pwsh'}elseif($RunTestsInCIMode){'pwsh'}else{'pwsh','powershell'}))
)

Import-Module "$PSScriptRoot/tools/BuildHelper.psm1"

$ErrorActionPreference = 'Stop'

$RequiredTestModules = @(
    @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
)
$ModuleDirs = @{
    'net471' = 'Desktop'
    'netcoreapp3.1' = 'Core'
}
$ModuleName = "PSArm"
$OutDir = "$PSScriptRoot/out/$ModuleName"
$SrcDir = "$PSScriptRoot/src"
$BinDir = "$SrcDir/bin/$Configuration"
$DotnetSrcDir = $srcDir
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
        foreach ($framework in $TargetFrameworks)
        {
            dotnet publish -c $Configuration -f $framework
        }
    }
    finally
    {
        Pop-Location
    }

    if (Test-Path $OutDir)
    {
        Remove-Item -Path $OutDir -Recurse -Force
    }

    # Copy shared assets

    $sharedAssets = @(
        "$SrcDir/$ModuleName.psd1",
        "$SrcDir/ArmBuiltins.psm1",
        "$SrcDir/OnImport.ps1"
    )

    New-Item -ItemType Directory -Path $OutDir

    foreach ($path in $sharedAssets)
    {
        Copy-Item -Recurse -Path $path -Destination $OutDir
    }

    # Create powershell-version-specific asset deployments

    foreach ($framework in $TargetFrameworks)
    {
        $fullBinDir = "$BinDir/$framework/publish"
        $destination = "$OutDir/$($ModuleDirs[$framework])"

        New-Item -ItemType Directory -Path $destination
        Copy-Item -Recurse -Path "$fullBinDir/*.dll" -Destination $destination
        Copy-Item -Recurse -Path "$fullBinDir/*.pdb" -Destination $destination
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

            foreach ($pwsh in $TestPowerShell)
            {
                exec { & $pwsh @pwshArgs }
            }
        }
    }
    finally
    {
        $env:PSModulePath = $oldPSModulePath
    }
}

task . Build,Test
