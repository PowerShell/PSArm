---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# ConvertFrom-ArmTemplate

## SYNOPSIS
Convert from an ARM JSON template to object form.

## SYNTAX

### Path
```
ConvertFrom-ArmTemplate [-Path] <String[]> [<CommonParameters>]
```

### Uri
```
ConvertFrom-ArmTemplate [-Uri] <Uri[]> [<CommonParameters>]
```

### Input
```
ConvertFrom-ArmTemplate [-Input] <String[]> [<CommonParameters>]
```

## DESCRIPTION
Converts an Azure ARM JSON template into a PSArm ARM template object type.
Such an object can then be manipulated using PowerShell
or written out either back to JSON or as a PSArm script using `ConvertTo-PSArmTemplate`.

## EXAMPLES

### Example 1
```powershell
PS C:\> $template = ConvertFrom-ArmTemplate -Uri 'https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/101-vm-simple-linux/azuredeploy.json'
```

Downloads the template at the given URL, parses it and deserializes it into an in-memory PSArm template object.

### Example 2
```powershell
PS C:\> ConvertFrom-ArmTemplate -Uri 'https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/101-vm-simple-linux/azuredeploy.json' | ConvertTo-PSArm -OutFile ./linux-vm.psarm.ps1
```

Downloads the template at the given URL, converts it from ARM JSON to a PSArm template object and then converts that to a PSArm script.

### Example 2
```powershell
PS C:\> (ConvertFrom-ArmTemplate -Uri 'https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/101-vm-simple-linux/azuredeploy.json').ToJsonString()
```

Downloads the template at the given URL, converts it from ARM JSON to a PSArm template object and then serializes it back to a JSON string.

## PARAMETERS

### -Input
A string containing the JSON for an ARM template.

```yaml
Type: String[]
Parameter Sets: Input
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Path
The path to a JSON file containing an ARM template.

```yaml
Type: String[]
Parameter Sets: Path
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Uri
The URI of an ARM JSON file to be converted.

```yaml
Type: Uri[]
Parameter Sets: Uri
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

### System.String[]

## OUTPUTS

### PSArm.Templates.ArmTemplate
## NOTES

## RELATED LINKS
