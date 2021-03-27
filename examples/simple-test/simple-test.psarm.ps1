
param(
    [Parameter(Mandatory)]
    [string]
    $storageAccountName,
    
    [Parameter(Mandatory)]
    [string]
    $location,

    [Parameter()]
    [string]
    $accountType = 'Standard_LRS',

    [Parameter()]
    [string]
    $kind = 'StorageV2',

    [Parameter()]
    [string]
    $accessTier = 'Hot',
    
    [Parameter()]
    [string]
    $minimumTLSVersion = 'TLS1_2',
    
    [Parameter()]
    [bool]
    $supportsHTTPSTrafficOnly = 1,
    
    [Parameter()]
    [bool]
    $allowBlobPublicAccess = 1,
    
    [Parameter()]
    [bool]
    $allowSharedKeyAccess = 1
)

Arm {
    Resource $storageAccountName -Namespace 'Microsoft.Storage' -Type 'storageAccounts' -apiVersion '2019-06-01' -kind $kind -Location $location {
        ArmSku $accountType
        Properties {
            accessTier $accessTier
            minimumTLSVersion $minimumTLSVersion
            supportsHTTPSTrafficOnly $supportsHTTPSTrafficOnly
            allowBlobPublicAccess $allowBlobPublicAccess
            allowSharedKeyAccess $allowSharedKeyAccess
        }
    }
}
