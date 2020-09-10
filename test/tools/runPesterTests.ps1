param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm",
    [switch]$CI
)

$ErrorActionPreference = 'Stop'

Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"

Push-Location "$PSScriptRoot/../../"
try
{
    $armDepsDir = join-path ([System.IO.Path]::GetTempPath()) 'PSArmDeps'
    Write-Verbose "PSModulePath: '$env:PSModulePath'"
    if (Test-Path $armDepsDir)
    {
        Write-Verbose "PSArmDeps: '$(Get-ChildItem $armDepsDir)'"
    }
    else
    {
        Write-Verbose "PSArmDeps directory not found"
    }

    if ($CI)
    {
        Invoke-Pester -Path "./test/pester" -CI
        return
    }

    $testResults = Invoke-Pester -Path "./test/pester" -PassThru
    if ($testResults.Failed)
    {
        throw "Pester tests failed. See output for details"
    }
}
finally
{
    Pop-Location
}
