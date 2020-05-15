
# Copyright (c) Microsoft Corporation.
# All rights reserved.

@{
    'Microsoft.Storage' = @{
        'storageAccounts' = @{
        }
    }

    'Microsoft.Network' = @{
        'publicIPAddresses' = @{
            'publicIPAllocationMethod' = @("Static", "Dynamic")
            'publicIPAddressVersion' = @('IPv4', 'IPv6')
            'ipAddress' = [string]
            'idleTimeoutInMinutes' = [int]
            'resourceGuid' = [guid]
            'provisioningState' = @('Updating', 'Deleting', 'Failed')
        }
    }

    'Microsoft.Compute' = @{

    }
}