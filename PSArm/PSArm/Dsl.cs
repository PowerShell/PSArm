using System.Collections.ObjectModel;
using System.Management.Automation;

public static class Dsl
{
    public static Collection<PSObject> Invoke(PSCmdlet cmdlet, ScriptBlock scriptBlock)
    {
        return cmdlet.InvokeCommand.InvokeScript(cmdlet.SessionState, scriptBlock);
    }
}
