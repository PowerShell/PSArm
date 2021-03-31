
BeforeAll {
    Import-Module "$PSScriptRoot/../tools/TestHelper.psm1"
}

Describe "PSArm templates working with PS aliases" {
    It "Disables aliases within the Arm block, but restores them afterward" {
        function DoNothing {}

        Set-Alias -Name addressPrefix -Value DoNothing

        $psArmScriptPath = "$PSScriptRoot/assets/aliastest.psarm.ps1"
        $expectedTemplatePath = "$PSScriptRoot/assets/aliastest-template.json"

        $template = Publish-PSArmTemplate -Path $psArmScriptPath -OutFile -NoHashTemplate -NoWriteFile -PassThru
        $template.Metadata.GeneratorMetadata.Remove('psarm-psversion')

        $generatedJson = $template.ToJson()
        $referenceJson = Get-Content -Raw -LiteralPath $expectedTemplatePath | ConvertFrom-Json

        (Get-Alias -Name addressPrefix -Scope Local).Definition | Should -BeExactly DoNothing
        (Get-Alias -Name '%' -Scope Global).Definition | Should -BeExactly ForEach-Object
        Assert-StructurallyEqual -ComparisonObject $referenceJson -JsonObject $generatedJson
    }
}