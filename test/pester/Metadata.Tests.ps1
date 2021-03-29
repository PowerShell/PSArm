Describe "Module and assembly metadata" {
    It "Should have matching module and assembly metadata" {
        $module = Get-Module 'PSArm'
        $asmVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($module.Path)

        $moduleVersion = "$($module.Version)"

        if ($module.PrivateData.PSData.Prerelease)
        {
            $prerelease = $module.PrivateData.PSData.Prerelease
            $moduleVersion = "$moduleVersion-$prerelease"
        }

        $asmVersion.ProductVersion | Should -BeExactly $moduleVersion
    }
}
