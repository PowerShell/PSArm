Resource 'MyNetwork' -Location 'Here' -ApiVersion '2019-11-01' -Type 'Microsoft.Network/networkInterfaces' {
    Properties {
        IpConfiguration -Name 'myConfig' {
            Subnet -Id 'subnetId'
            PrivateIPAllocationMethod 'Dynamic'
        }
    }
}