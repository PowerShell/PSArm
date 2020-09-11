param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm",
    [switch]$CI
)

$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot/../../tools/BuildHelper.psm1"
Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"

Push-Location "$PSScriptRoot/../../"
try
{
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
