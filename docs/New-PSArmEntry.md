---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# New-PSArmEntry

## SYNOPSIS
Declare a low-level ARM key/value pair.

## SYNTAX

### Value (Default)
```
New-PSArmEntry [-Key] <IArmString> [-Value] <ArmElement> [-Array] [<CommonParameters>]
```

### Body
```
New-PSArmEntry [-Key] <IArmString> [-Body] <ScriptBlock> [-Array] [-ArrayBody] [-DiscriminatorKey <String>]
 [-DiscriminatorValue <String>] [<CommonParameters>]
```

## DESCRIPTION
`New-PSArmEntry`, better used as the `RawEntry` keyword,
allows you to specify an arbitrary key/value pair when building ARM JSON.
This keyword is used internally for many PSArm definitions,
but you can use it too to add custom entries to ARM templates.

## EXAMPLES

### Example 1
```powershell
RawEntry 'key' 'value'
```

Creates an entry that in a parent body will be rendered like:

```json
{
    "key": "value"
}
```

### Example 2
```powershell
RawEntry 'object' {
    RawEntry 'one' 'value1'
    RawEntry 'two' 'value2'
    RawEntry 'three' 'value3'
}
```

Creates an entry that in a parent body will be rendered like:

```json
{
    "object": {
        "one": "value1",
        "two": "value2",
        "three": "value3"
    }
}
```

## PARAMETERS

### -Array
Defines this entry as an array entry.
This and any other entries with the same key will be collected into a JSON array.

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

### -ArrayBody
Defines this entry as having an array body.
The value of this entry will be rendered as an array in JSON.
If the value is already an array, it will be a second order array.

```yaml
Type: SwitchParameter
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Body
An object body for this entry.

```yaml
Type: ScriptBlock
Parameter Sets: Body
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DiscriminatorKey
Specifies the name of the discriminator of the ARM element being described

```yaml
Type: String
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DiscriminatorValue
Specifies the value of the discriminator of the ARM element being described

```yaml
Type: String
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
The JSON key of this ARM entry

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

### -Value
The value to set in the ARM JSON

```yaml
Type: ArmElement
Parameter Sets: Value
Aliases:

Required: True
Position: 1
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
