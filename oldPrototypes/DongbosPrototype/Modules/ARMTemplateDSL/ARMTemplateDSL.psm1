using namespace System.Text
using namespace System.Collections.Generic
using namespace System.Reflection;
using namespace System.Management.Automation.Language

class secureObject {  }

enum ArmParamType
{
    string
    securestring
    int
    bool
    object
    secureObject
    array
}

$Script:contextStack = [stack[object]]::new()
$Script:resources = [Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
$Script:expressions = [HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

#region Helpers

function GetParamType ([string] $type)
{
    return ([enum]::Parse([ArmParamType], $type, $true)).ToString()
}

function AlterParamBlock ([scriptblock] $sb)
{
    $sbAst = $sb.Ast
    $oldParams = $sbAst.ParamBlock.Parameters
    $newParams = [List[ParameterAst]]::new()
    foreach ($param in $oldParams) {
        $newParam = [ParameterAst]::new($param.Extent, $param.Name.Copy(), $null, $null)
        $newParams.Add($newParam)
    }

    $newParamBlock = [ParamBlockAst]::new($sbAst.ParamBlock.Extent, $null, $newParams)
    $newEndBlock = $sbAst.EndBlock.Copy()
    $newSBAst = [ScriptBlockAst]::new($sbAst.Extent, $newParamBlock, $null, $null, $newEndBlock, $null)

    return $newSBAst.GetScriptBlock()
}

function GetSessionState ([scriptblock] $sb)
{
    $flags = [BindingFlags]::NonPublic -bor [BindingFlags]::Instance
    $p1 = [scriptblock].GetProperty("SessionStateInternal", $flags)
    return $p1.GetValue($sb)
}

function SetSessionState ([scriptblock] $sb, $ssi)
{
    $flags = [BindingFlags]::NonPublic -bor [BindingFlags]::Instance
    $p1 = [scriptblock].GetProperty("SessionStateInternal", $flags)
    $p1.SetValue($sb, $ssi)
}

function QuoteAsNeeded ([string] $value)
{
    if ($Script:expressions.Contains($value)) {
        return $value.Trim([char[]]('[',']'))
    }

    return "'$value'"
}

function Concat
{
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [string] $Item1,

        [Parameter(Mandatory, Position = 1, ValueFromPipeline)]
        [string] $Item2,

        [Parameter(ValueFromRemainingArguments)]
        [string[]] $AdditionalItems
    )

    Process {
        $list = [List[string]]::new()
        $list.Add((QuoteAsNeeded $Item1))
        $list.Add((QuoteAsNeeded $Item2))

        foreach ($item in $AdditionalItems) {
            $list.Add((QuoteAsNeeded $item))
        }

        $combined = $list -join ", "
        $retValue = "[concat($combined)]"
        $Script:expressions.Add($retValue) > $null

        Write-Output $retValue
    }
}

function ResourceId
{
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $ResourceName
    )

    $resType = QuoteAsNeeded $Script:resources[$ResourceName].Type
    $resName = QuoteAsNeeded $ResourceName

    $resId = "[resourceId($resType, $resName)]"
    $Script:expressions.Add($resId) > $null

    if ($Script:contextStack.Count -gt 0) {
        $context = $Script:contextStack.Peek()
        $context.ResourceRef.DependsOn += $resId
    }

    return $resId
}

function GetModuleName ([string] $ResourceType)
{
    $ResourceType -replace '/', '.'
}

#endregion

function Template
{
    param(
        [Parameter(Mandatory)]
        [string] $ContentVersion,

        [Parameter(Mandatory, Position = 0)]
        [scriptblock] $Body
    )

    try {
        $template = [ordered]@{
            '$schema' = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
            contentVersion = $ContentVersion
        }

        if ($null -ne $Body.Ast.ParamBlock) {
            $params = $Body.Ast.ParamBlock.Parameters
            $armParameters = [ordered]@{}
            $arguments = @()

            foreach ($param in $params) {
                $name = $param.Name.VariablePath.UserPath
                $type = $param.StaticType.Name
                $armParameters[$name] = @{ type = GetParamType -type $type }

                $argument = "[parameters('$name')]"
                $Script:expressions.Add($argument) > $null
                $arguments += $argument
            }

            $template['parameters'] = $armParameters
            $newBody = AlterParamBlock $Body
            $ssi = GetSessionState -sb $Body
            SetSessionState -sb $newBody -ssi $ssi

            $results = & $newBody @arguments
        }
        else {
            $results = & $Body
        }

        if ($null -ne $results) {
            $resources = [List[object]]::new()
            $outputs = @{}

            foreach ($item in $results) {
                switch ($item.Type) {
                    'Resource' { $resources.Add($item.Payload) > $null }
                    'output'   { $outputs.Add($item.Name, $item.Value) > $null }
                }
            }

            if ($resources.Count -ne 0) {
                $template['resources'] = $resources
            }

            if ($outputs.Count -ne 0) {
                $template['outputs'] = $outputs
            }
        }

        $template | ConvertTo-Json -Depth 10
    }
    finally {
        $Script:contextStack.Clear()
        $Script:resources.Clear()
        $Script:expressions.Clear()
    }
}

function Output
{
    [OutputType([hashtable])]
    param(
        [ArmParamType] $Type = 'string',

        [Parameter(Mandatory, Position = 0)]
        [string] $Name,

        [Parameter(Mandatory, Position = 1)]
        [string] $Value
    )

    $retValue = [ordered]@{ type = $Type.ToString(); value = $Value }
    return @{ Name = $Name; Value = $retValue; Type = 'output' }
}

function Resource
{
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $ApiVersion,

        [Parameter(Mandatory)]
        [string] $Location,

        [Parameter(Mandatory)]
        [string] $Type,

        [Parameter(Position = 0)]
        [scriptblock] $Body
    )

    Process {
        $expando = [ordered]@{
            apiVersion = $ApiVersion
            type = $Type
            name = $Name
            location = $Location
        }

        if ($null -ne $Body) {
            try {
                $context = @{
                    ResourceType = $Type
                    SchemaName = ''
                    DependsOn = @()
                }
                $Script:contextStack.Push($context)

                $properties = [ordered]@{}
                $results = & $Body
                switch ($results) {
                    { $_.Type -eq 'Property' } {
                        $properties[$_.Name] = $_.Value
                    }
                    default { throw "$($_.Type) not supported yet." }
                }

                if ($properties.Count -gt 0) {
                    $expando.properties = $properties
                }

                if ($context.DependsOn.Count -ne 0) {
                    $expando.dependsOn = $context.DependsOn
                }
            }
            finally {
                $Script:contextStack.Pop() > $null
            }
        }

        $Script:resources.Add($expando.Name, $expando)
        Write-Output @{ Type = 'Resource'; Payload = $expando }
    }
}

function Property
{
    [OutputType([hashtable])]
    [CmdletBinding(DefaultParameterSetName = 'Simple')]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $Name,

        [Parameter(Mandatory, Position = 1, ParameterSetName = 'Simple')]
        [string] $Value,

        [Parameter(Mandatory, Position = 1, ParameterSetName = 'Complex')]
        [scriptblock] $Body
    )

    if ($PSCmdlet.ParameterSetName -eq 'Simple') {
        return @{ Name = $Name; Value = $Value; Type = 'Property' }
    }

    $context = $Script:contextStack.Peek()
    $moduleName = GetModuleName -ResourceType $context.ResourceType

    $schema = & "$moduleName\Get-Schema" -name $context.SchemaName
    $expectArray = $schema[$Name].Type -eq 'array'

    try {
        $Script:contextStack.Push(@{
            ResourceType = $context.ResourceType
            SchemaName = $schema[$Name].Command
            ResourceRef = $context.ResourceRef ?? $context
        })

        $results = @(& $Body)

        if ($results.Length -gt 1) {
            return @{ Name = $Name; Value = $results; Type = 'Property' }
        }
        elseif ($results.Length -eq 1) {
            if (-not $expectArray) {
                $results = $results[0]
            }
            return @{
                Name = $Name
                Value = $results
                Type = 'Property'
            }
        }
    }
    finally {
        $Script:contextStack.Pop() > $null
    }
}

## dot source the completion helpers.
. $PSScriptRoot/ARMTemplateCompletion.ps1
