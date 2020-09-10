param(
    $PSArmPath = "$PSScriptRoot/../../out/PSArm"
)

Import-Module -Name $PSArmPath
Import-Module -Name "$PSScriptRoot/TestHelper.psm1"
Invoke-Pester -Path "$PSScriptRoot/../pester"