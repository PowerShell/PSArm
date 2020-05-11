[PSArm.Completion.ArmTypeAccelerators]::Load()

$PSCmdlet.MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    [PSArm.Completion.ArmTypeAccelerators]::Unload()
    Set-Item Function:\TabExpansion2 (Get-Content -Raw Function:__OldTabExpansion2)
    Remove-Item Function:__OldTabExpansion2
}