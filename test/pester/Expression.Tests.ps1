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
            Expression = [PSArm.Expression.ArmStringLiteral]'hello'
            Expected = "hello"
        }
        @{
            Expression = [PSArm.Expression.ArmStringLiteral]'[things]'
            Expected = "[[things]"
        }
        @{
            Expression = [PSArm.Expression.ArmStringLiteral]'[something] else'
            Expected = "[something] else"
        }
        @{
            Expression = [PSArm.Expression.ArmStringLiteral]'something [else]'
            Expected = "something [else]"
        }
        @{
            Expression = [PSArm.Expression.ArmStringLiteral]'"quoted"'
            Expected = '\"quoted\"'
        }
    )

    It "Creates the expected expression: <Expected>" -TestCases $testCases {
        param(
            [PSArm.Expression.IArmExpression]
            $Expression,

            [string]
            $Expected
        )

        $Expression.ToExpressionString() | Should -BeExactly $Expected
    }
}