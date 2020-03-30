using System;

namespace Dsl
{
    public class KeywordAttribute : Attribute
    {
        public string[] OccursIn { get; set; }
    }
}