
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# All rights reserved.

Set-Item function:\__OldTabExpansion2 (Get-Content -Raw Function:\TabExpansion2)

function TabExpansion2
{
    [CmdletBinding(DefaultParameterSetName = 'ScriptInputSet')]
    Param(
        [Parameter(ParameterSetName = 'ScriptInputSet', Mandatory = $true, Position = 0)]
        [string] $inputScript,

        [Parameter(ParameterSetName = 'ScriptInputSet', Position = 1)]
        [int] $cursorColumn = $inputScript.Length,

        [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 0)]
        [System.Management.Automation.Language.Ast] $ast,

        [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 1)]
        [System.Management.Automation.Language.Token[]] $tokens,

        [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 2)]
        [System.Management.Automation.Language.IScriptPosition] $positionOfCursor,

        [Parameter(ParameterSetName = 'ScriptInputSet', Position = 2)]
        [Parameter(ParameterSetName = 'AstInputSet', Position = 3)]
        [Hashtable] $options = $null
    )
    
    if ($PSCmdlet.ParameterSetName -eq 'ScriptInputSet')
    {
        $convertedInput = [System.Management.Automation.CommandCompletion]::MapStringInputToParsedInput($inputScript, $cursorColumn)
        $ast = $convertedInput.Item1
        $tokens = $convertedInput.Item2
        $positionOfCursor = $convertedInput.Item3
    }

    $result = __OldTabExpansion2 $ast $tokens $positionOfCursor $options

    [PSArm.Completion.DslCompleter]::PrependDslCompletions($result, $ast, $tokens, $positionOfCursor, $options)

    return $result
}

# Use an event to set the OnRemove script
$null = Register-EngineEvent -SourceIdentifier PowerShell.OnIdle -MaxTriggerCount 1 -Action {
    (Get-Module PSArm).OnRemove = {
        Set-Item Function:\TabExpansion2 (Get-Content -Raw Function:__OldTabExpansion2)
        Remove-Item Function:__OldTabExpansion2
    }
}