# Copyright (c) Microsoft Corporation.
# All rights reserved.

Describe "PSArm type accelerators" {
    It "Defines an ArmVariable type accelerator" {
        [ArmVariable].FullName | Should -BeExactly 'PSArm.Expression.ArmVariable'
    }

    It "Defines a generic ArmParameter type accelerator" {
        [ArmParameter].FullName | Should -BeExactly 'PSArm.Expression.ArmParameter`1'
        [ArmParameter[string]].GenericTypeArguments[0].FullName | Should -BeExactly 'System.String'
    }
}