BeforeDiscovery {
    # Test cases come from the examples folder
    $exampleDir = (Resolve-Path "$PSScriptRoot/../../examples").Path
    $examples = Get-ChildItem -LiteralPath $exampleDir

    $testCases = $examples | ForEach-Object {
        $basePath = $_.FullName
        $scriptPath = Join-Path -Path $basePath -ChildPath 'test.ps1'
        $templatePath = Join-Path -Path $basePath -ChildPath 'template.json'

        if ((Test-Path -LiteralPath $scriptPath) -and (Test-Path -LiteralPath $templatePath))
        {
            @{ Name = $_.Name; ScriptPath = $scriptPath; TemplatePath = $templatePath }
        }
    }
}

BeforeAll {
    function Assert-JsonEqual
    {
        param(
            $ReferenceObject,
            $DifferenceObject
        )

        # Object handling
        if ($ReferenceObject -is [System.Collections.IDictionary])
        {
            $DifferenceObject | Should -BeOfType 'System.Collections.IDictionary'

            $differenceKeys = [System.Collections.Generic.HashSet[string]]::new([string[]]$DifferenceObject.get_Keys())

            foreach ($entry in $ReferenceObject.GetEnumerator())
            {
                $differenceKeys.Remove($entry.Key)

                $DifferenceObject.ContainsKey($entry.Key) | Should -BeTrue -Because "Difference object should have all keys in reference object"

                $differenceValue = $DifferenceObject[$entry.Key]
                Assert-JsonEqual -ReferenceObject $entry.Value -DifferenceObject $differenceValue
            }

            $differenceKeys | Should -HaveCount 0 -Because "Extra keys '$($differenceKeys -join ', ')' should not exist"

            return
        }

        # Array handling
        if ($ReferenceObject -is [array])
        {
            $DifferenceObject | Should -BeOfType 'System.Array'
            $DifferenceObject | Should -HaveCount $ReferenceObject.Count -Because "Difference object size $($DifferenceObject.Count) should be $($ReferenceObject.Count)"

            for ($i = 0; $i -lt $ReferenceObject.Count; $i++)
            {
                Assert-JsonEqual -ReferenceObject ($ReferenceObject[$i]) -DifferenceObject ($ReferenceObject[$i])
            }

            return
        }

        # At this point we only really expect primitives, since the input was JSON
        $DifferenceObject | Should -BeExactly $ReferenceObject
    }
}

Describe "Full ARM template conversions using examples" {
    It "Example <Name>: PSArm script at <ScriptPath> evaluates equivalently to <TemplatePath>" -TestCases $testCases {
        param([string]$Name, [string]$ScriptPath, [string]$TemplatePath)

        $armObject = & $ScriptPath
        $generatedJson = $armObject.ToJson().ToString() | ConvertFrom-Json -AsHashtable
        $referenceJson = Get-Content -Raw -LiteralPath $TemplatePath | ConvertFrom-Json -AsHashtable
        Assert-JsonEqual -ReferenceObject $referenceJson -DifferenceObject $generatedJson
    }
}