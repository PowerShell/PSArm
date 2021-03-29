# PSArm

PSArm is an experimental PowerShell module that provides a
domain-specific language (DSL) embedded in PowerShell
for Azure Resource Manager (ARM) templates,
allowing you to use PowerShell
to build ARM templates.

We're using this project both
to better understand how PowerShell could boost ARM authoring,
but also as test case for improving DSL support in PowerShell more generally.
We hope that work here can help us build an inventory of PowerShell DSL patterns,
and from those determine what could be implemented at PowerShell or tooling layers
to make DSL creation and maintenance easier and more "featureful".

Because this is currently an experimental project,
it is not, at present, planned for official support or maintenance,
and may make breaking changes as development continues.
If a functionality is missing or seems to not work correctly,
please open an issue!

#### Note

This project is different from [Project Bicep](https://github.com/Azure/Bicep),
which is a standalone DSL for building ARM templates.
PSArm is a PowerShell-embedded DSL exposed through a PowerShell module,
however it uses Bicep's underlying schema backend to power its ARM resource generation and completions.

## Goals

The primary goal of PSArm is to use the strengths of PowerShell
to enhance the ARM authoring experience.
In particular, high level goals are:

- Integrate with PowerShell's completion infrastructure
  to provide discoverability for ARM wherever possible
- Use PowerShell's reflective object awareness to intelligently
  create ARM structures based on context
- Leverage PowerShell's dynamic scope to make keywords context-dependent
- Reuse PowerShell's expressive pipeline-emitting semantics
  to enable powerful generative ARM template specification,
  especially with concepts like piping and `foreach`/`ForEach-Object`
- Take advantage of PowerShell's pithy, whitespace-aware syntax to offer a clean syntax for ARM
  that displays only the needed information, with as little boilerplate as possible

## Examples

Full, tested examples are available in the [examples directory](examples).

A simple example for creating a network interface, which can also be found [here](examples/network-interface):

```powershell
# network-interface.psarm.ps1

# Specify the ARM template purely within PowerShell
Arm {
    param(
        # ValidateSet is turned into "allowedValues"
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]
        $rgLocation,

        [ArmParameter[string]]
        $namePrefix = 'my',

        [ArmVariable]
        $vnetNamespace = 'myVnet/'
    )

    # Use existing PowerShell concepts to make ARM easier
    $PSDefaultParameterValues['Resource:Location'] = $rgLocation

    # Resources types, rather than being <Namespace>/<Type> have this broken into -Namespace <Namespace> -Type <Type>
    # Completions are available for Namespace and ApiVersion, and once these are specified, also for Type
    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Namespace Microsoft.Network -ApiVersion 2019-11-01 -Type virtualNetworks/subnets {
        Properties {
            # Each resource defines its properties as commands within its own body
            AddressPrefix 10.0.0.0/24
        }
    }

    # Piping, looping and commands like ForEach-Object all work
    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type publicIpAddresses {
            Properties {
                PublicIPAllocationMethod Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type networkInterfaces {
        Properties {
            # IpConfigurations is an array property, but PSArm knows this
            # All occurences of array properties will be collected into an array when the template is published
            IpConfigurations {
                Name 'myConfig'
                properties {
                    PrivateIPAllocationMethod Dynamic 

                    # ARM expressions can be expressed in PowerShell
                    # The subnet ID here is: [resourceId('Microsoft.Network/virtualNetworks/subnets', concat(variables('vnetNamespace'), variables('namePrefix'), '-subnet'))]
                    Subnet {
                        id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
                    }
                }
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}
```

Run this with the following command:

```powershell
# Run the template and publish it to a JSON file. By default this is ./template.json
Publish-PSArmTemplate -Path ./network-interface.psarm.ps1 -Parameters @{ rgLocation = 'WestUS2' }

# Deploy the template to a resource group using the Az.Resources command
New-AzResourceGroupDeployment -ResourceGroupName MyResourceGroup -TemplateFile ./template.json
```

This will create the following template:

```jsonc
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "metadata": {
    // PSArm, like Bicep, inserts this metadata so it's known how many deployments
    // are done through PSArm (i.e. how useful is PSArm to Azure customers?).
    // It can be stripped out harmlessly if it's unwanted.
    "_generator": {
      "name": "psarm",
      "version": "0.1.0.0",
      "psarm-psversion": "7.2.0-preview.4",
      "templateHash": "6758140738045718234"
    }
  },
  "resources": [
    {
      "name": "network-interface",
      "type": "Microsoft.Resources/deployments",
      "apiVersion": "2019-10-01",
      "properties": {
        "mode": "Incremental",
        "expressionEvaluationOptions": {
          "scope": "inner"
        },
        "template": {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "variables": {
            "vnetNamespace": "myVnet/"
          },
          "resources": [
            {
              "name": "[concat(variables('vnetNamespace'), 'my', '-subnet')]",
              "apiVersion": "2019-11-01",
              "type": "Microsoft.Network/virtualNetworks/subnets",
              "properties": {
                "addressPrefix": "10.0.0.0/24"
              }
            },
            {
              "name": "[concat('my', '-pip1')]",
              "apiVersion": "2019-11-01",
              "type": "Microsoft.Network/publicIpAddresses",
              "location": "WestUS2",
              "properties": {
                "publicIPAllocationMethod": "Dynamic"
              }
            },
            {
              "name": "[concat('my', '-pip2')]",
              "apiVersion": "2019-11-01",
              "type": "Microsoft.Network/publicIpAddresses",
              "location": "WestUS2",
              "properties": {
                "publicIPAllocationMethod": "Dynamic"
              }
            },
            {
              "name": "[concat('my', '-nic')]",
              "apiVersion": "2019-11-01",
              "type": "Microsoft.Network/networkInterfaces",
              "location": "WestUS2",
              "properties": {
                "ipConfigurations": [
                  {
                    "name": "myConfig",
                    "properties": {
                      "privateIPAllocationMethod": "Dynamic",
                      "subnet": {
                        "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', concat(variables('vnetNamespace'), 'my', '-subnet'))]"
                      }
                    }
                  }
                ]
              }
            }
          ],
          "outputs": {
            "nicResourceId": {
              "type": "string",
              "value": "[resourceId('Microsoft.Network/networkInterfaces', concat('my', '-nic'))]"
            }
          }
        }
      }
    }
  ]
}
```

For more in-depth examples, see the [examples](examples) directory.

## `Publish-PSArmTemplate`

The `Publish-PSArmTemplate` command is the key cmdlet for executing PSArm templates.
It performs the following tasks:

- Collects PSArm template files from the `-Path` parameter, supporting either file paths or directory paths
  (which it will recursively search for files ending with `.psarm.ps1`).
- Passes through any parameters specified with the `-Parameters` parameter to scripts executed (both the psarm.ps1 scripts and the Arm templates within)
- Executes the PSArm template scripts in discovery order and collects them into a nested ARM template
- Uses either `Get-AzAccessToken` or `az account get-access-token` to get an Azure access token
  and uses the [Azure template hash API](https://management.azure.com/providers/Microsoft.Resources/calculateTemplateHash?api-version=2020-06-01)
  to add a hash to the generated JSON template's metadata.
  This can be disabled with `-NoHashTemplate` or a custom Azure token provided with `-AzureToken`.
- Writes the final nested JSON template file out to the `-OutFile` path or `./template.json` by default.
  - If the file already exists this will fail unless `-Force` is used.
  - `-PassThru` can be specified to also get the full template object from the command
  - `-NoWriteFile` can be specified to prevent the file being written
- `-Verbose` will give a good account of what `Publish-PSArmTemplate` is doing

`Publish-PSArmTemplate` will write a JSON file to disk only,
and is not intended to deploy the resulting ARM template.
Deployment functionality is already provided and maintained
in Azure PowerShell commands and the `az` CLI.

## Conversion cmdlets

Having to learn and write a new DSL takes time and effort,
especially a with complex hierarchical one like PSArm.
So PSArm comes with two commands to help:

- `ConvertFrom-ArmTemplate`, which takes in ARM JSON and converts to a PSArm in-memory object
- `ConvertTo-PSArm`, which takes a PSArm object and writes it out as PSArm PowerShell script

A typical invocation looks like this:

```powershell
ConvertFrom-ArmTemplate -Uri 'https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/101-vm-simple-windows/azuredeploy.json' |
    ConvertTo-PSArm -OutFile ./windows-vm.psarm.ps1 -Force
```

These conversion cmdlets aren't perfect, and of course they can't replicate things like loops within PowerShell,
but they should help to make using PSArm much easier.
If you hit a bug or an issue with the conversion cmdlets, definitely open an issue.

## Completions

PSArm offers contextual completions on keywords and parameters:

![Completion example GIF](./completions.gif)

## Concepts

PSArm is a hierarchical, context-sensitive domain-specific language embedded within PowerShell.
The DSL tries to be as unmagical as possible;
pretty much all the functionality is ordinary functions, with next to no AST rewriting
(some has to be done with the `param` block, but that's it).
That means you can write an ordinary PowerShell script and embed one or more PSArm blocks inside of it,
or you can call out from your ARM block into another script.
You can use the PSArm keywords in any script and they will work.

ARM template definitions are written as scriptblock bodies to the `Arm` keyword,
within which PSArm offers its DSL functionality, complete with contextual completions.

The `Arm` keyword then constructs an object representation of an ARM template,
which is output when the script is executed.
So when `Publish-PSArmTemplate` is run on these scripts,
it simply executes them and collects all the ARM objects they emit.

`Publish-PSArmTemplate` only looks for scripts that end with the `.psarm.ps1` extension
so that it can support being given directory paths.
This means you can mix ordinary scripts and PSArm scripts in the same directory
without `Publish-PSArmTemplate` accidentally executing those ordinary scripts.

`Publish-PSArmTemplate` aggregates all the templates it collects into a nested template
and writes that out as an ARM JSON file, ready for deployment.

### Variables and parameters

PSArm scripts are ordinary PowerShell scripts,
so when they are run (by `Publish-PSArmTemplate` for example) they are simply invoked like any other script.
That means you can freely add a `param` block to your PSArm scripts to parameterize them,
and then provide those parameters to `Publish-PSArmTemplate` through its `-Parameters` parameter.

Note that the `-Parameters` parameter accepts a hashtable, but will also accept a PSObject,
meaning you can do the following:

```powershell
$parameters = Get-Content ./parameters.json | ConvertFrom-Json
Publish-PSArmTemplate ... -Parameters $parameters
```

Using ordinary PowerShell variables to create ARM scripts will work in many scenarios,
but sometimes you do need an actual ARM variable or parameter, for example:

- For secure parameters, like of type `securestring`, that you don't want to bake directly into an ARM template,
- With functions that must be evaluated at deployment time, such as `[resourceGroup()]` or `[deployment()]`,
- Or in cases where an expression needs to be evaluated only once, like `[uniqueString()]` or `[utcNow()]`

In these cases, you can create ARM parameters and variables by adding a `param` block to the body of the `Arm` keyword itself.
ARM parameters and variables are specified by type;
`[ArmParameter[<type>]]` and `[ArmVariable]` respectively.
Those parameters and variables then use their PowerShell variable name in their template.
They also support PowerShell features like default values and the `ValidateSet` attribute.

When `Publish-PSArmTemplate` instantiates an ARM template, it will try to use any of the values from the `-Parameters` parameter
to also instantiate parameters to the `Arm` block (in addition to the `.psarm.ps1` script).
Any parameters it doesn't have a value for will be left in the template and published as part of it,
to be provided at deployment.

As a quick example, the following PSArm script:

```powershell
# storageAccount.psarm.ps1

param(
  [Parameter(Mandatory)]
  [string]
  $StorageAccountName,

  [Parameter()]
  [ValidateSet('WestUS2', 'CentralUS')]
  [string]
  $Location = 'WestUS2'
)

Arm {
  param(
    [ValidateSet('Hot', 'Cool', 'Archive')]
    [ArmParameter[string]]
    $accessTier = 'Hot',

    [ArmParameter[int]]
    $httpsOnly,

    [ArmVariable]
    $deploymentTime = (utcNow)
  )

  Resource $StorageAccountName -Namespace Microsoft.Storage -Type storageAccounts -ApiVersion 2019-06-01 -Kind StorageV2 -Location $Location {
    ArmSku Standard_LRS
    properties {
      accessTier $accessTier
      supportsHTTPSTrafficOnly $httpsOnly
      allowBlobPublicAccess 1
      allowSharedKeyAccess 1
    }
  }

  Output 'deploymentTime' -Type string -Value $deploymentTime
}
```

Published like this:

```powershell
Publish-PSArmTemplate -TemplatePath ./storageAccount.psarm.ps1 -Parameters @{
  StorageAccountName = 'MyStorageAccount'
  allowPublicAccess = 1
}
```

Will generate an ARM template like this (the full output will be embedded in a nested template):

```jsonc
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "httpsOnly": {
      "type": "int"
    }
  },
  "variables": {
    "deploymentTime": "[utcNow()]"
  },
  "resources": [
    {
      "name": "MyStorageAccount",
      "apiVersion": "2019-06-01",
      "type": "Microsoft.Storage/storageAccounts",
      "kind": "StorageV2",
      "location": "WestUS2",
      "sku": {
        "name": "Standard_LRS"
      },
      "properties": {
        "accessTier": "Hot",
        "supportsHttpsTrafficOnly": "[parameters('httpsOnly')]",
        "allowBlobPublicAccess": 1,
        "allowSharedKeyAccess": 1
      }
    }
  ],
  "outputs": {
    "deploymentTime": {
      "type": "string",
      "value": "[variables('deploymentTime')]"
    }
  }
}
```

You can see:

- `$StorageAccountName` and `$Location` work like ordinary PowerShell variables and were simply subtituted by value.
  If `$StorageAccountName` weren't provided you would see the standard mandatory parameter prompt,
  and `$Location` defaults to the value given.
- `$accessTier` has a known value at publish time, so it's directly substitued into the template
- `$httpsOnly` has no given value, so becomes a parameter of the published template
- `$deploymentTime` is a variable, so always has its value embedded as a variable in the template
  (it's assumed that if you've created an ARM variable, you deliberately want it in the template)

  In general the advice is:

  - Keep things as ordinary script parameters whenever you can
  - Prefer ARM variables to ARM parameters for things that must be evaluated at deployment time
  - Try not to make ARM variables and ARM parameters depend on each other
  - Use ARM parameters for secure inputs

### High-level ARM keywords

High-level ARM template properties like `resources` and `outputs` are available in PSArm through keywords
such like `Resource` and `Output` respectively.
These keywords instantiate one resource or output instance at a time and can be in any order.

### Resource-level keywords

Most of the complexity in ARM templates lies within the resources themselves.
For any resource there may be a series of parameters, properties and nested resources.
In PSArm, simple parameters are parameters on the `Resource` keyword,
while parameters with object structure are keywords under the `Resource` keyword.

Underneath each resource, properties on that resource are available as PowerShell functions,
which either take a value or a scriptblock body depending on the type accepted by the keyword.
For example, for a resource of type `Microsoft.Network/networkInterfaces`,
the `properties` keyword will be available, and within that an `ipConfigurations` keyword
that specifically configures the `ipConfigurations` property.
Whereas in `Microsoft.Network/publicIpAddresses`, `ipConfigurations` is meaningless,
but `publicIPAllocationMethod` allows you to configure the IP allocation method.

### ARM template functions and expressions

The ARM template language has a template expression language embedded in JSON string values that it evaluates at deployment time,
allowing parameterization and deduplication of templates.

In PSArm, ordinary PowerShell variables can be used, obviating the need for many ARM variable expressions,
but there are still a number of cases where an ARM expression may be required:

- A builtin ARM template function that must be evaluated at deployment time, like `resourceGroup()` or `utcNow()`
- A variable is needed to be evaluated only once, such as using `uniqueString()` to provide a hash value reused everywhere in a template
- The template is to be constructed with PowerShell, but parameterized for later deployment without PowerShell or without the PSArm module

In these cases, it's still desirable to be able to write ARM expressions into a template,
but writing these as strings in PSArm would be a cumbersome experience.
Instead PSArm provides ARM builtin expression functions as PowerShell functions.
These functions allow you to use PowerShell syntax to express function application and member access:

- `(resourceGroup)` becomes `[resourceGroup()]`
- `concat "a" (resourceId 'Microsoft.Storage/storageAccounts')` becomes `[concat('a', resourceId('Microsoft.Storage/storageAccounts'))]`
- `(resourceGroup).location` becomes `[resourceGroup().location]`

## Telemetry metadata

PSArm embeds metadata in generated templates to mark them as being generated by PSArm,
using the same method as Azure Bicep.
When those ARM templates are deployed,
this metadata is picked up for telemetry by the ARM deployment service.
Importantly, no personal or identifiable information is made available by this.
This helps us to count the number of ARM deployments PSArm powers,
so we can understand how useful PSArm is to Azure customers
and make investments in its development.

The metadata in the template looks like this:

```json
{
  "metadata": {
    "_generator": {
      "name": "psarm",
      "version": "0.1.0.0",
      "psarm-psversion": "7.1.3"
      "templateHash": "<hash>"
    }
  }
}
```

You can strip out this metadata from the template without affecting any functionality.

## Building

PSArm comes with a build script that tries to keep things simple and minimal. To build it, run:

```powershell
./build.ps1
```

This will output the built module to `out/PSArm`, which can be imported with `Import-Module ./out/PSArm`.
Keep in mind that this is a binary module, so you'll likely want to start a new process before importing it
so that you can easily rebuild and reimport as you make changes.

To run the tests after your build, run:

```powershell
./build.ps1 -Test
```

PSArm is currently built with [Invoke-Build](https://github.com/nightroman/Invoke-Build),
so more advanced build tasks are available through the `Invoke-Build` command.

## Schemas

Template schema support in PSArm comes from the [bicep-types-az](https://github.com/Azure/bicep-types-az) project,
which also powers Bicep.
While an early prototype of PSArm did create and ship its own schemas,
the `Azure.Bicep.Types.Az` NuGet package is now the best way to deal with ARM schemas in .NET.

## Implementation details

- High-level DSL keywords are implemented as cmdlets that implement logic by hand
- Beyond the high level keywords, all other template building functionality is implemented by wrapping primitive commands:
  - `RawEntry`/`New-PSArmEntry`, which describes a JSON key/value pair
  - `RawCall`/`New-PSArmFunctionCall`, which describes an ARM function call (like `[concat('prefix', 'suffix')]`)
  - All ARM functions and resource keywords are autogenerated functions defined in script that wrap these primitives
- Lower-level keywords within resources are described by Bicep schema types and are converted to script on demand:
  - When completions are asked for or an ARM template command is processed with resources, the required schema objects are loaded
  - A script writer visits these schemas and converts them to a series of simple PowerShell functions,
    with inner keywords represented recursively as inner functions
  - These inner functions mainly declare their parameters and delegate back to cmdlets that turn these parameters into a named JSON element
  - When each resource is invoked, the functions are converted to scriptblocks
    and the resource bodies are invoked using the [`ScriptBlock.InvokeWithContext()` method](https://docs.microsoft.com/dotnet/api/system.management.automation.scriptblock.invokewithcontext),
    allowing the DSL functions to be defined within the body scriptblock without polluting any higher scopes
- Each keyword invokes its scriptblock body in user scope and collects the output,
  sifting through it based on object type and reconstructing an object hierarchy from it, like a complex builder pattern
- These objects agglomerate together as they come up through the keywords,
  with the `Arm` keyword capturing them all under one big object
- The `Arm` keyword also looks at the AST of the scriptblock its given to build a list of ARM parameters and variables,
  and remember any constraints applied to them like types or enums.
  It also rewrites the scriptblock to remove any of the constraints on parameter values so that it can run the scriptblock
- ARM expression functions like `concat` and `resourceId` are also written as functions
  and instantiate `ArmFunctionCall` instances to render properly in templates.
  - These objects also extend `DynamicObject` so that member access and indexing turns such an expression into
    the corresponding ARM expressions
- The DSL also comes with completion logic:
  - There's an argument completer for the `Resource` keyword to complete the `-Type`, `-ApiVersion` and `-Namespace` parameters to list resources for which schemas are available
  - Most completions come from a from-scratch completer written to understand the hierarchical keyword context
    to provide keywords within each schema that work for particular contexts, in particular keywords that work with each resource.

## Contributing

PSArm is open-source and contributions are welcome.
The project is still under heavy development,
so code contributions, especially to the core library,
may not get immediate attention.

Some of the most valuable contributions you can provide are
issues, scenarios and PSArm examples (or desired template examples).

## Legal and Licensing

PSArm is licensed under the [MIT license](LICENSE).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct][conduct-code].
For more information see the [Code of Conduct FAQ][conduct-FAQ] or contact [opencode@microsoft.com][conduct-email] with any additional questions or comments.

[conduct-code]: https://opensource.microsoft.com/codeofconduct/
[conduct-FAQ]: https://opensource.microsoft.com/codeofconduct/faq/
[conduct-email]: mailto:opencode@microsoft.com
