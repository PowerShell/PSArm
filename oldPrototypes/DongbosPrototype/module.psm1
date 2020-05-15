
# Copyright (c) Microsoft Corporation.
# All rights reserved.

function Resource
{
    [CmdletBinding()]
    param([scriptblock]$Body)

    $sessionState = $PSCmdlet.SessionState
    $dslScriptBlock = (Get-Command "$PSScriptRoot/dsl.ps1").ScriptBlock
    $PSCmdlet.InvokeCommand.InvokeScript($sessionState, $dslScriptBlock)
    $PSCmdlet.InvokeCommand.InvokeScript($sessionState, $Body)
}