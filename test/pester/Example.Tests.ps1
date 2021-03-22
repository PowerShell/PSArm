﻿
# Copyright (c) Microsoft Corporation.

BeforeDiscovery {
    # Test cases come from the examples folder
    $exampleDir = (Resolve-Path "$PSScriptRoot/../../examples").Path
    $examples = Get-ChildItem -LiteralPath $exampleDir

    $testCases = $examples | ForEach-Object {
        $basePath = $_.FullName
        $templatePath = Join-Path -Path $basePath -ChildPath 'template.json'

        if (Test-Path -LiteralPath $templatePath)
        {
            @{ Name = $_.Name; ExamplePath = $basePath }
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot/../tools/TestHelper.psm1"
}

Describe "Full ARM template conversions using examples" {
    It "Example <Name>: PSArm script at <ScriptPath> evaluates equivalently to <TemplatePath>" -TestCases $testCases {
        param([string]$Name, [string]$ExamplePath)

        $templatePath = Join-Path -Path $ExamplePath -ChildPath 'template.json'
        $parameterPath = Join-Path -Path $ExamplePath -ChildPath 'parameters.json'

        if (Test-Path $parameterPath)
        {
            $parameters = Get-Content -Raw $parameterPath | ConvertFrom-Json
        }

        $armObject = Publish-PSArmTemplate -TemplatePath $ExamplePath -Parameters $parameters -NoWriteFile -NoHashTemplate -PassThru

        # Deal with PSVersion separately since otherwise tests run across powershell versions will fail
        $metadataPSVersion = $armObject.Metadata.GeneratorMetadata['psarm-psversion'].Value
        $armObject.Metadata.GeneratorMetadata.Remove('psarm-psversion')

        $generatedJson = $armObject.ToJson()
        $referenceJson = Get-Content -Raw -LiteralPath $templatePath | ConvertFrom-Json

        Assert-StructurallyEqual -ComparisonObject $referenceJson -JsonObject $generatedJson
        $metadataPSVersion | Should -BeExactly $PSVersionTable.PSVersion
    }
}