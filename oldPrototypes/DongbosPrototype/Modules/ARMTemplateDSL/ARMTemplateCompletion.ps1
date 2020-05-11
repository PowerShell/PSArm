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
                $resAsts = $body.ScriptBlock.FindAll({
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
                    $resName = $resMetadata.ResourceName

                    ## We may not be able to extract the names of some resources because
                    ## they are passed in through the pipeline.
                    if ($resName) {
                        $resources.Add($resName)
                    }
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
            $bodyValue = $bindingResults.BoundParameters['Body'].Value
            $bodyValue = $bodyValue -is [ScriptBlockExpressionAst] ? $bodyValue : $null

            return @{
                CommandName = $commandName
                Body = $bodyValue
            }
        }
    }
}

function HandleQuote
{
    param([string]$wordToComplete)

    if ([string]::IsNullOrEmpty($wordToComplete)) {
        return
    }

    $firstCharIsSingleQuote = $wordToComplete[0] -eq "'"
    $firstCharIsDoubleQuote = $wordToComplete[0] -eq '"'

    if ($firstCharIsSingleQuote -or $firstCharIsDoubleQuote) {
        $quote = $firstCharIsSingleQuote ? "'" : '"'

        $length = $wordToComplete.Length
        if ($wordToComplete.Length -eq 1) {
            return @{ WordToComplete = ''; Quote = $quote }
        }

        $lastCharIsSingleQuote = $wordToComplete[-1] -eq "'"
        $lastCharIsDoubleQuote = $wordToComplete[-1] -eq '"'

        if (($firstCharIsSingleQuote -and $lastCharIsSingleQuote) -or
            ($firstCharIsDoubleQuote -and $lastCharIsDoubleQuote)) {
            return @{
                WordToComplete = $wordToComplete.Substring(1, $length - 2)
                Quote = $quote
            }
        }

        if (!$lastCharIsSingleQuote -and !$lastCharIsDoubleQuote) {
            return @{
                WordToComplete = $wordToComplete.Substring(1)
                Quote = $quote
            }
        }
    }
}

## TODO: Need to have helper methods to handle quotes for tab completion.

Register-ArgumentCompleter -CommandName Property -ParameterName Name -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $quoteToUse = "'"
    $handledWord = HandleQuote $wordToComplete
    if ($handledWord -ne $null) {
        $wordToComplete = $handledWord.WordToComplete
        $quoteToUse = $handledWord.Quote
    }

    $result = Build-Context $commandAst
    $context = $result.Context

    $moduleName = GetModuleName -ResourceType $context.ResourceType
    $schema = & "$moduleName\Get-Schema" -name $context.SchemaName
    $schema.Keys | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        $completionText = $quoteToUse + $_ + $quoteToUse
        [CompletionResult]::new($completionText, $_, 'ParameterValue', $_)
    }
}

Register-ArgumentCompleter -CommandName Property -ParameterName Value -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $quoteToUse = "'"
    $handledWord = HandleQuote $wordToComplete
    if ($handledWord -ne $null) {
        $wordToComplete = $handledWord.WordToComplete
        $quoteToUse = $handledWord.Quote
    }

    $result = Build-Context $commandAst
    $context = $result.Context

    $moduleName = GetModuleName -ResourceType $context.ResourceType
    $schema = & "$moduleName\Get-Schema" -name $context.SchemaName

    $cmdMetadata = Get-CommandMetadata $commandAst
    $propName = $cmdMetadata.PropertyName

    $currentKeySchema = $schema[$propName]
    if ($currentKeySchema.Type -eq 'enum') {
        $currentKeySchema.Values | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            $completionText = $quoteToUse + $_ + $quoteToUse
            [CompletionResult]::new($completionText, $_, 'ParameterValue', $_)
        }
    }
}

Register-ArgumentCompleter -CommandName ResourceId -ParameterName ResourceName -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $quoteToUse = "'"
    $handledWord = HandleQuote $wordToComplete
    if ($handledWord -ne $null) {
        $wordToComplete = $handledWord.WordToComplete
        $quoteToUse = $handledWord.Quote
    }

    $result = Build-Context $commandAst
    $context = $result.Context
    $result.Resources |
        Where-Object { $_ -ne $context.ResourceName -and $_ -like "$wordToComplete*" } |
        ForEach-Object {
            $completionText = $_[0] -eq '$' ? $_ : $quoteToUse + $_ + $quoteToUse
            [CompletionResult]::new($completionText, $_, 'ParameterValue', $_)
        }
}
