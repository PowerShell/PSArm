# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Basic PSArm template elements" {

    $testCases = @(
        @{
            CaseName = 'empty template'
            Template = Arm { }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0"
            }'
        }
        @{
            CaseName = 'single resource'
            Template = Arm {
                Resource "Example" -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type virtualNetworks/subnets {
                    Properties {
                        AddressPrefix 10.0.0.0/24
                    }
                }
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "resources": [
                    {
                        "apiVersion": "2019-11-01",
                        "type": "Microsoft.Network/virtualNetworks/subnets",
                        "name": "Example",
                        "properties": {
                            "addressPrefix": "10.0.0.0/24"
                        }
                    }
                ]
            }'
        }
        @{
            CaseName = 'two resources'
            Template = Arm {
                '-pip1','-pip2' | ForEach-Object {
                    Resource -Location WestUS2 "ex$_" -ApiVersion 2019-11-01 -Namespace Microsoft.Network -Type publicIPAddresses {
                        Properties {
                            PublicIPAllocationMethod Dynamic
                        }
                    }
                }
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "resources": [
                    {
                        "apiVersion": "2019-11-01",
                        "type": "Microsoft.Network/publicIPAddresses",
                        "name": "ex-pip1",
                        "location": "WestUS2",
                        "properties": {
                            "publicIPAllocationMethod": "dynamic"
                        }
                    },
                    {
                        "apiVersion": "2019-11-01",
                        "type": "Microsoft.Network/publicIPAddresses",
                        "name": "ex-pip2",
                        "location": "WestUS2",
                        "properties": {
                            "publicIPAllocationMethod": "dynamic"
                        }
                    }
                ]
            }'
        }
        @{
            CaseName = 'output'
            Template = Arm {
                Output 'myOutput' -Type 'string' -Value 'example-output'
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "outputs": {
                    "myOutput": {
                        "type": "string",
                        "value": "example-output"
                    }
                }
            }'
        }
        @{
            CaseName = 'output'
            Template = Arm {
                Output 'myOutput' -Type 'string' -Value 'example-output'
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "outputs": {
                    "myOutput": {
                        "type": "string",
                        "value": "example-output"
                    }
                }
            }'
        }
        @{
            CaseName = 'multiple output'
            Template = Arm {
                Output 'myOutput' -Type string -Value 'example-output'
                Output 'secondOutput' -Type int -Value 10
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "outputs": {
                    "myOutput": {
                        "type": "string",
                        "value": "example-output"
                    },
                    "secondOutput": {
                        "type": "int",
                        "value": 10
                    }
                }
            }'
        }
        @{
            CaseName = 'single string parameter'
            Template = Arm {
                param(
                    [ValidateSet('A', 'B')]
                    [ArmParameter[string]]
                    $MyParameter = 'A'
                )
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "parameters": {
                    "MyParameter": {
                        "type": "string",
                        "defaultValue": "A",
                        "allowedValues": [
                            "A",
                            "B"
                        ]
                    }
                }
            }'
        }
        @{
            CaseName = 'multiple parameters'
            Template = Arm {
                param(
                    [ValidateSet('A', 'B')]
                    [ArmParameter[string]]
                    $MyParameter = 'A',

                    [ArmParameter[int]]
                    $Count = 3
                )
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "parameters": {
                    "MyParameter": {
                        "type": "string",
                        "defaultValue": "A",
                        "allowedValues": [
                            "A",
                            "B"
                        ]
                    },
                    "Count": {
                        "type": "int",
                        "defaultValue": 3
                    }
                }
            }'
        }
        @{
            CaseName = 'single variable'
            Template = Arm {
                param(
                    [ArmVariable]
                    $MyVariable = 'value'
                )
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "variables": {
                    "MyVariable": "value"
                }
            }'
        }
        @{
            CaseName = 'multiple variables'
            Template = Arm {
                param(
                    [ArmVariable]
                    $MyVariable = 'value',

                    [ArmVariable]
                    $SecondVariable = 73
                )
            }
            Expected = '
            {
                "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "contentVersion": "1.0.0.0",
                "variables": {
                    "MyVariable": "value",
                    "SecondVariable": 73
                }
            }'
        }
    )

    It "Correctly generates a template for case: <CaseName>" -TestCases $testCases {
        param(
            [string]$CaseName,
            [PSArm.Templates.Primitives.ArmElement]$Template,
            [string]$Expected
        )

        Assert-EquivalentToTemplate -GeneratedObject $Template -TemplateDefinition $Expected
    }
}