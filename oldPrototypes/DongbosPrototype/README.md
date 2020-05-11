## Idea of the Design

In its simplest structure, a template has the following elements:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "",
  "apiProfile": "",
  "parameters": {  },
  "variables": {  },
  "functions": [  ],
  "resources": [  ],
  "outputs": {  }
}
```

The schema for a resource can be found [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-syntax#resources). Schemas for other elements of a template can also be found on the same page.

For the common keys of a template and its elements (such as the keys of a resource),
simple key/value pairs are treated as parameters,
and complext key/value pairs are represented as functions.
Parameters of a template are declared directly as a `ParamBlock`.

For example,
- `contentVersion`, `apiProfile` are parameters of the `Template` function.
  `Template` takes a script block that declares the complex elements (object/array).
- `parameters` are declared as a `ParamBlock` of the script block.
- `resources` takes an array of resources. Function `Resource` is for declaring one resource.
  The script block for `Template` can call `Resource` for one or more times
  to declare one or more resources.
- `outputs` take an object, each of whose properties is an output.
  Function `Output` is for declaring one output.
  The script block for `Template` can call `Output` for one or more times
  to declare one or more outputs.
- `properties` of a `resource` is an object, each of whose properties is a property.
  Function `Property` is for declaring one property.

The functions described so far are declared in the base `ARMTemplateDSL` module.
This module only contains the function definitions for the common keys.

The type of a resource is a combination of the namespace of the resource provider and the resource type
(such as **Microsoft.Storage/storageAccounts**).
Each type will have a corresponding PowerShell module,
which has to be **auto-generated out of the schema of the resource type**.

Each of such modules will need to implement the command `Get-Schema [-Name] <string>` 
which returns schemas related to this resource, including:
- schema of this resource itself
- schemas of the objects used by this resource

For each of the special objects used by the resource,
a function will be defined for it in the module of that resource.
Those functions will use some of the common functions from `ARMTemplateDSL`,
such as the function `Property`.

With `Get-Schema`, the common functions defined in `ARMTemplateDSL` module can query for the schema
of a specific resource, or a specific property.
The resource type will be specified when using the `Resource` function,
so the module name can be inferred from the resource type,
and then `$moduleName\Get-Schema` can be called to get the schemas of that resource.

`Get-Schema` can help the tab completion as well.

## Example

[`basic.ps1`](./basic.ps1) is a sample of the DSL to generate [`basic.json`](./basic.json).
To try it out, add the [`Modules`](./Modules) folder to your module path, and then run `basic.ps1`.
It will generate the same JSON content as in [`basic.json`](./basic.json),
which is copied from [`arm-templator-transpiler/tests/basic.json`](https://github.com/anthony-c-martin/arm-templator-transpiler/blob/master/tests/basic.json).
