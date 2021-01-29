
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using PSArm.Internal;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    /// <summary>
    /// Describes all the information needed about a cursor position in a script
    /// to provide ARM DSL keyword completions at that position.
    /// </summary>
    internal class KeywordContext
    {
        public KeywordContext()
        {
            KeywordStack = new List<string>();
        }

        /// <summary>
        /// The DSL keywords by scope, from the bottom up.
        /// </summary>
        public List<string> KeywordStack { get; }

        /// <summary>
        /// The ARM resource type namespace we're in.
        /// </summary>
        public string ResourceNamespace { get; set; }

        /// <summary>
        /// The ARM resource type name (without the namespace) we're in.
        /// </summary>
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// The stated API version of the ARM resource we're in.
        /// </summary>
        public string ResourceApiVersion { get; set; }

        /// <summary>
        /// The smallest AST containing our effetive position.
        /// </summary>
        public Ast ContainingAst { get; set; }

        /// <summary>
        /// The complete AST of the input script we're completing in.
        /// </summary>
        public Ast FullAst { get; set; }

        /// <summary>
        /// The complete list of tokens of the input script we're in.
        /// </summary>
        public IReadOnlyList<Token> Tokens { get; set; }

        /// <summary>
        /// The index in the token list of the last token before the cursor.
        /// </summary>
        public int LastTokenIndex { get; set; }

        /// <summary>
        /// The last token before the cursor.
        /// </summary>
        public Token LastToken { get; set; }

        /// <summary>
        /// The last non-newline token before the cursor.
        /// </summary>
        public Token LastNonNewlineToken { get; set; }

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public IScriptPosition Position { get; set; }

        /// <summary>
        /// The smallest containing command AST containing the cursor.
        /// </summary>
        public CommandAst ContainingCommandAst { get; set; }

        public string GetDiscriminatorValue(string discriminatorName)
        {
            if (ContainingCommandAst.CommandElements == null)
            {
                return null;
            }

            bool expectingDiscriminator = false;
            foreach (CommandElementAst commandElement in ContainingCommandAst.CommandElements)
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
    }

}