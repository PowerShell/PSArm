---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmResource

## SYNOPSIS
Declare an ARM resource in PSArm.

## SYNTAX

```
New-PSArmResource [-Name] <IArmString> -ApiVersion <IArmString> -Provider <IArmString> -Type <IArmString>
 [-Body] <ScriptBlock> [<CommonParameters>]
```

## DESCRIPTION
The `Resource` keyword declares ARM resources in PSArm,
to combine into templates for deployment.
It is intended to be used in the body of the `Arm` keyword.

## EXAMPLES

### Example 1
```powershell
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
```

Declares a storage account resource in the PSArm DSL.

## PARAMETERS

### -ApiVersion
The API version of the declared ARM resource.

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Body
The definition of the ARM resource, given as a scriptblock in PSArm.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The name of the resource.

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
The type of the resource being defined.

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Provider
{{ Fill Provider Description }}

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: True
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

### PSArm.Templates.Primitives.ArmEntry
## NOTES

## RELATED LINKS
