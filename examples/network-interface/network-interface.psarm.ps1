
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.


# Copyright (c) Microsoft Corporation.

# Specify the ARM template purely within PowerShell
Arm {
    param(
        # ValidateSet is turned into "allowedValues"
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]
        $rgLocation,

        [ArmParameter[string]]
        $namePrefix = 'my',

        [ArmVariable]
        $vnetNamespace = 'myVnet/'
    )

    # Use existing PowerShell concepts to make ARM easier
    $PSDefaultParameterValues['Resource:Location'] = $rgLocation

    # Resources types, rather than being <Namespace>/<Type> have this broken into -Namespace <Namespace> -Type <Type>
    # Completions are available for Namespace and ApiVersion, and once these are specified, also for Type
    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Namespace Microsoft.Network -ApiVersion 2019-11-01 -Type virtualNetworks/subnets {
        Properties {
            # Each resource defines its properties as commands within its own body
            AddressPrefix 10.0.0.0/24
        }
    }

    # Piping, looping and commands like ForEach-Object all work
    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type publicIpAddresses {
            Properties {
                PublicIPAllocationMethod Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type networkInterfaces {
        Properties {
            # IpConfigurations is an array property, but PSArm knows this
            # All occurences of array properties will be collected into an array when the template is published
            IpConfigurations {
                Name 'myConfig'
                properties {
                    PrivateIPAllocationMethod Dynamic

                    # ARM expressions can be expressed in PowerShell
                    # The subnet ID here is: [resourceId('Microsoft.Network/virtualNetworks/subnets', concat(variables('vnetNamespace'), variables('namePrefix'), '-subnet'))]
                    Subnet {
                        id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
                    }
                }
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}
