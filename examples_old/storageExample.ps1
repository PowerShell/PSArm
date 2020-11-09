
# Copyright (c) Microsoft Corporation.
# All rights reserved.


$template = Arm {
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

Publish-ArmTemplate -Template $template -OutFile ./storageExampleTemplate.json -StorageAccountType 'Standard_GRS' -PassThru