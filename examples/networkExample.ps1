#$vnetNamespace = 'myVnet/'
New-Variable vnetNamespace 'myVnet/'

$template = Arm {
    param(
        [ValidateSet('WestUS2', 'CentralUS')][string]$rgLocation,
        [string]$namePrefix
    )

    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/virtualNetworks/subnets {
        Properties {
            AddressPrefix -Prefix 10.0.0.0/24
        }
    }

    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/publicIpAddresses {
            Properties {
                PublicIPAllocationMethod -Method Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/networkInterfaces {
        Properties {
            IpConfiguration -Name 'myConfig' {
                Subnet -Id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
                PrivateIPAllocationMethod -Method Dynamic
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}

$template | % { $_.ToString() }

Publish-ArmTemplate -Template $template -Parameters @{
    rgLocation = 'WestUS2'
    namePrefix = 'rob'
} | % { $_.ToString() }