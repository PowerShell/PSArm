
# Copyright (c) Microsoft Corporation.
# All rights reserved.

Import-Module "$PSScriptRoot\PsArm\bin\Debug\netstandard2.0\PsArm.dll"

Import-Module "$PSScriptRoot\ArmFunctions.psm1" -Force

Import-Module "$PSScriptRoot\ArmResources.psm1" -Force

function global:TabExpansion2
{
    [CmdletBinding(DefaultParameterSetName = 'ScriptInputSet')]
    Param(
        [Parameter(ParameterSetName = 'ScriptInputSet', Mandatory = $true, Position = 0)]
        [string] $inputScript,

        [Parameter(ParameterSetName = 'ScriptInputSet', Mandatory = $true, Position = 1)]
        [int] $cursorColumn,

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

    End
    {
        if ($psCmdlet.ParameterSetName -eq 'ScriptInputSet')
        {
            return [PsArm.ArmDslCompletions]::CompleteAnyInput($inputScript, $cursorColumn, $options)
        }

        return [PSArm.ArmDslCompletions]::CompleteAnyInput($ast, $tokens, $positionOfCursor, $options)
    }
}

function Parameter
{
    param(
        [Parameter(Position=0)]
        $DefaultValue,

        [Parameter()]
        $Type = 'string',

        [Parameter()]
        [switch]
        $Secure,

        [Parameter()]
        [PsArm.ArmValue[]]
        $AllowedValues,

        [Parameter()]
        $MaxValue = -1,

        [Parameter()]
        $MinValue = -1,

        [Parameter()]
        $MaxLength = -1,

        [Parameter()]
        $MinLength = -1,

        [Parameter()]
        [PsArm.ArmObjectValue]
        $Metadata
    )

    switch ($Type)
    {
        'string'
        {
            return [PsArm.ArmStringParameter]::new($DefaultValue, $AllowedValues, $Metadata, $Secure, $MinLength, $MaxLength)
        }

        'int'
        {
            return [PsArm.ArmIntParameter]::new($DefaultValue, $AllowedValues, $Metadata, $MinValue, $MaxValue)
        }

        'bool'
        {
            return [PsArm.ArmBoolParameter]::new($DefaultValue, $AllowedValues, $Metadata)
        }

        'object'
        {
            return [PsArm.ArmObjectParameter]::new($Secure, $DefaultValue, $AllowedValues, $Metadata)
        }

        'array'
        {
            return [PsArm.ArmArrayParameter]::new($DefaultValue, $AllowedValues, $Metadata, $MinLength, $MaxLength)
        }
    }

    if ($DefaultValue)
    {
        $defaultType = $DefaultValue.GetType()

        if ($defaultType -eq [string])
        {
            return [PsArm.ArmStringParameter]::new($DefaultValue, $AllowedValues, $Metadata, $Secure, $MinLength, $MaxLength)
        }

        if ($defaultType -eq [bool])
        {
            return [PsArm.ArmBoolParameter]::new($DefaultValue, $AllowedValues, $Metadata)
        }
    }
}

function ArmVariable
{
    param(
        [Parameter()]
        $Value
    )

    return [PsArm.ArmVariable]::new($Value)
}

function BuildArm
{
    param(
        [Parameter(Position=0)]
        [scriptblock]
        $ArmDslTemplate
    )

    $definedObjects = & {
        # evil hack
        $sb = $ArmDslTemplate.Ast.GetScriptBlock()
        . $sb

        Wait-Debugger

        Get-Variable |
            Where-Object { $_.Value -is [PsArm.ArmParameter] -or $_.Value -is [PsArm.ArmVariable] } |
            ForEach-Object { $_.Value.Name = $_.Name; $_.Value }
    }

    $armTemplate = [PsArm.ArmTemplate]::new()

    foreach ($obj in $definedObjects)
    {
        if ($obj -is [PsArm.ArmParameter])
        {
            $armTemplate.Parameters.Add($obj.Name, $obj)
            continue
        }

        if ($obj -is [PsArm.ArmVariable])
        {
            $armTemplate.Variables.Add($obj.Name, $obj)
            continue
        }

        if ($obj -is [PsArm.ArmResourceBuilder])
        {
            $armTemplate.Resources.Add($obj)
            continue
        }
    }

    return $armTemplate
}