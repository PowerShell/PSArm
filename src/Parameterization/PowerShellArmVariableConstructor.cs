
// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal class PowerShellArmVariableConstructor : PowerShellArmTemplateParameterConstructor<ArmVariable>
    {
        public PowerShellArmVariableConstructor(HashSet<string> parameterNames)
            : base(parameterNames)
        {
        }

        protected override ArmVariable EvaluateParameter(List<PSVariable> variables, ParameterAst parameter)
        {
            return new ArmVariable(new ArmStringLiteral(GetParameterName(parameter)), GetParameterValue(parameter, variables));
        }
    }
}
