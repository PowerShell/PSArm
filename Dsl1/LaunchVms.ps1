
BuildArm {
    $windowsOSVersionDetails = @{
        DefaultValue = '2016-Datacenter'
        AllowedValues = @(
            "2008-R2-SP1",
            "2012-Datacenter",
            "2012-R2-Datacenter",
            "2016-Nano-Server",
            "2016-Datacenter-with-Containers",
            "2016-Datacenter",
            "2019-Datacenter"
        )
        Metadata = @{
            description = 'The Windows version for the VM. This will pick a fully patched image of this given Windows version'
        }
    }

    $adminUserName = Parameter 'GEN-USER' -Metadata @{ description = 'Username for Virtual Machine' }
    $adminPassword = Parameter 'GEN-PASSWORD' -Secure -Metadata @{ description = 'Password for Virtual Machine' }
    $dnsLabelPrefix = Parameter 'GEN-UNIQUE' -Metadata @{ description = 'Unique DNS Name for the Public IP used to access the Virtual Machine' }
    $windowsOSVersion = Parameter @windowsOSVersionDetails
    $location = Parameter -DefaultValue (ResourceGroup).Location -Metadata @{ description = 'Location for all resources' }

    $storageAccountName = ArmVariable (Concat (UniqueString (ResourceGroup).Id) 'sawinvm')
    $myNic = ArmVariable 'myVMNic'
    $addressPrefix = ArmVariable '10.0.0.0/16'
    $subnetName = ArmVariable 'Subnet'
    $subnetPrefix = ArmVariable '10.0.0.0/24'
    $publicIPAddressName = ArmVariable 'myPublicIP'
    $vmName = ArmVariable 'SimpleWinVM'
    $virtualNetworkName = ArmVariable 'MyVNET'
    $subnetRef = ArmVariable (ResourceId 'Microsoft.Network/virtualNetworks/subnets' $virtualNetworkName $subnetName)

    Microsoft.Storage {
        StorageAccounts $storageAccountName $location -Sku @{ name = 'Standard_LRS' } -Kind 'Storage'
    }

    Microsoft.Network {
        PublicIPAddresses $storageAccountName $location {
            PublicIPAllocationMethod 'Dynamic'
            DnsSettings {
                DomainNameLabel $dnsLabelPrefix
            }
        }

        VirtualNetworks $virtualNetworkName -Location $location {
            AddressSpace {
                AddressPrefixes $addressPrefix
            }
        }

        NetworkInterfaces $nicName $location -DependsOn @(
            ResourceId 'Microsoft.Network/publicIPAddresses/' $publicIPAddressName
            ResourceId 'Microsoft.Network/virtualNetworks/' $virtualNetworkName
        ) {
            IPConfiguration ipconfig1 {
                PrivateIPAllocationMethod 'Dynamic'
                PublicIPAddress (ResourceId 'Microsoft.Network/publicIPAddresses/' $publicIPAddressName)
                Subnet $subnetRef
            }
        }
    }

    Microsoft.Compute {

        VirtualMachines $vmName $location -DependsOn @(
            ResourceId 'Microsoft.Storage/storageAccounts/' $storageAccountName
            ResourceId 'Microsoft.Network/networkInterfaces/' $nicName
        ) {

            HardwareProfile {
                VMSize 'Standard_A2'
            }

            OSProfile {
                ComputerName $vmName
                AdminUsername $adminUserName
                AdminPassword $adminPassword
            }

            StorageProfile {

                ImageReference {
                    Publisher MicrosoftWindowsServer
                    Offer WindowsServer
                    Sku $windowsOSVersion
                    Version latest
                }

                OSDisk {
                    CreateOption FromImage
                }

                DataDisk {
                    DiskSizeGB 1023
                    Lun 0
                    CreateOption Empty
                }
            }

            NetworkProfile {
                NetworkInterface (ResourceId 'Microsoft.Network/networkInterfaces/' $nicName)
            }

            DiagnosticsProfile {
                BootDiagnostics {
                    StorageUri (Reference (ResourceId 'Microsoft.Storage/storageAccounts/' $storageAccountName)).primaryEndpoints.blob
                }
            }
        }
    }

    Output 'hostname' (Reference $publicIPAddressName).dnsSettings.fqdn
}