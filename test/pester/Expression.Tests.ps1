# Copyright (c) Microsoft Corporation.
# All rights reserved.

Describe "Arm expression tests" {
    $testCases = @(
        @{
            Expression = Concat 'a' 'b'
            Expected = "[concat('a', 'b')]"
        }
        @{
            Expression = (ResourceGroup).Location
            Expected = "[resourceGroup().location]"
        }
        @{
            Expression = Concat 'storage' (UniqueString (ResourceGroup).Id)
            Expected = "[concat('storage', uniqueString(resourceGroup().id))]"
        }
        @{
            Expression = [PSArm.Templates.Primitives.ArmStringLiteral]'hello'
            Expected = "hello"
        }
        @{
            Expression = [PSArm.Templates.Primitives.ArmStringLiteral]'[things]'
            Expected = "[[things]"
        }
        @{
            Expression = [PSArm.Templates.Primitives.ArmStringLiteral]'[something] else'
            Expected = "[something] else"
        }
        @{
            Expression = [PSArm.Templates.Primitives.ArmStringLiteral]'something [else]'
            Expected = "something [else]"
        }
        @{
            Expression = [PSArm.Templates.Primitives.ArmStringLiteral]'"quoted"'
            Expected = '\"quoted\"'
        }
    )

    It "Creates the expected expression: <Expected>" -TestCases $testCases {
        param(
            [PSArm.Templates.Primitives.ArmExpression]
            $Expression,

            [string]
            $Expected
        )

        $Expression.ToExpressionString() | Should -BeExactly $Expected
    }
}