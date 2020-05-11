function IPConfigurations
{
    param([Parameter(Position=0)][scriptblock]$Body)

    function NetworkInterfaceIPConfiguration
    {
        param([Parameter(Position=0)][string]$Name, [Parameter(Position=1)][scriptblock]$Body)

        function Subnet
        {
            param([string]$Id)

            "subnet: $Id"
        }

        function PrivateIPAllocationMethod
        {
            param([ValidateSet('Static', 'Dynamic')][string]$Method)

            "privateIPAllocationMethod: $Method"
        }

        & $Body
    }

    & $Body
}
