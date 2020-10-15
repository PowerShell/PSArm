using AutoRest.Core;
using AutoRest.Core.Extensibility;
using AutoRest.Core.Model;

namespace AutoRest.PSArm
{
    public sealed class PluginPSArm
        : Plugin<IGeneratorSettings, CodeModelTransformer<CodeModel>, CodeGeneratorPSArm, CodeNamer, CodeModel>
    {
    }
}