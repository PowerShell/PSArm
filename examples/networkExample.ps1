
# Copyright (c) Microsoft Corporation.
# All rights reserved.

param(
  [string]
  $OutTemplatePath = './networkExampleTemplate.json'
)

$template = Arm {
    param(
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]$rgLocation,

        [ArmParameter]$namePrefix = 'my',

        [ArmVariable]$vnetNamespace = 'myVnet/'
    )

    $PSDefaultParameterValues['Resource:Location'] = $rgLocation
    $PSDefaultParameterValues['Resource:ApiVersion'] = '2019-11-01'

    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Provider Microsoft.Network -Type virtualNetworks/subnets {
        Properties {
            AddressPrefix 10.0.0.0/24
        }
    }

    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -Provider Microsoft.Network -Type publicIPAddresses {
            Properties {
                PublicIPAllocationMethod Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -Provider Microsoft.Network -Type networkInterfaces {
        Properties {
            IpConfiguration -Name 'myConfig' -PrivateIPAllocationMethod Dynamic {
                Subnet -Id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}

Publish-ArmTemplate -Template $template -OutFile $OutTemplatePath -Parameters @{
    rgLocation = 'WestUS2'
}
