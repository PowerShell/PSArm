# Copyright (c) Microsoft Corporation.
# All rights reserved.

Describe "PSArm completions" {
    $testCases = @(
        @{
            StringToComplete = 'Arm { '
            Type = 'Command'
            ExpectedCompletions = @(
                @{ Completion = 'Resource' }
                @{ Completion = 'Output' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource -'
            Type = 'ParameterName'
            ExpectedCompletions = @(
                @{ Completion = '-Name' }
                @{ Completion = '-Type' }
                @{ Completion = '-Location' }
                @{ Completion = '-Kind' }
                @{ Completion = '-Provider' }
                @{ Completion = '-Body' }
                @{ Completion = '-ApiVersion' }
            )
        }
    )

    It "Completes input '<StringToComplete>' as expected" -TestCases $testCases {
        param(
            [string]$StringToComplete,
            [hashtable[]]$ExpectedCompletions,
            [System.Management.Automation.CompletionResultType]$Type,
            [int]$CursorIndex = $StringToComplete.Length
        )

        $expectedItems = $ExpectedCompletions | ForEach-Object { if ($_.ListItem) { $_.ListItem} else { $_.Completion } }

        $completions = (TabExpansion2 -inputScript $StringToComplete -cursorColumn $CursorIndex).CompletionMatches |
            Where-Object { $_.ListItemText -in $expectedItems } |
            ForEach-Object { $ht = @{} } { $ht[$_.ListItemText] = $_ } { $ht }

        for ($i = 0; $i -lt $ExpectedCompletions.Count; $i++)
        {
            $expected = $ExpectedCompletions[$i]
            $expectedItem = $expectedItems[$i]
            $expectedType = if ($expected.Type) { $expected.Type } else { $Type }

            $actual = $completions[$expectedItem]
            $actual | Should -Not -BeNullOrEmpty -Because "Expected a completion like '$($expected.Completion)'"

            $actual.CompletionText | Should -BeExactly $expected.Completion

            if ($expectedType)
            {
                $actual.ResultType | Should -BeExactly $expectedType
            }

            if ($expected.ListItem)
            {
                $actual.ListItemText | Should -BeExactly $expectedItem
            }

            if ($expected.ToolTip)
            {
                $actual.ToolTip | Should -BeExactly $expected.ToolTip
            }
        }
    }
}