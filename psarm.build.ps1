
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug'
)

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

Write-Host "TempDepsDir: '$TempDependenciesLocation'"
Write-Host "TempModDir: '$TempModulesLocation'"

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

    $alreadyInstalled = Get-Module -ListAvailable -FullyQualifiedName $RequiredModules
    $needToInstall = $RequiredModules | Where-Object { $_.ModuleName -notin $alreadyInstalled.Name }

    foreach ($module in $needToInstall)
    {
        if (-not (Test-Path $TempModulesLocation))
        {
            New-Item -Path $TempModulesLocation -ItemType Directory
        }

        Write-Host "Installing module '$($module.ModuleName)' to '$TempModulesLocation'"
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
    $pwshArgs = @('-File', "$PSScriptRoot/test/tools/runPesterTests.ps1")
    if ($env:TF_BUILD)
    {
        $pwshArgs += @('-CI')
    }

    $oldPSModulePath = $env:PSModulePath
    $sep = [System.IO.Path]::PathSeparator
    $env:PSModulePath = "${$script:TempModulesLocation}${sep}${env:PSModulePath}"
    try
    {
        exec { & (Get-PwshPath) @pwshArgs }
    }
    finally
    {
        $env:PSModulePath = $oldPSModulePath
    }
}

task . Build,Test