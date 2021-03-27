---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmSku

## SYNOPSIS
Declare the SKU of the given resource

## SYNTAX

```
New-PSArmSku [-Name] <IArmString> [-Tier <IArmString>] [-Size <IArmString>] [-Family <IArmString>]
 [-Capacity <IArmString>] [<CommonParameters>]
```

## DESCRIPTION
The `ArmSku` keyword specifies what SKU the given resource is.
Not all `ArmSku` parameters will apply to every resource

## EXAMPLES

### Example 1
```powershell
ArmSku 'Basic' -Tier 'Regional'
```

Declares the resource to have a SKU with name `Basic` and tier `Regional`

## PARAMETERS

### -Capacity
The SKU capacity required

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Family
The SKU family required

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The SKU name required

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

### -Size
The SKU size required

```yaml
Type: IArmString
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Tier
The SKU tier required

```yaml
Type: IArmString
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

### PSArm.Templates.PRimitives.ArmEntry
## NOTES

## RELATED LINKS
