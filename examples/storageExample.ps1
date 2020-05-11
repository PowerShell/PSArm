
$t = Arm {
    param(
        # Storage account type
        [ValidateSet('Standard_LRS', 'Standard_GRS', 'Standard_ZRS', 'Premium_LRS')]
        [ArmParameter[string]]
        $storageAccountType = 'Standard_LRS',

        [ArmParameter[string]]
        $location = (ResourceGroup).Location,

        [ArmVariable]
        $storageAccountName = (Concat 'storage' (UniqueString (ResourceGroup).Id))
    )

    Resource -Name $storageAccountName -Location $location -ApiVersion 2018-07-01 -Type Microsoft.Storage/storageAccounts -Kind 'StorageV2' {
        Sku -Name $storageAccountType
    }

    Output 'storageAccountName' -Type 'string' -Value $storageAccountName
}

$t.ToString()