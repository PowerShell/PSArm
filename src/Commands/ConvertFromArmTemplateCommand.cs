using PSArm.Conversion;
using System;
using System.Management.Automation;

namespace PSArm.Commands
{
    [Cmdlet(VerbsData.ConvertFrom, "ArmTemplate", DefaultParameterSetName = "Input")]
    public class ConvertFromArmTemplateCommand : PSCmdlet
    {
        private readonly ArmParser _parser;

        public ConvertFromArmTemplateCommand()
        {
            _parser = new ArmParser();
        }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Path", Position = 0, Mandatory = true)]
        public string[] Path { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Uri", Position = 0, Mandatory = true)]
        public Uri[] Uri { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Input", Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string[] Input { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Input":
                    foreach (string input in Input)
                    {
                        WriteObject(_parser.ParseString(input));
                    }
                    return;

                case "Path":
                    foreach (string path in Path)
                    {
                        WriteObject(_parser.ParseFile(path));
                    }
                    return;

                case "Uri":
                    foreach (Uri uri in Uri)
                    {
                        WriteObject(_parser.ParseUri(uri));
                    }
                    return;
            }
        }
    }
}

