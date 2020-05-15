# PSArm

PSArm is a PowerShell module that provides a domain-specific language (DSL)
for Azure Resource Manager (ARM) templates,
allowing you to use PowerShell constructs
to build ARM templates.

This is currently an experimental project,
meaning it may make breaking changes as development continues.
If a functionality is missing or seems to not work correctly,
please open an issue!

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
  to enable pithy ARM template specification,
  especially with concepts like piping and `foreach`/`ForEach-Object`
- Take advantage of PowerShell's pithy, whitespace-aware syntax to offer a clean syntax for ARM
  that displays only the needed information, with as little boilerplate as possible

## Examples

A simple example for creating a network interface:

**TODO: Reference original JSON**

```powershell
# Specify the ARM template purely within PowerShell
$template = Arm {
    param(
        [ValidateSet('WestUS2', 'CentralUS')]
        [ArmParameter[string]]$rgLocation,

        [ArmParameter]$namePrefix = 'my',

        [ArmVariable]$vnetNamespace = 'myVnet/'
    )

    Resource (Concat $vnetNamespace $namePrefix '-subnet') -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/virtualNetworks/subnets {
        Properties {
            # Each resource defines its properties as commands within its own body
            AddressPrefix -Prefix 10.0.0.0/24
        }
    }

    # Piping, looping and commands like ForEach-Object all work
    '-pip1','-pip2' | ForEach-Object {
        Resource (Concat $namePrefix $_) -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/publicIpAddresses {
            Properties {
                PublicIPAllocationMethod -Method Dynamic
            }
        }
    }

    Resource (Concat $namePrefix '-nic') -Location $rgLocation -ApiVersion 2019-11-01 -Type Microsoft.Network/networkInterfaces {
        Properties {
            IpConfiguration -Name 'myConfig' {
                # Sub-properties also appear as commands within property contexts
                PrivateIPAllocationMethod -Method Dynamic

                # ARM expressions can be expressed in PowerShell
                # The subnet ID here is: [resourceId('Microsoft.Network/virtualNetworks/subnets', concat(variables('vnetNamespace'), variables('namePrefix'), '-subnet'))]
                Subnet -Id (ResourceId 'Microsoft.Network/virtualNetworks/subnets' (Concat $vnetNamespace $namePrefix '-subnet'))
            }
        }
    }

    Output 'nicResourceId' -Type 'string' -Value (ResourceId 'Microsoft.Network/networkInterfaces' (Concat $namePrefix '-nic'))
}

# Publish the template using the default value for $namePrefix
# If a bad value is given for $rgLocation here, a helpful error will be thrown
Publish-ArmTemplate -Template $template -OutFile ./networkTemplate.json -Parameters @{ rgLocation = 'WestUS2' }

# Publish-ArmTemplate also supports ARM parameters as dynamic parameters on the template you pass to it
Publish-ArmTemplate -Template $template -OutFile ./networkTemplate.json -rgLocation WestUS2

# Deploy the template to a resource group using the Az.Resources command
New-AzResourceGroupDeployment -ResourceGroupName MyResourceGroup -TemplateFile ./networkTemplate.json
```

A full list of examples can be found under `examples/` in the repository root.

## Concepts

PSArm is a hierarchical, context-sensitive domain-specific language embedded within PowerShell.
That means, you can write an ordinary PowerShell script and embed one or more PSArm blocks inside of it;
you don't need a special separate file or a different file extension.

The DSL is signalled by the `Arm` keyword (also available as `New-ArmTemplate`) followed by a scriptblock.
From this point within the scriptblock, the DSL offers contextual functionality.

### Variables and parameters

The `param` block can be used for specifying ARM parameters and variables.
Variables are simply given a name (their PowerShell variable name) and value (as PowerShell default value).
Parameters only require a name but can be given a type (as a generic type argument), a default value,
and can also apply constraint attributes like `ValidateSet` to the ARM template.

### High-level ARM keywords

High-level ARM template properties like `resources` and `outputs` are available in PSArm through keywords
such like `Resource` and `Output` respectively.
These keywords instantiate one resource or output instance at a time and can be in any order.

### Resource-level keywords

Most of the complexity in ARM templates lies within the resources themselves.
For any resource there may be a series of parameters, properties and nested resources.
In PSArm, simple parameters are parameters on the `Resource` keyword,
while parameters with object structure are keywords under the `Resource` keyword.

Resource properties are specified under the `Properties` keyword
and are made available as functions within that particular context.
For example, for a resource of type `Microsoft.Network/networkInterfaces`,
the `Properties` block offers an `IPConfiguration` keyword that specifically configures the `ipConfiguration` property.
Whereas in `Microsoft.Network/publicIpAddresses`, `IPConfiguration` is meaningless,
but `PublicIPAllocationMethod` allows you to configure the IP allocation method.

### ARM functions and expressions

The ARM template language has a template expression language embedded in JSON string values that it evaluates at deployment time,
allowing parameterization and deduplication of templates.

In PSArm, ordinary PowerShell variables can be used, obviating the need for many ARM variable expressions,
but there are still a number of cases where an ARM expression may be required:

- A builtin ARM function that must be evaluated at deployment time, like `resourceGroup()` or `utcNow()`
- A variable is needed to be evaluated only once, such as using `uniqueString()` to provide a hash value reused everywhere in a template
- The template is to be constructed with PowerShell, but parameterized for later deployment without PowerShell or without the PSArm module

In these cases, it's still desirable to be able to write ARM expressions into a template,
but writing these as strings in PSArm would be a cumbersome experience.
Instead PSArm provides ARM builtin expression functions as its own keywords.
These functions allow you to use PowerShell syntax to express function application and member access:

- `(ResourceGroup)` becomes `[resourceGroup()]`
- `Concat "a" (ResourceId 'Microsoft.Storage/storageAccounts')` becomes `[concat('a', resourceId('Microsoft.Storage/storageAccounts'))]`
- `(ResourceGroup).Location` becomes `[resourceGroup().location]`

## Building

PSArm comes with a build script that tries to keep things simple and minimal. To build it, run:

```powershell
./build.ps1
```

This will output the built module to `out/PSArm`, which can be imported with `Import-Module ./out/PSArm`.
Keep in mind that this is a binary module, so you'll likely want to start a new process before importing it
so that you can easily rebuild and reimport as you make changes.

## Implementation details

- High-level DSL keywords are implemented as cmdlets that implement logic by hand
- Lower-level keywords within resources are described by JSON schemas and are converted to script on demand:
  - When completions are asked for or an ARM template command is processed with resources, the required resource JSON schemas are read in
  - A script writer visits these schemas and converts them to a series of simple PowerShell functions,
    with inner keywords represented as inner functions
  - These inner functions mainly declare their parameters and delegate back to cmdlets that turn these parameters into a named JSON element
  - When each resource is invoked, the functions are converted to scriptblocks and the definitions dot-sourced in the user scope
    (so that dynamic scope works properly for resolving inner functions in user-defined scriptblocks)
  - The fact of defining DSL functions in user scope is considered an implementation detail/bug and hopefully can be removed in later development
- Each keyword invokes its scriptblock body in user scope and collects the output,
  sifting through it based on object type and reconstructing an object hierarchy from it, like a complex builder pattern
- These objects agglomerate together as they come up through the keywords,
  with the `Arm` keyword capturing them all under one big object
- The `Arm` keyword also looks at the AST of the scriptblock its given to build a list of ARM parameters and variables,
  and remember any constraints applied to them like types or enums.
  It also rewrites the scriptblock to remove any of the constraints on parameter values so that it can run the scriptblock
- ARM expression functions like `Concat` and `ResourceId` are also written as cmdlets
  and directly instantiate `ArmFunctionCall` instances to render properly in templates.
  - These objects also extend `DynamicObject` so that member access and indexing turns such an expression into
    the corresponding ARM expressions
- The DSL also comes with completion logic:
  - There's an argument completer for the `Resource` keyword to complete the `-Type` parameter to list resources for which schemas are available
  - Most completions come from a from-scratch completer written to understand the hierarchical keyword context
    to provide keywords within each schema that work for particular contexts, in particular keywords that work with each resource.
