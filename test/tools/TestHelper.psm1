# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Assert-StructurallyEqual
{
    param(
        [Parameter(Mandatory)]
        [Newtonsoft.Json.Linq.JToken]
        $JsonObject,

        [Parameter(Mandatory)]
        [object]
        $ComparisonObject,

        [Parameter()]
        [string]
        $Path = '/'
    )

    if ($ComparisonObject -is [array])
    {
        $JsonObject.GetType().FullName | Should -BeExactly 'Newtonsoft.Json.Linq.JArray' -Because "array expected at '$Path'"
        $JsonObject.Count | Should -BeExactly $ComparisonObject.Count -Because "array should have the correct number of elements at '$Path'"

        for ($i = 0; $i -lt $ComparisonObject.Count; $i++)
        {
            Assert-StructurallyEqual -JsonObject $JsonObject[$i] -ComparisonObject $ComparisonObject[$i] -Path "$Path.[$i]"
        }

        return
    }

    if ($JsonObject -is [Newtonsoft.Json.Linq.JObject])
    {
        foreach ($key in Get-Member -InputObject $ComparisonObject -MemberType Properties)
        {
            $name = $key.Name
            $JsonObject.ContainsKey($name) | Should -BeTrue -Because "object should contain property '$name' at '$Path'"
        }

        foreach ($entry in $JsonObject.GetEnumerator())
        {
            $key = $entry.Key
            $subObject = $ComparisonObject.$key

            $subObject | Should -Not -BeNullOrEmpty -Because "Property '$key' is present in the JSON object at '$Path.$key'"

            Assert-StructurallyEqual -JsonObject $entry.Value -ComparisonObject $subObject -Path "$Path.$key"
        }

        return
    }

    if ($JsonObject -is [Newtonsoft.Json.Linq.JValue])
    {
        $JsonObject.Value | Should -Be $ComparisonObject -Because "$value should equal $ComparisonObject at '$Path'"
        return
    }

    Write-Error "$JsonObject is of unexpected type $($JsonObject.GetType().FullName)"
}

function Assert-EquivalentToTemplate
{
    param(
        [Parameter(Mandatory)]
        [PSArm.Templates.Primitives.ArmElement]
        $GeneratedObject,

        [Parameter(Mandatory, ParameterSetName="Definition")]
        [string]
        $TemplateDefinition,

        [Parameter(Mandatory, ParameterSetName="Path")]
        [string]
        $TemplatePath
    )

    $generatedJson = $GeneratedObject.ToJson()

    $template = if ($TemplateDefinition)
    {
        ConvertFrom-Json -InputObject $TemplateDefinition
    }
    else
    {
        ConvertFrom-Json -InputObject (Get-Content -Raw $TemplatePath)
    }

    Assert-StructurallyEqual -JsonObject $generatedJson -ComparisonObject $template
}
