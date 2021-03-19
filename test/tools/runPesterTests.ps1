
# Copyright (c) Microsoft Corporation.

param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm",
    [switch]$CI
)

$ErrorActionPreference = 'Stop'

Import-Module -Name Pester
Import-Module "$PSScriptRoot/../../tools/BuildHelper.psm1"
Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"

Write-Log "PSModulePath: '$env:PSModulePath'"
Write-Log "PSArmPath: '$PSArmPath'"

$armDepsDir = join-path ([System.IO.Path]::GetTempPath()) 'PSArmDeps'
if (Test-Path $armDepsDir)
{
    Write-Log "PSArmDeps: '$(Get-ChildItem $armDepsDir)'"
}
else
{
    Write-Log "PSArmDeps directory not found"
}

$repoRoot = (Resolve-Path "$PSScriptRoot/../../").Path
$testsPath = Join-Path $repoRoot "test/pester"
$testResultsPath = Join-Path $repoRoot "testResults.xml"

$config = [PesterConfiguration]@{
    Run = @{
        Path = $testsPath
        PassThru = $true
    }
    Output = @{
        Verbosity = 'Detailed'
    }
}

if ($CI)
{
    $config.TestResult = @{
        Enabled = $true
        OutputFormat = 'NUnitXml'
        OutputPath = $testResultsPath
    }
}

$testResults = Invoke-Pester -Configuration $config

if ($null -eq $testResults -or $testResults.Failed -or ($CI -and -not (Test-Path $testResultsPath)))
{
    throw "Pester tests failed. See output for details"
}
