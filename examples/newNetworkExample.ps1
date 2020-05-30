$template = Arm {
    param(
        [ArmParameter[string]]
        $location = 'westus',

        [ArmParameter[string]]
        $networkInterfaceName = 'networkInterface',

        [ArmParameter[string]]
        $networkSecurityGroupName = 'networkSecurityGroup',

        [ArmParameter[string]]
        $publicIPAddressName = 'publicIPAddress',

        [ArmParameter[string]]
        $subnetRef = 'subnetRef'
    )

    Resource -Name $networkInterfaceName -Type Microsoft.Network/networkInterface -ApiVersion 2019-11-01 -Location $location {
        Properties {
            networkSecurityGroup -id (ResourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName)
        }
    }
}