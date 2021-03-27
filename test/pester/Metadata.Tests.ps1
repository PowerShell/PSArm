Describe "Module and assembly metadata" {
    It "Should have matching module and assembly metadata" {
        $module = Get-Module 'PSArm'
        $assembly = (Get-Command Publish-PSArmTemplate).ImplementingType.Assembly

        $module.Version | Should -Be $assembly.GetName().Version
    }
}