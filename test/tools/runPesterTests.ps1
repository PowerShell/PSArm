param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm",
    [switch]$CI
)

$ErrorActionPreference = 'Stop'

Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"

Push-Location $PSScriptRoot
try
{
    $armDepsDir = join-path ([System.IO.Path]::GetTempPath()) 'PSArmDeps'
    Write-Host "PSModulePath: '$env:PSModulePath'"
    if (Test-Path $armDepsDir)
    {
        Write-Host "PSArmDeps: '$(gci $armDepsDir)'"
    }
    else
    {
        Write-Host "PSArmDeps directory not found"
    }

    $results = Invoke-Pester -Path "$PSScriptRoot/../pester" -CI:$CI -PassThru
    if ($results.Failed)
    {
        throw "Pester tests failed. See output for details"
    }
}
finally
{
    Pop-Location
}
