$contentVersion = '1.0.0.0'
$apiVersion = '2019-11-01'

ArmTemplate -ContentVersion $contentVersion {
    param(
        [string]$rgLocation,
        [string]$namePrefix
    )

    $mySubnet = (Concat 'myVnet/' $namePrefix '-subnet')
    Resource -Name $mySubnet `
             -Location $rgLocation `
             -ApiVersion $apiVersion `
             -Type "Microsoft.Network/virtualNetworks/subnets" {
        AddressPrefix '10.0.0.0/24'
    }

    '-pip1', '-pip2' | ForEach-Object { Concat $namePrefix $_ } |
        Resource -Location $rgLocation -ApiVersion $apiVersion -Type 'Microsoft.Network/publicIpAddresses' {
            PublicIPAllocationMethod 'Dynamic'
        }

    $myNic = (Concat $namePrefix '-nic')
    Resource -Name $myNic `
             -Location $rgLocation `
             -ApiVersion $apiVersion `
             -Type 'Microsoft.Network/networkInterfaces' {

        ## 'Property' should be smart about its context.
        ## For example, within this resource context, it should know 'ipConfigurations' expects an array.
        IpConfigurations {
            NetworkInterfaceIPConfiguration -Name 'myConfig' {
                Subnet -Id (ResourceId $mySubnet)
                PrivateIPAllocationMethod 'Dynamic'
            }
        }
    }

    Output -Type string 'nicResourceId' (ResourceId $myNic)
}
