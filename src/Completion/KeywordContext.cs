using System.Collections.Generic;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    internal class KeywordContext
    {
        public KeywordContext()
        {
            KeywordStack = new List<string>();
        }

        public List<string> KeywordStack { get; }

        public string ResourceNamespace { get; set; }

        public string ResourceTypeName { get; set; }

        public string ResourceApiVersion { get; set; }

        public Ast ContainingAst { get; set; }

        public Ast FullAst { get; set; }

        public IReadOnlyList<Token> Tokens { get; set; }

        public int LastTokenIndex { get; set; }

        public Token LastToken { get; set; }

        public Token LastNonNewlineToken { get; set; }

        public IScriptPosition Position { get; set; }

        public CommandAst ContainingCommandAst { get; set; }
    }

}