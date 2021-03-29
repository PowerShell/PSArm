param(
  [Parameter(Mandatory)]
  [string]
  $StorageAccountName,

  [Parameter()]
  [ValidateSet('WestUS2', 'CentralUS')]
  [string]
  $Location = 'WestUS2'
)

Arm {
  param(
    [ValidateSet('Hot', 'Cool', 'Archive')]
    [ArmParameter[string]]
    $accessTier = 'Hot',

    [ArmParameter[int]]
    $allowPublicAccess,

    [ArmParameter[int]]
    $httpsOnly,

    [ArmVariable]
    $deploymentTime = (utcNow)
  )

  Resource $StorageAccountName -Namespace Microsoft.Storage -Type storageAccounts -ApiVersion 2019-06-01 -Kind StorageV2 -Location $Location {
    ArmSku Standard_LRS
    properties {
      accessTier $accessTier
      supportsHTTPSTrafficOnly $httpsOnly
      allowBlobPublicAccess $allowPublicAccess
      allowSharedKeyAccess 1
    }
  }

  Output 'deploymentTime' -Type string -Value $deploymentTime
}
