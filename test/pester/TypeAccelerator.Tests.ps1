# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# All rights reserved.

Describe "PSArm type accelerators" {
    It "Defines an ArmVariable type accelerator" {
        [ArmVariable].FullName | Should -BeExactly 'PSArm.Templates.ArmVariable'
    }

    It "Defines a generic ArmParameter type accelerator" {
        [ArmParameter].FullName | Should -BeExactly 'PSArm.Templates.ArmParameter`1'
        [ArmParameter[string]].GenericTypeArguments[0].FullName | Should -BeExactly 'System.String'
    }
}