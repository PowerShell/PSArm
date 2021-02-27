
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Template;
using PSArm.Internal;
using PSArm.Schema;
using System;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    internal class KeywordContextFrame
    {
        private readonly Lazy<ArmResourceName?> _resourceNameLazy;

        public KeywordContextFrame(
            KeywordContext context,
            int contextIndex,
            CommandAst commandAst)
        {
            ParentContext = context;
            ContextIndex = contextIndex;
            CommandAst = commandAst;
            _resourceNameLazy = new Lazy<ArmResourceName?>(GetResourceName);
        }

        public CommandAst CommandAst { get; }

        public string ResourceNamespace => _resourceNameLazy.Value?.Namespace;

        public string ResourceTypeName => _resourceNameLazy.Value?.Type;

        public string ResourceApiVersion => _resourceNameLazy.Value?.ApiVersion;

        public KeywordContext ParentContext { get; }

        public int ContextIndex { get; }

        public string GetDiscriminatorValue(string discriminatorName)
        {
            if (CommandAst.CommandElements is null)
            {
                return null;
            }

            bool expectingDiscriminator = false;
            foreach (CommandElementAst commandElement in CommandAst.CommandElements)
            {
                if (commandElement is CommandParameterAst parameterAst
                    && parameterAst.ParameterName.Is(discriminatorName))
                {
                    if (parameterAst.Argument != null)
                    {
                        return (parameterAst.Argument as StringConstantExpressionAst)?.Value;
                    }

                    expectingDiscriminator = true;
                    continue;
                }

                if (expectingDiscriminator)
                {
                    return (commandElement as StringConstantExpressionAst)?.Value;
                }
            }

            return null;
        }

        private ArmResourceName? GetResourceName()
        {
            if (CommandAst.CommandElements is null)
            {
                return null;
            }

            string resourceNamespace = null;
            string type = null;
            string apiVersion = null;

            int expect = 0;
            for (int i = 0; i < CommandAst.CommandElements.Count; i++)
            {
                CommandElementAst element = CommandAst.CommandElements[i];

                if (element is CommandParameterAst parameterAst)
                {
                    expect = 0;
                    if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.Type)))
                    {
                        expect = 1;
                    }
                    else if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.ApiVersion)))
                    {
                        expect = 2;
                    }
                    else if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.Provider)))
                    {
                        expect = 3;
                    }

                    continue;
                }

                switch (expect)
                {
                    case 1:
                        if (element is StringConstantExpressionAst typeStrExpr)
                        {
                            type = typeStrExpr.Value;
                        }
                        break;

                    case 2:
                        if (element is StringConstantExpressionAst apiVersionStrExpr)
                        {
                            apiVersion = apiVersionStrExpr.Value;
                        }
                        break;

                    case 3:
                        if (element is StringConstantExpressionAst providerStrExpr)
                        {
                            resourceNamespace = providerStrExpr.Value;
                        }
                        break;
                }

                expect = 0;
            }

            if (resourceNamespace is null
                && type is null
                && apiVersion is null)
            {
                return null;
            }

            return new ArmResourceName(resourceNamespace, type, apiVersion);
        }
    }
}
