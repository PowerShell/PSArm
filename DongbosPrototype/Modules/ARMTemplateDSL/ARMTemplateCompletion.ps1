using namespace System.Collections.Generic
using namespace System.Management.Automation
using namespace System.Management.Automation.Language

function Build-Context
{
    param(
        [CommandAst] $commandAst
    )

    $stack = [Stack[CommandAst]]::new()

    ## Currently handle the happy case only, and doesn't do any check on the mis-use of the DSL.
    $parent = $commandAst.Parent
    while ($null -ne $parent) {
        if ($parent -is [CommandAst]) {
            $cmdAst = $parent
            $cmdName = Get-CommandName $cmdAst
            if ('Resource', 'Property', 'Template' -contains $cmdName) {
                $stack.Push($cmdAst)
            }
            if ($cmdName -eq 'Template') {
                break
            }
        }
        $parent = $parent.Parent
    }

    $context = $null
    $resources = [List[string]]::new()

    while ($stack.Count -gt 0) {
        $commandAst = $stack.Pop()
        $metadata = Get-CommandMetadata $commandAst

        switch ($metadata.CommandName) {
            'Resource' {
                $context = @{
                    ResourceName = $metadata.ResourceName
                    ResourceType = $metadata.ResourceType
                    SchemaName = ''
                }
            }

            'Property' {
                $moduleName = GetModuleName -ResourceType $context.ResourceType
                $schema = & "$moduleName\Get-Schema" -name $context.SchemaName

                $context = @{
                    ResourceName = $context.ResourceName
                    ResourceType = $context.ResourceType
                    SchemaName = $schema[$metadata.PropertyName].Command
                }
            }

            'Template' {
                $body = $metadata.Body
                $resAsts = $body.FindAll({
                    param($ast)
                    $cmdAst = $ast -as [CommandAst]
                    if ($null -eq $cmdAst) {
                        return $false
                    }

                    $cmdName = Get-CommandName $cmdAst
                    if ($cmdName -eq 'Resource') {
                        return $true
                    }
                }, $false)

                foreach ($resAst in $resAsts) {
                    $resMetadata = Get-CommandMetadata $resAst
                    $resources.Add($resMetadata.ResourceName)
                }
            }
        }
    }

    return @{ Context = $context; Resources = $resources }
}

function Get-CommandName
{
    param(
        [CommandAst] $commandAst
    )

    $firstElement = $commandAst.CommandElements[0] -as [StringConstantExpressionAst]
    if ($null -ne $firstElement) {
        return $firstElement.Value
    }
}

function Get-CommandMetadata
{
    param(
        [CommandAst] $commandAst
    )

    $commandName = Get-CommandName $commandAst
    $bindingResults = [StaticParameterBinder]::BindCommand($commandAst)

    ## TODO: handle BindingExceptions?
    switch ($commandName) {
        'Resource' {
            $resNameElement = $bindingResults.BoundParameters['Name']
            $resNameValue = $resNameElement.ConstantValue
            if (-not $resNameValue -and $resNameElement.Value -is [VariableExpressionAst]) {
                $resNameValue = $resNameElement.Value.ToString()
            }

            $resTypeElement = $bindingResults.BoundParameters['Type']
            $resTypeValue = $resTypeElement.ConstantValue

            return @{
                CommandName = $commandName
                ResourceName = $resNameValue
                ResourceType = $resTypeValue
            }
        }

        'Property' {
            $propNameElement = $bindingResults.BoundParameters['Name']
            $propNameValue = $propNameElement.ConstantValue

            return @{
                CommandName = $commandName
                PropertyName = $propNameValue
            }
        }

        'Template' {
            $bodyValue = $bindingResults.BoundParameters['Body']
            $bodyValue = $bodyValue -is [ScriptBlockExpressionAst] ? $bodyValue : $null

            return @{
                CommandName = $commandName
                Body = $bodyValue
            }
        }
    }
}

Register-ArgumentCompleter -CommandName Property -ParameterName Name -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $result = Build-Context $commandAst
    $context = $result.Context

    $moduleName = GetModuleName -ResourceType $context.ResourceType
    $schema = & "$moduleName\Get-Schema" -name $context.SchemaName
    $schema.Keys | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        [CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}

Register-ArgumentCompleter -CommandName Property -ParameterName Value -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $result = Build-Context $commandAst
    $context = $result.Context

    $moduleName = GetModuleName -ResourceType $context.ResourceType
    $schema = & "$moduleName\Get-Schema" -name $context.SchemaName

    $cmdMetadata = Get-CommandMetadata $commandAst
    $propName = $cmdMetadata.PropertyName

    $currentKeySchema = $schema[$propName]
    if ($currentKeySchema.Type -eq 'enum') {
        $currentKeySchema.Values | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}

Register-ArgumentCompleter -CommandName ResourceId -ParameterName ResourceName -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $result = Build-Context $commandAst
    $context = $result.Context
    $result.Resources |
        Where-Object { $_ -ne $context.ResourceName -and $_ -like "$wordToComplete*" } |
        ForEach-Object {
            [CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
}
