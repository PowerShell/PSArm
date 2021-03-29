# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding(DefaultParameterSetName = "Build")]
param(
    [Parameter(ParameterSetName = "Build")]
    [Parameter(ParameterSetName = "Test")]
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug',

    [Parameter(Mandatory, ParameterSetName="Test")]
    [switch]
    $Test,

    [Parameter(ParameterSetName="Test")]
    [switch]
    $SkipBuild
)

if (-not (Get-Command Invoke-Build -ErrorAction Ignore))
{
    Install-Module InvokeBuild -Scope CurrentUser
}

Push-Location $PSScriptRoot

try
{
    if (-not $SkipBuild)
    {
        Invoke-Build Build -Configuration $Configuration
    }

    if ($Test)
    {
        Invoke-Build Test
    }
}
finally
{
    Pop-Location
}
