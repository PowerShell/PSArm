function NewExpression
{
    param([Parameter(Position=0)][PsArm.ArmExpression]$Expression)

    New-Object 'PsArm.ArmExpressionBuilder' $Expression
}

function BuildFunction
{
    param(
        [Parameter(Position=0)][string]$FunctionName,
        [Parameter(ValueFromRemainingArguments)]$Arguments = @()
    )

    $acc = New-Object 'System.Collections.Generic.List[PsArm.ArmExpression]'

    foreach ($arg in $Arguments)
    {
        if ($arg -is [PsArm.ArmExpression] -or $arg -is [PsArm.ArmVariable] -or $arg -is [PsArm.ArmParameter])
        {
            $acc.Add($arg)
            continue
        }

        if ($arg -is [PsArm.ArmExpressionBuilder])
        {
            $acc.Add($arg.GetArmExpression())
            continue
        }

        if ($arg -is [int] -or $arg -is [double] -or $arg -is [decimal])
        {
            $acc.Add((New-Object 'PsArm.ArmNumberLiteralExpression' $arg))
            continue
        }

        if ($arg -is [string])
        {
            $acc.Add((New-Object 'PsArm.ArmStringLiteralExpression' $arg))
            continue
        }

        throw "Unknown value for ARM expression usage: $arg"
    }

    NewExpression (New-Object 'PsArm.ArmFunctionCallExpression' $FunctionName,$acc)
}

function Concat
{
    param([Parameter(ValueFromRemainingArguments)]$Arguments)

    BuildFunction 'concat' $Arguments
}

function Variables
{
    param($VariableName)

    BuildFunction 'variables' $VariableName
}

function Parameters
{
    param($ParameterName)

    BuildFunction 'parameters' $ParameterName
}

function ResourceId
{
    param(
        [Parameter()]
        $SubscriptionId = $null,

        [Parameter()]
        $ResourceGroupName = $null,

        [Parameter(Position=0, Mandatory)]
        $ResourceType,

        [Parameter(Mandatory, ValueFromRemainingArguments)]
        [object[]]
        $ResourceNames
    )

    $acc = New-Object 'System.Collections.Generic.List[object]'
    
    if ($SubscriptionId) { $acc.Add($SubscriptionId) }
    if ($ResourceGroupName) { $acc.Add($ResourceGroupName) }

    $acc.Add($ResourceType)

    $acc.AddRange($ResourceNames)

    BuildFunction 'resourceId' ($acc.ToArray())
}

function ResourceGroup
{
    BuildFunction 'resourceGroup'
}

function Reference
{
    param(
        [Parameter(Mandatory, Position=0)]
        $Resource,

        [Parameter()]
        $ApiVersion,

        [switch]
        $Full
    )

    $acc = New-Object 'System.Collections.Generic.List[object]' (@($Resource))

    if ($ApiVersion) { $acc.Add($ApiVersion) }

    if ($Full) { $acc.Add('Full') }

    BuildFunction 'reference' ($acc.ToArray())
}

function UniqueString
{
    param(
        [Parameter(ValueFromRemainingArguments, Mandatory)]
        [object[]]
        $Arguments
    )

    BuildFunction 'uniqueString' $Arguments
}