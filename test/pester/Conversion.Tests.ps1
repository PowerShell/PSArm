BeforeAll {
    Import-Module "$PSScriptRoot/../tools/TestHelper.psm1"
}

Describe "ARM conversion cmdlets" {
    It "Can round-trip an ARM template successfully" {
        $templatePath = "$PSScriptRoot/assets/roundtrip-template.json"
        $psarmScriptPath = Join-Path $TestDrive 'test-template.psarm.ps1'

        ConvertFrom-ArmTemplate -Path $templatePath | ConvertTo-PSArm -OutFile $psarmScriptPath -Force
        $armObject = Publish-PSArmTemplate -TemplatePath $psarmScriptPath -NoWriteFile -NoHashTemplate -PassThru

        $armTemplate = $armObject.Resources[0]['properties']['template']

        $original = Get-Content -Path $templatePath -Raw | ConvertFrom-Json
        $created = $armTemplate.ToJson()

        Assert-StructurallyEqual -ComparisonObject $original -JsonObject $created
    }
}
