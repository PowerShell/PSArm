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
                @{ Completion = '-Name'; ListItem = 'Name' }
                @{ Completion = '-Type'; ListItem = 'Type' }
                @{ Completion = '-Location'; ListItem = 'Location' }
                @{ Completion = '-Kind'; ListItem = 'Kind' }
                @{ Completion = '-Provider'; ListItem = 'Provider' }
                @{ Completion = '-Body'; ListItem = 'Body' }
                @{ Completion = '-ApiVersion'; ListItem = 'ApiVersion' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -Provider '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'Microsoft.Network' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -ApiVersion '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = '2019-11-01' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -Provider Microsoft.Network -ApiVersion '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = '2019-11-01' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -ApiVersion 2019-11-01 -Provider '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'Microsoft.Network' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'virtualRouters' }
                @{ Completion = 'networkInterfaces' }
                @{ Completion = 'networkInterfaces/tapConfigurations' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInter'
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'networkInterfaces' }
                @{ Completion = 'networkInterfaces/tapConfigurations' }
            )
        }
        @{
            StringToComplete = 'Arm { Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInterfaces/'
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'networkInterfaces/tapConfigurations' }
            )
        }
        @{
            StringToComplete = 'Arm { Output -'
            Type = 'ParameterName'
            ExpectedCompletions = @(
                @{ Completion = '-Name'; ListItem = 'Name' }
                @{ Completion = '-Value'; ListItem = 'Value' }
                @{ Completion = '-Type'; ListItem = 'Type' }
            )
        }
        @{
            StringToComplete = '
                Arm {
                    Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInterfaces {
                        '
            Type = 'Command'
            ExpectedCompletions = @(
                @{ Completion = 'Properties' }
                @{ Completion = 'Resource' }
                @{ Completion = 'DependsOn' }
                @{ Completion = 'Sku' }
            )
        }
        @{
            StringToComplete = '
                Arm {
                    Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInterfaces {
                        Properties {
                            '
            Type = 'Command'
            ExpectedCompletions = @(
                @{ Completion = 'Subnet' }
                @{ Completion = 'NetworkSecurityGroup' }
                @{ Completion = 'PrivateEndpoint' }
                @{ Completion = 'IpConfiguration' }
            )
        }
        @{
            StringToComplete = '
                Arm {
                    Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInterfaces {
                        Properties {
                            IPConfiguration -'
            Type = 'ParameterName'
            ExpectedCompletions = @(
                @{ Completion = 'Name'; ListItem = '-Name' }
                @{ Completion = 'Id'; ListItem = '-Id' }
                @{ Completion = 'PrivateIPAddress'; ListItem = '-PrivateIPAddress' }
                @{ Completion = 'PrivateIPAllocationMethod'; ListItem = '-PrivateIPAllocationMethod' }
            )
        }
        @{
            StringToComplete = '
                Arm {
                    Resource "banana" -ApiVersion 2019-11-01 -Provider Microsoft.Network -Type networkInterfaces {
                        Properties {
                            IPConfiguration -PrivateIPAllocationMethod '
            Type = 'ParameterValue'
            ExpectedCompletions = @(
                @{ Completion = 'Static' }
                @{ Completion = 'Dynamic' }
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

        $expectedItems = @($ExpectedCompletions | ForEach-Object { if ($_.ListItem) { $_.ListItem} else { $_.Completion } })

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