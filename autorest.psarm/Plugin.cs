using AutoRest.Core;
using AutoRest.Core.Extensibility;
using AutoRest.Core.Model;

namespace AutoRest.PSArm
{
    public sealed class PluginPSArm
        : Plugin<IGeneratorSettings, CodeModelTransformer<CodeModel>, CodeGeneratorPSArm, CodeNamer, CodeModel>
    {
        public PluginPSArm(Logger logger, string outputFolder) : base()
        {
            CodeGenerator.Logger = logger;
            CodeGenerator.OutputFolder = outputFolder;
        }
    }
}