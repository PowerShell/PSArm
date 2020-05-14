$template = Arm {
    param(
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]$rgLocation,

        [ArmParameter]$namePrefix = 'my',

        [ArmVariable]$vnetNamespace = 'myVnet/'
    )

    $PSDefaultParameterValues['Resource:Location'] = $rgLocation
    $PSDefaultParameterValues['Resource:ApiVersion'] = '2019-11-01'

    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Type Microsoft.Network/virtualNetworks/subnets {
        Properties {
            AddressPrefix -Prefix 10.0.0.0/24
        }
    }

    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -Type Microsoft.Network/publicIpAddresses {
            Properties {
                PublicIPAllocationMethod -Method Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -Type Microsoft.Network/networkInterfaces {
        Properties {
            IpConfiguration -Name 'myConfig' {
                Subnet -Id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
                PrivateIPAllocationMethod -Method Dynamic
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}

Publish-ArmTemplate -Template $template -OutFile ./template.json -Parameters @{
    rgLocation = 'WestUS2'
}