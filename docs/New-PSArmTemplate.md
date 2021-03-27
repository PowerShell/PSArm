---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmTemplate

## SYNOPSIS
Define an Azure ARM template in script.

## SYNTAX

```
New-PSArmTemplate [-Name <String>] [-Body] <ScriptBlock> [<CommonParameters>]
```

## DESCRIPTION
Defines an Azure ARM template in the body scriptblock.
This cmdlet is intended to be used in the form of the `Arm` keyword for more fluent reading.

## EXAMPLES

### Example 1
```powershell
Arm {
    Resource $storageAccountName -Provider 'Microsoft.Storage' -Type 'storageAccounts' -apiVersion '2019-06-01' -kind 'StorageV2' -Location 'WestUS2' {
        ArmSku 'Standard_LRS'
        Properties {
            accessTier 'Hot'
            minimumTLSVersion 'TLS1_2'
            supportsHTTPSTrafficOnly 1
            allowBlobPublicAccess 1
            allowSharedKeyAccess 1
        }
    }
}
```

Declares a very simple ARM template that will deploy a new storage account.

## PARAMETERS

### -Body
The ARM template declaration in the PSArm DSL.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The name of the ARM template.
If not provided, the name of the declaring file will be used.
If multiple PSArm templates are declared in the same file,
numbers will be added.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### PSArm.Templates.ArmTemplate
## NOTES

## RELATED LINKS
