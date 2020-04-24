$PSCmdlet.MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    Set-Item Function:\TabExpansion2 (Get-Content -Raw Function:__OldTabExpansion2)
    Remove-Item Function:__OldTabExpansion2
}