param(
  [string]
  $AdminUsername,

  [string]
  $AdminPassword,

  [string]
  $PublicIPName = 'myPublicIP',

  [ValidateSet('Dynamic', 'Static')]
  [string]
  $PublicIPAllocationMethod = 'Dynamic',

  [ValidateSet('Basic', 'Standard')]
  [string]
  $PublicIPSku = 'Basic',

  [ValidateSet('2008-R2-SP1', '2012-Datacenter', '2012-R2-Datacenter', '2016-Nano-Server', '2016-Datacenter-with-Containers', '2016-Datacenter', '2019-Datacenter', '2019-Datacenter-Core', '2019-Datacenter-Core-smalldisk', '2019-Datacenter-Core-with-Containers', '2019-Datacenter-Core-with-Containers-smalldisk', '2019-Datacenter-smalldisk', '2019-Datacenter-with-Containers', '2019-Datacenter-with-Containers-smalldisk')]
  [string]
  $OSVersion = '2019-Datacenter',

  [string]
  $VMSize = 'Standard_D2_v3',

  [string]
  $VMName = 'simple-vm'
)

$nicName = 'myVMNic'
$addressPrefix = '10.0.0.0/16'
$subnetName = 'Subnet'
$subnetPrefix = '10.0.0.0/24'
$virtualNetworkName = 'MyVNET'
$networkSecurityGroupName = 'default-NSG'

Arm {
  param(
    [ArmParameter[string]]
    $dnsLabelPrefix = (toLower (concat $vmName '-' (uniqueString (resourceGroup).id $vmName))),

    [ArmParameter[string]]
    $location = (resourceGroup).location,

    [ArmVariable]
    $storageAccountName = (concat 'bootdiags' (uniquestring (resourceGroup).id)),

    [ArmVariable]
    $subnetRef = (resourceId 'Microsoft.Network/virtualNetworks/subnets' $virtualNetworkName $subnetName)
  )

  Resource $storageAccountName -Provider 'Microsoft.Storage' -Type 'storageAccounts' -ApiVersion '2019-06-01' -Location $location -Kind 'Storage' {
    Sku 'Standard_LRS'
  }
  Resource $publicIPName -Provider 'Microsoft.Network' -Type 'publicIPAddresses' -ApiVersion '2020-06-01' -Location $location {
    Sku $publicIpSku
    Properties {
      PublicIPAllocationMethod $publicIPAllocationMethod
      DnsSettings {
        DomainNameLabel $dnsLabelPrefix
      }
    }
  }
  Resource $networkSecurityGroupName -Provider 'Microsoft.Network' -Type 'networkSecurityGroups' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      SecurityRule 'default-allow-3389' {
        Priority '1000'
        Access 'Allow'
        Direction 'Inbound'
        DestinationPortRange '3389'
        Protocol 'Tcp'
        SourcePortRange '*'
        SourceAddressPrefix '*'
        DestinationAddressPrefix '*'
      }
    }
  }
  Resource $virtualNetworkName -Provider 'Microsoft.Network' -Type 'virtualNetworks' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      AddressSpace {
        AddressPrefixe $addressPrefix
      }
      Subnet $subnetName {
        AddressPrefix $subnetPrefix
        NetworkSecurityGroup {
          Id (resourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName)
        }
      }
      
    }
    DependsOn (resourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName)
  }
  Resource $nicName -Provider 'Microsoft.Network' -Type 'networkInterfaces' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      IpConfiguration 'ipconfig1' {
        PrivateIPAllocationMethod 'Dynamic'
        PublicIPAddress {
          Id (resourceId 'Microsoft.Network/publicIPAddresses' $publicIPName)
        }
        Subnet {
          Id $subnetRef
        }
      }
      
    }
    DependsOn (resourceId 'Microsoft.Network/publicIPAddresses' $publicIPName)
    DependsOn (resourceId 'Microsoft.Network/virtualNetworks' $virtualNetworkName)
  }
  Resource $vmName -Provider 'Microsoft.Compute' -Type 'virtualMachines' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      HardwareProfile {
        VmSize $vmSize
      }
      OsProfile {
        ComputerName $vmName
        AdminUsername $adminUsername
        AdminPassword $adminPassword
      }
      StorageProfile {
        ImageReference {
          Publisher 'MicrosoftWindowsServer'
          Offer 'WindowsServer'
          Sku $OSVersion
          Version 'latest'
        }
        OsDisk {
          CreateOption 'FromImage'
          ManagedDisk {
            StorageAccountType 'StandardSSD_LRS'
          }
        }
        DataDisk {
          DiskSizeGB '1023'
          Lun '0'
          CreateOption 'Empty'
        }
      }
      NetworkProfile {
        NetworkInterface {
          Id (resourceId 'Microsoft.Network/networkInterfaces' $nicName)
        }
      }
      DiagnosticsProfile {
        BootDiagnostics {
          Enabled $true
          StorageUri (reference (resourceId 'Microsoft.Storage/storageAccounts' $storageAccountName)).primaryEndpoints.blob
        }
      }
    }
    DependsOn (resourceId 'Microsoft.Storage/storageAccounts' $storageAccountName)
    DependsOn (resourceId 'Microsoft.Network/networkInterfaces' $nicName)
  }
  Output 'hostname' -Type 'string' -Value (reference $publicIPName).dnsSettings.fqdn
}