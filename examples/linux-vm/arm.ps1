param(
  [Parameter(Mandatory)]
  [string]
  $AdminUserName,

  [Parameter(Mandatory)]
  [ValidateSet('SshPublicKey', 'Password')]
  [string]
  $AuthenticationType,

  [Parameter(Mandatory)]
  [string]
  $AdminPasswordOrKey,

  [Parameter()]
  [string]
  $VMName = 'simpleLinuxVM',

  [Parameter()]
  [string]
  $VmSize = 'Standard_B2s',

  [Parameter()]
  [string]
  $virtualNetworkName = 'vNet',

  [Parameter()]
  [string]
  $subnetName = 'Subnet',

  [Parameter()]
  [string]
  $networkSecurityGroupName = 'SecGroupNet',

  [Parameter()]
  [ValidateSet('12.04.5-LTS', '14.04.5-LTS', '16.04.0-LTS', '18.04-LTS')]
  [string]
  $ubuntuOSVersion = '18.04-LTS',

  [Parameter()]
  [ValidateSet('WestUS2', 'CentralUS')]
  $location
)

$publicIPAddressName = "${VMName}PublicIP"
$networkInterfaceName = "${VMName}NetInt"
$osDiskType = 'Standard_LRS'
$subnetAddressPrefix = '10.1.0.0/24'
$addressPrefix = '10.1.0.0/16'
$linuxConfiguration = @{
  disablePasswordAuthentication = 'True'
  ssh = @{
    publicKeys = @(
      @{
        path = "/home/$AdminUsername/.ssh/authorized_keys"
        keyData = $AdminPasswordOrKey
      }
    )
  }
}

Arm {
  param(
    [ArmParameter[string]]
    $dnsLabelPrefix = (toLower (concat 'simplelinuxvm-' (uniqueString (resourceGroup).id))),
    
    [ArmParameter[string]]
    $location = (resourceGroup).location,

    [ArmVariable]
    $subnetRef = (resourceId 'Microsoft.Network/virtualNetworks/subnets' $virtualNetworkName $subnetName)
  )

  Resource $networkInterfaceName -Provider 'Microsoft.Network' -Type 'networkInterfaces' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      IpConfiguration -Name 'ipconfig1' {
        Subnet -Id $subnetRef 
        PrivateIPAllocationMethod 'Dynamic'
        PublicIpAddress -Id (resourceId 'Microsoft.Network/publicIPAddresses' $publicIPAddressName) 
      }
      
      NetworkSecurityGroup -Id (resourceId 'Microsoft.Network/networkSecurityGroups' $networkSecurityGroupName) 
    }
    DependsOn (resourceId 'Microsoft.Network/networkSecurityGroups/' $networkSecurityGroupName)
    DependsOn (resourceId 'Microsoft.Network/virtualNetworks/' $virtualNetworkName)
    DependsOn (resourceId 'Microsoft.Network/publicIpAddresses/' $publicIpAddressName)
  }
  Resource $networkSecurityGroupName -Provider 'Microsoft.Network' -Type 'networkSecurityGroups' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      SecurityRule -Name 'SSH' {
        Priority '1000'
        Protocol 'TCP'
        Access 'Allow'
        Direction 'Inbound'
        SourceAddressPrefix '*'
        SourcePortRange '*'
        DestinationAddressPrefix '*'
        DestinationPortRange '22'
      }
    }
  }
  Resource $virtualNetworkName -Provider 'Microsoft.Network' -Type 'virtualNetworks' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      AddressSpace -AddressPrefixes @($addressPrefix) 
      Subnet -Name $subnetName {
        AddressPrefix $subnetAddressPrefix
        PrivateEndpointNetworkPolicies 'Enabled'
        PrivateLinkServiceNetworkPolicies 'Enabled'
      }
    }
  }
  Resource $publicIpAddressName -Provider 'Microsoft.Network' -Type 'publicIPAddresses' -ApiVersion '2020-06-01' -Location $location {
    Sku 'Basic' -Tier 'Regional'
    Properties {
      PublicIpAllocationMethod 'Dynamic'
      PublicIPAddressVersion 'IPv4'
      DnsSettings -DomainNameLabel $dnsLabelPrefix 
      IdleTimeoutInMinutes '4'
    }
  }
  Resource $VMName -Provider 'Microsoft.Compute' -Type 'virtualMachines' -ApiVersion '2020-06-01' -Location $location {
    Properties {
      HardwareProfile {
        VmSize $VmSize 
      }
      StorageProfile {
        OsDisk {
          CreateOption 'fromImage'
          ManagedDisk {
            StorageAccountType $osDiskType
          }
        }
        ImageReference {
          Publisher 'Canonical'
          Offer 'UbuntuServer'
          Sku $ubuntuOSVersion
          Version 'latest'
        } 
      }
      NetworkProfile {
        NetworkInterface {
          Id (resourceId 'Microsoft.Network/networkInterfaces' $networkInterfaceName) 
        }
      }
      OsProfile {
        ComputerName $vmName
        AdminUsername $adminUsername
        AdminPassword $adminPasswordOrKey
        LinuxConfiguration $(if ($AuthenticationType -eq 'password') { $null } else { $linuxConfiguration })
      }
    }
    DependsOn (resourceId 'Microsoft.Network/networkInterfaces/' $networkInterfaceName)
  }
  Output 'hostname' -Type 'string' -Value (reference $publicIPAddressName).dnsSettings.fqdn
  Output 'sshCommand' -Type 'string' -Value (concat 'ssh ' $adminUsername '@' (reference $publicIPAddressName).dnsSettings.fqdn)
}