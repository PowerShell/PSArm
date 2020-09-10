
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$script:RequiredTestModules = @(
    @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
)
$script:TargetFrameworks = 'net452','netstandard2.0'
$script:NetTarget = 'netstandard2.0'
$script:ModuleName = "PSArm"
$script:DotnetLibName = $moduleName
$script:OutDir = "$PSScriptRoot/out/$moduleName"
$script:SrcDir = "$PSScriptRoot/src"
$script:DotnetSrcDir = $srcDir
$script:BinDir = "$srcDir/bin/$Configuration/$netTarget/publish"
$script:TempDependenciesLocation = Join-Path ([System.IO.Path]::GetTempPath()) 'PSArmDeps'
$script:TempModulesLocation = Join-Path $script:TempDependenciesLocation 'Modules'

$script:OldModulePath = $env:PSModulePath

function Get-PwshPath
{
    return (Get-Process -Id $PID).PAth
}

function Remove-ModuleDependencies
{
    if (Test-Path -Path $script:TempDependenciesLocation)
    {
        Remove-Item -Force -Recurse -LiteralPath $script:TempDependenciesLocation
    }
}

task InstallTestDependencies InstallRequiredTestModules

task InstallRequiredTestModules {
    Remove-ModuleDependencies

    $alreadyInstalled = Get-Module -ListAvailable -FullyQualifiedName $script:RequiredModules
    $needToInstall = $script:RequiredModules | Where-Object { $_.ModuleName -notin $alreadyInstalled.Name }

    foreach ($module in $needToInstall)
    {
        Save-Module -LiteralPath $script:TempDependenciesLocation -Name $module.ModuleName -MinimumVersion $module.ModuleVersion
    }

    $sep = [System.IO.Path]::PathSeparator
    $env:PSModulePath = "${env:PSModulePath}${sep}${$script:TempModulesLocation}"
}

task Build {
    Push-Location $script:DotnetSrcDir
    try
    {
        dotnet restore
        dotnet publish -f $script:NetTarget
    }
    finally
    {
        Pop-Location
    }

    if (Test-Path $script:OutDir)
    {
        Remove-Item -Path $script:OutDir -Recurse -Force
    }

    $assets = @(
        "$script:BinDir/*.dll",
        "$script:BinDir/*.pdb",
        "$script:SrcDir/$script:ModuleName.psd1",
        "$script:SrcDir/schemas",
        "$script:SrcDir/OnImport.ps1"
    )

    New-Item -ItemType Directory -Path $script:OutDir
    foreach ($path in $assets)
    {
        Copy-Item -Recurse -Path $path -Destination $script:OutDir
    }
}

task Test TestPester

task TestPester {
    # Run tests in a new process so that the built module isn't stuck in the calling process
    $pwshArgs = @('-File', "$PSScriptRoot/test/tools/runPesterTests.ps1")
    if ($env:TF_BUILD)
    {
        $pwshArgs += @('-CI')
    }
    exec { & (Get-PwshPath) @pwshArgs }
}

task . Build,Test