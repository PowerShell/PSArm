---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# ConvertTo-PSArm

## SYNOPSIS
Convert a PSArm template object to a PSArm script that can generate it.

## SYNTAX

```
ConvertTo-PSArm -InputTemplate <ArmTemplate[]> [-OutFile <String>] [-PassThru] [-Force] [<CommonParameters>]
```

## DESCRIPTION
Converts a PSArm template object into a PSArm PowerShell script
that, when executed, will generate that template.
This is most useful for converting from ARM JSON templates
after `ConvertFrom-PSArmJsonTemplate`.

## EXAMPLES

### Example 1
```powershell
PS C:\> ConvertFrom-PSArmJsonTemplate -Path ./examples/windows-vm/template.json | ConvertTo-PSArm -OutFile ./windows-vm.psarm.ps1
```

Converts a JSON ARM template file to a PSArm script.

## PARAMETERS

### -Force
If a file exists in the `OutFile` location, overwrite it.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputTemplate
The PSArm template object to convert to PSArm script.

```yaml
Type: ArmTemplate[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -OutFile
The file system location to write the converted template to.
If a file already exists there, this will fail unless `-Force` is used.

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

### -PassThru
If set, the PSArm script will also be written out as a string.

```yaml
Type: SwitchParameter
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

### PSArm.Templates.ArmTemplate[]

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
