using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal class PowerShellArmVariableConstructor : PowerShellArmTemplateParameterConstructor<ArmVariable>
    {
        public PowerShellArmVariableConstructor(PowerShell pwsh, HashSet<string> parameterNames)
            : base(pwsh, parameterNames)
        {
        }

        protected override ArmVariable EvaluateParameter(ParameterAst parameter)
        {
            return new ArmVariable(new ArmStringLiteral(GetParameterName(parameter)), GetParameterValue(parameter));
        }
    }
}
