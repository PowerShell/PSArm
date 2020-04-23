
function IpConfiguration
{
    param([string]$Name, [scriptblock]$Body)

    function Subnet
    {
        param([string]$Id)

        Composite 'subnet' $PSBoundParameters
    }

    function PrivateIPAllocationMethod
    {
        param([ValidateSet('Static', 'Dynamic')][string]$Method)

        Value 'privateIPAllocationMethod' $Method
    }

    ArrayItem 'ipConfiguration' $PSBoundParameters $Body
}