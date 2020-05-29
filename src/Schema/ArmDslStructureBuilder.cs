using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    public class ArmDslKeywordDefinitionScope
    {
        public ArmDslKeywordDefinitionScope()
        {
            Keywords = new Dictionary<ArmDslKeywordSchema, ArmDslKeywordDefinitionScope>();
        }

        public Dictionary<ArmDslKeywordSchema, ArmDslKeywordDefinitionScope> Keywords { get; }
    }

    public class ArmDslStructureBuilder
    {
        private readonly HashSet<ArmDslKeywordSchema> _seenKeywords;

        public ArmDslStructureBuilder()
        {
            _seenKeywords = new HashSet<ArmDslKeywordSchema>();
        }

        public IReadOnlyDictionary<string, ArmDslKeywordDefinitionScope> GatherKeywordDefinitionStructure(ArmDslProviderSchema providerSchema)
        {
            var resourceDslStructures = new Dictionary<string, ArmDslKeywordDefinitionScope>();
            foreach (KeyValuePair<string, ArmDslResourceSchema> resourceEntry in providerSchema.Resources)
            {
                resourceDslStructures.Add(resourceEntry.Key, BuildResourceKeywordStructure(resourceEntry.Value));
            }
            return resourceDslStructures;
        }

        private ArmDslKeywordDefinitionScope BuildResourceKeywordStructure(ArmDslResourceSchema resourceSchema)
        {
            // Collect all keywords
            IEnumerable<ArmDslKeywordSchema> keywords = CollectAllKeywordsForResource(resourceSchema);

            // Visit schema for each keyword and find the lowest scope where that keyword can be defined
            (IEnumerable<ArmDslKeywordSchema> topLevelKeywords, Dictionary<ArmDslKeywordSchema, List<ArmDslKeywordSchema>> keywordChildTable) = DetermineKeywordDefinitionScope(resourceSchema, keywords);

            // Now reconstruct the scopes from the top level keywords down based on the keyword scope pointers
            return ConstructDefinitionScopes(keywordChildTable, topLevelKeywords);
        }

        private IEnumerable<ArmDslKeywordSchema> CollectAllKeywordsForResource(ArmDslResourceSchema resource)
        {
            return CollectKeywords(new HashSet<ArmDslKeywordSchema>(), resource.Keywords);
        }

        private HashSet<ArmDslKeywordSchema> CollectKeywords(HashSet<ArmDslKeywordSchema> keywordAcc, Dictionary<string, ArmDslKeywordSchema> keywordSchema)
        {
            foreach (ArmDslKeywordSchema keyword in keywordSchema.Values)
            {
                if (keywordAcc.Add(keyword) && keyword.Body != null)
                {
                    CollectKeywords(keywordAcc, keyword.Body);
                }
            }

            return keywordAcc;
        }

        private (IEnumerable<ArmDslKeywordSchema>, Dictionary<ArmDslKeywordSchema, List<ArmDslKeywordSchema>>) DetermineKeywordDefinitionScope(ArmDslResourceSchema resourceSchema, IEnumerable<ArmDslKeywordSchema> keywords)
        {
            var keywordChildTable = new Dictionary<ArmDslKeywordSchema, List<ArmDslKeywordSchema>>();
            var topLevelKeywords = new HashSet<ArmDslKeywordSchema>(resourceSchema.Keywords.Values);
            foreach (ArmDslKeywordSchema keyword in keywords)
            {
                if (topLevelKeywords.Contains(keyword))
                {
                    continue;
                }

                ArmDslKeywordSchema parent = DetermineKeywordParent(resourceSchema, keyword);

                if (parent == null)
                {
                    topLevelKeywords.Add(keyword);
                    continue;
                }

                if (!keywordChildTable.TryGetValue(parent, out List<ArmDslKeywordSchema> children))
                {
                    children = new List<ArmDslKeywordSchema>();
                    keywordChildTable[parent] = children;
                }
                children.Add(keyword);
            }

            return (topLevelKeywords, keywordChildTable);
        }

        private ArmDslKeywordSchema DetermineKeywordParent(ArmDslResourceSchema resource, ArmDslKeywordSchema keyword)
        {
            CountKeywordInstancesAndTrackBestScope(new HashSet<ArmDslKeywordSchema>(), keyword, resource.Keywords, currentParent: null, out ArmDslKeywordSchema parent);
            return parent;
        }

        private int CountKeywordInstancesAndTrackBestScope(
            HashSet<ArmDslKeywordSchema> seenKeywords,
            ArmDslKeywordSchema keyword,
            Dictionary<string, ArmDslKeywordSchema> currentSchemaNode,
            ArmDslKeywordSchema currentParent,
            out ArmDslKeywordSchema parent)
        {
            // Algorithm is:
            //  - Count the occurences of the keyword in each child scope
            //  - The count for this scope is the total of those plus 1 if the keyword occurs in this scope
            //  - If the count for this scope is higher than any child scope, the current parent is now the parent

            // Set the parent to null, since we can't guarantee we will set it
            parent = null;

            // Prevent recursion
            if (currentParent != null && !seenKeywords.Add(currentParent))
            {
                return 0;
            }

            int count = 0;
            int max = 0;
            foreach (ArmDslKeywordSchema childKeyword in currentSchemaNode.Values)
            {
                if (childKeyword == keyword)
                {
                    count++;
                    continue;
                }

                if (childKeyword.Body != null)
                {
                    int childCount = CountKeywordInstancesAndTrackBestScope(seenKeywords, keyword, childKeyword.Body, childKeyword, out ArmDslKeywordSchema suggestedParent);
                    count += childCount;
                    if (childCount > max)
                    {
                        max = childCount;
                        parent = suggestedParent;
                    }
                }
            }

            if (count > max)
            {
                parent = currentParent;
            }

            return count;
        }

        private ArmDslKeywordDefinitionScope ConstructDefinitionScopes(
            Dictionary<ArmDslKeywordSchema, List<ArmDslKeywordSchema>> childKeywordTable,
            IEnumerable<ArmDslKeywordSchema> scopeKeywords)
        {
            var scope = new ArmDslKeywordDefinitionScope();
            foreach (ArmDslKeywordSchema keyword in scopeKeywords)
            {
                if (childKeywordTable.TryGetValue(keyword, out List<ArmDslKeywordSchema> childKeywords)
                    && childKeywordTable != null)
                {
                    scope.Keywords[keyword] = ConstructDefinitionScopes(childKeywordTable, childKeywords);
                }
                else
                {
                    scope.Keywords[keyword] = null;
                }
            }

            return scope;
        }
    }
}
