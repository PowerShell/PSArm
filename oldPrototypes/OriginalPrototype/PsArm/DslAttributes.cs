
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;

namespace Dsl
{
    public class KeywordAttribute : Attribute
    {
        public string[] OccursIn { get; set; }
    }
}