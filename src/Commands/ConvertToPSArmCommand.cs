using PSArm.ArmBuilding;
using PSArm.Conversion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands
{
    [Cmdlet(VerbsData.ConvertTo, "PSArm")]
    public class ConvertToPSArmCommand : PSCmdlet
    {
        private List<ArmTemplate> _templatesToConvert;

        [ValidateNotNull]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ArmTemplate[] InputTemplate { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter]
        public string OutFile { get; set; }

        public ConvertToPSArmCommand()
        {
            _templatesToConvert = new List<ArmTemplate>();
        }

        protected override void ProcessRecord()
        {
            foreach (ArmTemplate template in InputTemplate)
            {
                _templatesToConvert.Add(template);
            }
        }

        protected override void EndProcessing()
        {
            if (OutFile != null)
            {
                foreach (ArmTemplate template in _templatesToConvert)
                {
                    PSArmWriter.WriteToFile(OutFile, template);
                }

                return;
            }

            foreach (ArmTemplate template in _templatesToConvert)
            {
                WriteObject(PSArmWriter.WriteToString(template));
            }
        }
    }
}
