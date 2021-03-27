---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmOutput

## SYNOPSIS
Declare outputs of the template

## SYNTAX

```
New-PSArmOutput [-Name] <IArmString> -Type <IArmString> -Value <IArmString> [<CommonParameters>]
```

## DESCRIPTION
Declares any outputs of the ARM template. Best used as the `Output` keyword

## EXAMPLES

### Example 1
```powershell
Output 'hostname' -Type 'string' -Value (reference $publicIPAddressName).dnsSettings.fqdn
```

Declares the `hostname` output, which outputs the fully qualified domain name of the public IP address, which is a string.

## PARAMETERS

### -Name
The name of the output

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
The ARM type of the output

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

### -Value
The reference expression for the output

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
