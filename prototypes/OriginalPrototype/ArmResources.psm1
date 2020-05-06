$script:defaultApiVersion = '2018-11-01'

function BuildResource
{
    param(
        [Parameter()]
        $Condition,

        [Parameter(Mandatory)]
        $ApiVersion,

        [Parameter(Mandatory)]
        $Type,

        [Parameter(Mandatory)]
        $Name,

        [Parameter()]
        $Location,

        [Parameter()]
        [hashtable]
        $Sku,

        [Parameter()]
        $Kind,

        [Parameter()]
        $DependsOn,

        [Parameter(ValueFromRemainingArguments)]
        [scriptblock]
        $Properties
    )
    
    $builder = [PsArm.ArmResourceBuilder]::new($Name, $Type, $ApiVersion)

    if ($Location)
    {
        $builder.Location = $Location
    }

    if ($Kind)
    {
        $builder.Kind = $Kind
    }

    if ($Sku)
    {
        $builder.Sku = $Sku
    }

    if ($DependsOn)
    {
        foreach ($dependedItem in $DependsOn)
        {
            $builder.DependsOn.Add($dependedItem)
        }
    }

    if ($Properties)
    {
        foreach ($property in & ($Properties.Ast.GetScriptBlock()))
        {
            $builder.Properties.AddProperty($property)
        }
    }

    return $builder
}

function BuildNamespace
{
    param(
        [Parameter(Position=0)]
        [string]
        $Namespace,

        [Parameter(Position=1)]
        [scriptblock]
        $Resources
    )

    $namespaceBuilder = [PsArm.ArmResourceNamespace]::new($Namespace)

    foreach ($resource in (& ($Resources.Ast.GetScriptBlock())))
    {
        if ($resource -is [PsArm.ArmResourceBuilder])
        {
            $namespaceBuilder.AddResource($resource)
        }
    }

    $namespaceBuilder
}

function BuildProperties
{
    param(
        [scriptblock]
        $Properties
    )

    $propertyBuilder = [PsArm.ArmPropertyBuilder]::new()

    foreach ($property in (& ($properties.Ast.GetScriptBlock())))
    {
        if ($property -is [PsArm.IArmProperty])
        {
            $propertyBuilder.AddProperty($property)
        }
    }

    $propertyBuilder
}

function Microsoft.Storage
{
    param(
        [Parameter(Position=0)]
        [scriptblock]
        $Body
    )

    function StorageAccounts
    {
        param(
            [Parameter(Position=0, Mandatory)]
            $Name,

            [Parameter(Position=1, Mandatory)]
            $Location,

            [Parameter()]
            [hashtable]
            $Sku,

            [Parameter()]
            $Kind
        )

        BuildResource -ApiVersion $script:defaultApiVersion -Type 'storageAccounts' @PSBoundParameters
    }

    BuildNamespace 'Microsoft.Storage' $Body
}

function Microsoft.Compute
{
    param(
        [Parameter(Position=0)]
        [scriptblock]
        $Body
    )

    BuildNamespace 'Microsoft.Compute' $Body
}

function Microsoft.Network
{
    param(
        [Parameter(Position=0)]
        [scriptblock]
        $Body
    )

    function PublicIPAddresses
    {
        param(
            [Parameter(Position=0)]
            $Name,

            [Parameter(Position=1)]
            $Location,

            [Parameter(ValueFromRemainingArguments)]
            [scriptblock]
            $Properties
        )

        function PublicIPAllocationMethod
        {
            param(
                [Parameter(Position=0)]
                $Method
            )

            [PsArm.ArmSimpleProperty]::new('publicIPAllocationMethod', $Method)
        }

        function DnsSettings
        {
            param(
                [Parameter(ValueFromRemainingArguments)]
                [scriptblock]
                $Settings
            )

            function DomainNameLabel
            {
                param(
                    [Parameter(Position=0)]
                    $DnsLabelPrefix
                )

                [PsArm.ArmSimpleProperty]::new('domainNameLabel', $DnsLabelPrefix)
            }

            BuildProperties $Settings
        }

        BuildResource -ApiVersion $script:defaultApiVersion -Type 'virtualNetworks' @PSBoundParameters
    }

    Wait-Debugger

    BuildNamespace 'Microsoft.Network' $Body
}
