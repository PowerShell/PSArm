
using PSArm.Commands.Template;
using PSArm.Completion;
using PSArm.Internal;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSArm.Schema.Keyword
{
    internal class ResourceKeywordSchema : KnownParametersSchema
    {
        public static ResourceKeywordSchema Value { get; } = new ResourceKeywordSchema();

        private static readonly ConcurrentDictionary<ArmResourceName, ResourceKeywordCache> s_resourceKeywordCaches = new ConcurrentDictionary<ArmResourceName, ResourceKeywordCache>();

        private ResourceKeywordSchema()
            : base(KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmResourceCommand)), useParametersForCompletions: false)
        {
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context)
        {
            if (!ResourceIndex.SharedInstance.TryGetResourceSchema(
                context.ResourceNamespace,
                context.ResourceTypeName,
                context.ResourceApiVersion,
                out ResourceSchema resource))
            {
                return null;
            }

            ResourceKeywordCache cache = s_resourceKeywordCaches.GetOrAdd(
                new ArmResourceName(context.ResourceNamespace, context.ResourceTypeName, context.ResourceApiVersion),
                (resourceName) => resource.Discriminator != null ? new DiscriminatedResourceKeywordCache(resource) : new ObjectResourceKeywordCache(resource));

            return cache.GetInnerKeywords(context);
        }

        public override IEnumerable<string> GetParameterValues(KeywordContextFrame context, string parameterName)
        {
            ArmResourceName resourceName = GetResourceNameFromAst(context.CommandAst);
            return GetParameterValues(parameterName, resourceName.Namespace, resourceName.Type, resourceName.ApiVersion);
        }

        public IEnumerable<string> GetParameterValues(
            string parameterName,
            string namespaceValue,
            string typeValue,
            string apiVersionValue)
        {
            IEnumerable<ResourceSchema> resources = ResourceIndex.SharedInstance.GetResourceSchemas();

            if (parameterName.Is(nameof(NewPSArmResourceCommand.Provider)))
            {
                FilterForName(ref resources, typeValue);
                FilterForApiVersion(ref resources, apiVersionValue);
                return resources.Select(r => r.Namespace).Distinct();
            }

            if (parameterName.Is(nameof(NewPSArmResourceCommand.Type)))
            {
                FilterForNamespace(ref resources, namespaceValue);
                FilterForApiVersion(ref resources, apiVersionValue);
                return resources.Select(r => r.Name).Distinct();
            }

            if (parameterName.Is(nameof(NewPSArmResourceCommand.ApiVersion)))
            {
                FilterForNamespace(ref resources, namespaceValue);
                FilterForName(ref resources, typeValue);
                return resources.Select(r => r.ApiVersion).Distinct();
            }

            return null;
        }

        private static void FilterForNamespace(ref IEnumerable<ResourceSchema> resources, string namespaceValue)
        {
            if (namespaceValue is null)
            {
                return;
            }

            resources = resources.Where(r => r.Namespace.HasPrefix(namespaceValue));
        }

        private static void FilterForName(ref IEnumerable<ResourceSchema> resources, string nameValue)
        {
            if (nameValue is null)
            {
                return;
            }

            resources = resources.Where(r => r.Name.HasPrefix(nameValue));
        }

        private static void FilterForApiVersion(ref IEnumerable<ResourceSchema> resources, string apiVersionValue)
        {
            if (apiVersionValue is null)
            {
                return;
            }

            resources = resources.Where(r => r.ApiVersion.HasPrefix(apiVersionValue));
        }

        private static ArmResourceName GetResourceNameFromAst(CommandAst commandAst)
        {
            string provider = null;
            string type = null;
            string apiVersion = null;

            var expectedParameter = ResourceKeywordParameter.None;
            foreach (CommandElementAst commandElement in commandAst.CommandElements)
            {
                if (expectedParameter != ResourceKeywordParameter.None
                    && commandElement is CommandParameterAst parameterAst)
                {
                    if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.Provider)))
                    {
                        if (parameterAst.Argument != null)
                        {
                            provider = (parameterAst.Argument as StringConstantExpressionAst)?.Value;
                        }
                        else
                        {
                            expectedParameter = ResourceKeywordParameter.Provider;
                        }

                        continue;
                    }

                    if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.Type)))
                    {
                        if (parameterAst.Argument != null)
                        {
                            type = (parameterAst.Argument as StringConstantExpressionAst)?.Value;
                        }
                        else
                        {
                            expectedParameter = ResourceKeywordParameter.Type;
                        }

                        continue;
                    }

                    if (parameterAst.ParameterName.Is(nameof(NewPSArmResourceCommand.ApiVersion)))
                    {
                        if (parameterAst.Argument != null)
                        {
                            apiVersion = (parameterAst.Argument as StringConstantExpressionAst)?.Value;
                        }
                        else
                        {
                            expectedParameter = ResourceKeywordParameter.ApiVersion;
                        }

                        continue;
                    }
                }

                switch (expectedParameter)
                {
                    case ResourceKeywordParameter.Provider:
                        provider = (commandElement as StringConstantExpressionAst)?.Value;
                        break;

                    case ResourceKeywordParameter.Type:
                        type = (commandElement as StringConstantExpressionAst)?.Value;
                        break;

                    case ResourceKeywordParameter.ApiVersion:
                        apiVersion = (commandElement as StringConstantExpressionAst)?.Value;
                        break;
                }

                expectedParameter = ResourceKeywordParameter.None;
            }

            return new ArmResourceName(provider, type, apiVersion);
        }

        private enum ResourceKeywordParameter
        {
            None,
            Type,
            Provider,
            ApiVersion,
        }
    }
}
