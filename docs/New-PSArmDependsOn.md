---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmDependsOn

## SYNOPSIS
Declares an ARM template dependency

## SYNTAX

### Value (Default)
```
New-PSArmDependsOn [-Value] <IArmString[]> [<CommonParameters>]
```

### Body
```
New-PSArmDependsOn [-Body] <ScriptBlock> [<CommonParameters>]
```

## DESCRIPTION
Declares a dependency from the resource this is used in to the resources it refers to.

## EXAMPLES

### Example 1
```powershell
DependsOn @(
    resourceId 'Microsoft.Storage/storageAccounts' $storageAccountName
    resourceId 'Microsoft.Network/networkInterfaces' $nicName
)
```

Adds an entry in the current resource that it depends on the two other resources referred to.

## PARAMETERS

### -Value
References to the resources depended on.

```yaml
Type: IArmString[]
Parameter Sets: Value
Aliases:

Required: True
Position: 0
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
