---
external help file: PSArm.dll-Help.xml
Module Name: PSArm
online version:
schema: 2.0.0
---

# Publish-PSArmTemplate

## SYNOPSIS
Execute PSArm templates and write them as an ARM JSON template file.

## SYNTAX

```
Publish-PSArmTemplate -TemplatePath <String[]> [-AzureToken <String>] [-Parameters <Hashtable>]
 [-OutFile <String>] [-PassThru] [-Force] [-NoWriteFile] [-NoHashTemplate] [<CommonParameters>]
```

## DESCRIPTION
`Publish-PSArmTemplate` is the primary cmdlet in the PSArm module,
used to execute PSArm template files.
It works similarly to `Invoke-Pester` or `Invoke-Build`,
where files with a given prefix are found and executed.

`Publish-PSArmTemplate` will execute PSArm scripts given to it,
aggregate them into a nested ARM template,
add a hash value to that template,
and then write the template out to a JSON file.

Template file deployment must be done with a separate command,
such as `New-AzResourceGroupDeployment`.

## EXAMPLES

### Example 1
```powershell
PS C:\> Publish-PSArmTemplate -TemplatePath ./examples/linux-vm -Parameters @{
    AdminUsername = 'admin'
    AdminPasswordOrKey = 'verystrongpassword'
    AuthenticationType = 'password'
}
```

Execute the PSArm templates (files with the `.psarm.ps1` extension) in the `./examples/linux-vm` directory
with the supplied parameters and write the ARM JSON template file out to the default location of `./template.json`.
If the file already exists, this will fail.

### Example 1
```powershell
PS C:\> $parameters = Get-Content -Raw -Path ./examples/windows-vm/parameters.json | ConvertFrom-Json
PS C:\> Publish-PSArmTemplate -TemplatePath ./examples/windows-vm/windows-vm.psarm.ps1 -Parameters $parameters -OutFile windows-vm.json -Force
```

Execute the PSArm template at `./examples/windows-vm/windows-vm.psarm.ps1`
with the supplied parameters (`-Parameters` supports PSObjects)
and write the ARM JSON template file out to `./windows-vm.json`.
If the file already exists, this will overwrite it.

## PARAMETERS

### -AzureToken
An Azure token used to hash the generated template.
By default, `Publish-PSArmTemplate` will try to use `Get-AzAccessToken` and `az accounts get-access-token`,
but supplying a value for this parameter will override that.
If `Publish-PSArmTemplate` is unable to hash the template
(because it can't get a token or because the token is invalid)
and `-NoHashTemplate` isn't specified,
the command will fail.

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

### -Force
Overwrite the output template file if it already exists.

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

### -NoHashTemplate
Skip the template hashing step.
This means the template will not be hashed by sending it to the Azure template hashing API,
so no token is needed and no hash is added to the template.
This can be used to make template publication faster when experimenting with changes.

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

### -NoWriteFile
Do not write a template file.

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

### -OutFile
The path to write the template file to.
If not specified, the template is written to `./template.json`.

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

### -Parameters
A hashtable of parameters to supply to executed PSArm scripts
and the templates they contain.
If a PSArm script has a `param` block, parameters from this value will be used for it.
Additionally, if the body of an `Arm` keyword has a `param` block,
parameters from this value will be used for that as well.
This parameter also supports `PSObject` values,
meaning it can accept the output of cmdlets like `ConvertFrom-Json`.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Emit the final aggregated template object (not the JSON string) as output
in addition to writing the template file.

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

### -TemplatePath
The path to the PSArm template scripts to execute, or directories (or a combination of both).
If directory paths are given, those will be recursively searched for files ending with the `.psarm.ps1` extension.
All found PSArm template scripts will be executed in the order they're found
and aggregated into a single nested ARM template for publication.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Path

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
