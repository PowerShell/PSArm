
# Copyright (c) Microsoft Corporation.

param(
  [string]
  $NicName = 'myVMNic',

  [string]
  $AddressPrefix = '10.0.0.0/16',

  [string]
  $SubnetName = 'Subnet',

  [string]
  $SubnetPrefix = '10.0.0.0/24',

  [string]
  $virtualNetworkName = 'MyVNET',

  [string]
  $networkSecurityGroupName = 'default-NSG'
)

Arm {
  param(
    [ArmParameter[string]]
    $adminUsername,

    [ArmParameter[securestring]]
    $adminPassword,

    [ArmParameter[string]]
    $vmName = 'simple-vm',

    [ArmParameter[string]]
    $dnsLabelPrefix = (toLower (concat $vmName '-' (uniqueString (resourceGroup).id $vmName))),

    [ArmParameter[string]]
    $publicIpName = 'myPublicIP',

    [ValidateSet('Dynamic', 'Static')]
    [ArmParameter[string]]
    $publicIPAllocationMethod = 'Dynamic',

    [ValidateSet('Basic', 'Standard')]
    [ArmParameter[string]]
    $publicIpSku = 'Basic',

    [ValidateSet('2008-R2-SP1', '2012-Datacenter', '2012-R2-Datacenter', '2016-Nano-Server', '2016-Datacenter-with-Containers', '2016-Datacenter', '2019-Datacenter', '2019-Datacenter-Core', '2019-Datacenter-Core-smalldisk', '2019-Datacenter-Core-with-Containers', '2019-Datacenter-Core-with-Containers-smalldisk', '2019-Datacenter-smalldisk', '2019-Datacenter-with-Containers', '2019-Datacenter-with-Containers-smalldisk')]
    [ArmParameter[string]]
    $OSVersion = '2019-Datacenter',

    [ArmParameter[string]]
    $vmSize = 'Standard_D2_v3',

    [ArmParameter[string]]
    $location = (resourceGroup).location,

    [ArmVariable]
    $storageAccountName = (concat 'bootdiags' (uniquestring (resourceGroup).id)),

    [ArmVariable]
    $subnetRef = (resourceId 'Microsoft.Network/virtualNetworks/subnets' $virtualNetworkName $subnetName)
  )

  Resource $storageAccountName -Namespace 'Microsoft.Storage' -Type 'storageAccounts' -ApiVersion '2019-06-01' -Location $location -Kind 'Storage' {
    ArmSku 'Standard_LRS'
  }

  Resource $publicIPName -Namespace 'Microsoft.Network' -Type 'publicIPAddresses' -ApiVersion '2020-06-01' -Location $location {
    ArmSku $publicIpSku
    properties {
      publicIPAllocationMethod $publicIPAllocationMethod
      dnsSettings {
        domainNameLabel $dnsLabelPrefix
      }
    }
  }

  Resource $networkSecurityGroupName -Namespace 'Microsoft.Network' -Type 'networkSecurityGroups' -ApiVersion '2020-06-01' -Location $location {
    properties {
      securityRules {
        name 'default-allow-3389'
        properties {
          priority 1000
          access 'Allow'
          direction 'Inbound'
          destinationPortRange '3389'
          protocol 'Tcp'
          sourcePortRange '*'
          sourceAddressPrefix '*'
          destinationAddressPrefix '*'
        }
      }
    }
  }

  Resource $virtualNetworkName -Namespace 'Microsoft.Network' -Type 'virtualNetworks' -ApiVersion '2020-06-01' -Location $location {
    properties {
      addressSpace {
        addressPrefixes $addressPrefix
      }
      subnets {
        name $subnetName
        properties {
          addressPrefix $subnetPrefix
          networkSecurityGroup {
            id (resourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName)
          }
        }
      }
    }
    DependsOn (resourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName)
  }

  Resource $nicName -Namespace 'Microsoft.Network' -Type 'networkInterfaces' -ApiVersion '2020-06-01' -Location $location {
    properties {
      ipConfigurations {
        name 'ipconfig1'
        properties {
          privateIPAllocationMethod 'Dynamic'
          publicIPAddress {
            id (resourceId 'Microsoft.Network/publicIPAddresses' $publicIPName)
          }
          subnet {
            id $subnetRef
          }
        }
      }
    }
    DependsOn @(
      (resourceId 'Microsoft.Network/publicIPAddresses' $publicIPName),
      (resourceId 'Microsoft.Network/virtualNetworks' $virtualNetworkName)
    )
  }

  Resource $vmName -Namespace 'Microsoft.Compute' -Type 'virtualMachines' -ApiVersion '2020-06-01' -Location $location {
    properties {
      hardwareProfile {
        vmSize $vmSize
      }
      osProfile {
        computerName $vmName
        adminUsername $adminUsername
        adminPassword $adminPassword
      }
      storageProfile {
        imageReference {
          publisher 'MicrosoftWindowsServer'
          offer 'WindowsServer'
          sku $OSVersion
          version 'latest'
        }
        osDisk {
          createOption 'FromImage'
          managedDisk {
            storageAccountType 'StandardSSD_LRS'
          }
        }
        dataDisks {
          diskSizeGB 1023
          lun 0
          createOption 'Empty'
        }
      }
      networkProfile {
        networkInterfaces {
          id (resourceId 'Microsoft.Network/networkInterfaces' $nicName)
        }
      }
      diagnosticsProfile {
        bootDiagnostics {
          enabled $true
          storageUri (reference (resourceId 'Microsoft.Storage/storageAccounts' $storageAccountName)).primaryEndpoints.blob
        }
      }
    }

    DependsOn @(
      resourceId 'Microsoft.Storage/storageAccounts' $storageAccountName
      resourceId 'Microsoft.Network/networkInterfaces' $nicName
    )
  }

  Output 'hostname' -Type 'string' -Value (reference $publicIPName).dnsSettings.fqdn
}