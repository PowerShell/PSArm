
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json;

namespace PSArm.Serialization
{
    public class ArmBuiltinFunction
    {
        [JsonConstructor]
        public ArmBuiltinFunction(
            string name,
            string description,
            int minimumArguments,
            int? maximumArguments,
            string[] returnValueMembers)
        {
            Name = name;
            Description = description;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            ReturnValueMembers = returnValueMembers;
        }

        public string Name { get; }

        public string Description { get; }

        public int MinimumArguments { get; }

        public int? MaximumArguments { get; }

        public string[] ReturnValueMembers { get; }
    }
}
