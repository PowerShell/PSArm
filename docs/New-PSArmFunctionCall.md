---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmFunctionCall

## SYNOPSIS
Low-level way to declare a function call in ARM

## SYNTAX

```
New-PSArmFunctionCall [-Name] <IArmString> [-Arguments <ArmExpression[]>] [<CommonParameters>]
```

## DESCRIPTION
`New-PSArmFunctionCall`, better used as the `RawCall` keyword,
specifies an ARM function call with the given name and arguments.

## EXAMPLES

### Example 1
```powershell
RawCall concat prefix suffic
```

Specifies the parameter call `[concat('prefix', 'suffix')]`.

## PARAMETERS

### -Arguments
The arguments to the function call.

```yaml
Type: ArmExpression[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The name of the function being called.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSArm.Templates.Primitives.ArmExpression[]

## OUTPUTS

### PSArm.Templates.Operations.ArmFunctionCallExpression
## NOTES

## RELATED LINKS
