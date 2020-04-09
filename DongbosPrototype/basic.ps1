$contentVersion = '1.0.0.0'
$apiVersion = '2019-11-01'

Template -ContentVersion $contentVersion {
    param(
        [string]$rgLocation,
        [string]$namePrefix
    )

    $mySubnet = Concat 'myVnet/' $namePrefix '-subnet'
    Resource -Name $mySubnet `
             -Location $rgLocation `
             -ApiVersion $apiVersion `
             -Type "Microsoft.Network/virtualNetworks/subnets" {
        Property 'addressPrefix' '10.0.0.0/24'
    }

    '-pip1', '-pip2' | Concat $namePrefix |
        Resource -Location $rgLocation -ApiVersion $apiVersion -Type 'Microsoft.Network/publicIpAddresses' {
            Property 'publicIPAllocationMethod' 'Dynamic'
        }

    $myNic = Concat $namePrefix '-nic'
    Resource -Name $myNic `
             -Location $rgLocation `
             -ApiVersion $apiVersion `
             -Type 'Microsoft.Network/networkInterfaces' {

        ## 'Property' should be smart about its context.
        ## For example, within this resource context, it should know 'ipConfigurations' expects an array.
        Property 'ipConfigurations' {
            NetworkInterfaceIPConfiguration -Name 'myConfig' {
                Property 'subnet' {
                    Subnet -Id (ResourceId $mySubnet)
                }
                Property 'privateIPAllocationMethod' 'Dynamic'
            }
        }
    }

    Output -Type string 'nicResourceId' (ResourceId $myNic)
}
