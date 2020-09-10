param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm",
    $OutputPath = "$PSScriptRoot/../../testResults.xml",
    [switch]$EnableExit
)

Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"
Invoke-Pester -Path "$PSScriptRoot/../pester" -CI
Move-Item -Path 'testResults.xml' -Destination "$OutputPath" -Force