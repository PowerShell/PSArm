$template = Arm {
    param(
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]
        $rgLocation,

        [ArmParameter[string]]
        $namePrefix = 'my',

        [ArmVariable]
        $vnetNamespace = 'myVnet/'
    )

    $PSDefaultParameterValues['Resource:Location'] = $rgLocation

    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Provider Microsoft.Network -ApiVersion 2019-11-01 -Type virtualNetworks/subnets {
        Properties {
            AddressPrefix -Value 10.0.0.0/24
        }
    }

    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type publicIPAddresses {
            Properties {
                PublicIPAllocationMethod -Value Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -Provider Microsoft.Network -ApiVersion 2019-11-01 -Type networkInterfaces {
        Properties {
            IpConfiguration -Name 'myConfig' -PrivateIPAllocationMethod Dynamic {
                Subnet -Id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId ('Microsoft.Network/networkInterfaces') (Concat $namePrefix '-nic'))
}